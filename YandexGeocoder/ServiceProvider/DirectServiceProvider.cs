using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IRTech.YandexGeocoder;
using IRTech.YandexGeocoder.CacheProvider;
using Newtonsoft.Json.Linq;

namespace YandexGeocoder.ServiceProvider
{
    internal class DirectServiceProvider : IDisposable
    {

        private static readonly string BaseUrl = "http://geocode-maps.yandex.ru/1.x/?format=json&geocode=";

        private readonly HttpClient _client = new HttpClient();
        private readonly FailureStrategy _failureStrategy;
        private readonly ICacheProvider _cacheProvider;
        private readonly object _counterLock = new object();
        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource _clearCacheCancellationTokenSource = new CancellationTokenSource();
        private int _requestCount = 0;

        public int RequestCount => _requestCount;

        internal DirectServiceProvider(ICacheProvider cacheProvider, FailureStrategy failureStrategy)
        {
            _failureStrategy = failureStrategy;
            _cacheProvider = cacheProvider;
            _clearCacheTask(_clearCacheCancellationTokenSource.Token);
        }

        public async Task<IEnumerable<GeoPoint>> GetPoints(string address)
        {
            if (_cacheProvider.ContainsAddress(address))
            {
                return _cacheProvider.Get(address);
            }

            await _cacheLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_cacheProvider.ContainsAddress(address))
                {
                    return _cacheProvider.Get(address);
                }

                var jsonObject = await _getResponseJObject(address).ConfigureAwait(false);
                try
                {
                    var rawPoints = _parseJObjectCommon(jsonObject);
                    if (_failureStrategy == FailureStrategy.ThrowException && rawPoints.Count() == 0)
                    {
                        throw new Exception("Empty result");
                    }

                    var result = rawPoints.Select(x => new GeoPoint(_parsePos(x)));
                    var toCache = new KeyValuePair<string, IEnumerable<GeoPoint>>(address, result);
                    _cacheProvider.Set(new[] { toCache });

                    return result;
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

        public async Task<IEnumerable<KeyValuePair<string, IEnumerable<GeoPoint>>>> GetPointsByAddressList(IEnumerable<string> addresses)
        {
            var emptyValues = addresses.Where(x => !_cacheProvider.ContainsAddress(x));
            var fromCacheTask = Task.Run(() => _cacheProvider.Get(addresses.Where(x => _cacheProvider.ContainsAddress(x))));

            var fillTasks = emptyValues
                .Select(async x => new KeyValuePair<string, IEnumerable<GeoPoint>>(x, await GetPoints(x).ConfigureAwait(false)));

            await Task.WhenAll(fillTasks).ConfigureAwait(false);
            var filled = fillTasks.Select(x => x.Result);
            _cacheProvider.Set(filled);

            var fromCache = await fromCacheTask.ConfigureAwait(false);
            return fromCache.Concat(filled);
        }

        private async Task<JObject> _getResponseJObject(string address)
        {
            var jsonStringTask = _client.GetStringAsync(BaseUrl + address);
            lock (_counterLock)
            {
                _requestCount++;
            }

            return JObject.Parse(await jsonStringTask.ConfigureAwait(false));
        }

        private static JToken _parseJObjectCommon(JObject jObject)
        {
            return jObject["response"]["GeoObjectCollection"]["featureMember"];
        }

        private static string _parsePos(JToken featureMember)
        {
            return (string)featureMember["GeoObject"]["Point"]["pos"];
        }

        private void _clearCacheTask(CancellationToken cToken)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromDays(30), cToken);
                    await _cacheLock.WaitAsync(cToken).ConfigureAwait(false);
                    try
                    {
                        _cacheProvider.Clear();
                    }
                    finally
                    {
                        _cacheLock.Release();
                    }
                }
            }, cToken);
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _clearCacheCancellationTokenSource.Cancel();
                    _client.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

    }
}