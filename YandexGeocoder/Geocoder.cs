using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using IRTech.YandexGeocoder.CacheProvider;

namespace IRTech.YandexGeocoder
{
    public class Geocoder : IDisposable
    {

        private static readonly string BaseUrl = "http://geocode-maps.yandex.ru/1.x/?format=json&geocode=";

        private int _requestCount = 0;
        private readonly HttpClient _client;
        private readonly ICacheProvider _cacheProvider;
        private readonly bool _hasCacheProvider;
        private readonly FailureStrategy _failureStrategy;

        private readonly object _counterLock = new object();
        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);

        public int RequestCount => _requestCount;

        public ICacheProvider СacheProvider => _cacheProvider;

        public Geocoder(ICacheProvider cacheProvider = null, FailureStrategy failureStrategy = FailureStrategy.ReturnDefault)
        {
            _client = new HttpClient();
            _cacheProvider = cacheProvider ?? new DummyCacheProvider();
            _hasCacheProvider = _cacheProvider != null;
            _failureStrategy = failureStrategy;
        }

        public async Task<IEnumerable<GeoPoint>> GetPoints(string address)
        {
            return await _getPoints(address);
        }

        public async Task<GeoPoint> GetPoint(string address)
        {
            var collection = await _getPoints(address);
            if (collection.Count() == 0)
            {
                return null;
            }

            return collection.First();
        }

        public async Task<IDictionary<string, GeoPoint>> GetPointByAddresses(IEnumerable<string> addresses)
        {
            var rawResult = await _getPointsByAddressList(addresses.Distinct()).ConfigureAwait(false);
            return rawResult.ToDictionary(x => x.Key, x => x.Value.First());
        }

        public async Task<IDictionary<string, IEnumerable<GeoPoint>>> GetPointsByAddresses(IEnumerable<string> addresses)
        {
            var rawResult = await _getPointsByAddressList(addresses.Distinct()).ConfigureAwait(false);
            return rawResult.ToDictionary(x => x.Key, x => x.Value);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        #region private

        private async Task<IEnumerable<GeoPoint>> _getPoints(string address)
        {
            if (_cacheProvider.ContainsAddress(address))
            {
                return _cacheProvider.Get(address);
            }

            await _cacheLock.WaitAsync();
            try
            {
                if (_cacheProvider.ContainsAddress(address))
                {
                    return _cacheProvider.Get(address);
                }

                var jsonObject = await _getResponseJObject(address);
                try
                {
                    var rawPoints = _parseJObjectCommon(jsonObject);
                    if (_failureStrategy == FailureStrategy.ThrowException && rawPoints.Count() == 0)
                    {
                        throw new Exception("Empty result");
                    }
                    return rawPoints.Select(x => new GeoPoint(_parsePos(x)));
                }
                catch (Exception ex)
                {
                    switch (_failureStrategy)
                    {
                        case FailureStrategy.ReturnDefault:
                            return new GeoPoint[0];
                        case FailureStrategy.ThrowException:
                        default:
                            throw new Exception("JSON parse error", ex);
                    }
                }
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        private async Task<IEnumerable<KeyValuePair<string, IEnumerable<GeoPoint>>>> _getPointsByAddressList(IEnumerable<string> addresses)
        {
            var emptyValues = addresses.Where(x => !_cacheProvider.ContainsAddress(x));
            var fromCacheTask = Task.Run(() => _cacheProvider.Get(addresses.Where(x => _cacheProvider.ContainsAddress(x))));

            var fillTasks = emptyValues.AsParallel()
                .Select(async x => new KeyValuePair<string, IEnumerable<GeoPoint>>(x, await _getPoints(x).ConfigureAwait(false)));

            await Task.WhenAll(fillTasks).ConfigureAwait(false);
            var filled = fillTasks.Select(x => x.Result);
            _cacheProvider.Set(filled);

            var fromCache = await fromCacheTask;
            return fromCache.Concat(filled);
        }

        private async Task<JObject> _getResponseJObject(string address)
        {
            var jsonStringTask = _client.GetStringAsync(BaseUrl + address);
            lock (_counterLock)
            {
                _requestCount++;
            }


            return JObject.Parse(await jsonStringTask);
        }

        private static JToken _parseJObjectCommon(JObject jObject)
        {
            return jObject["response"]["GeoObjectCollection"]["featureMember"];
        }

        private static string _parsePos(JToken featureMember)
        {
            return (string)featureMember["GeoObject"]["Point"]["pos"];
        }

        #endregion

    }
}