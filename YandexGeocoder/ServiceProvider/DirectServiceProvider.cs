using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using IRTech.YandexGeocoder;
using IRTech.YandexGeocoder.CacheProvider;
using Newtonsoft.Json.Linq;

namespace YandexGeocoder.ServiceProvider
{
    internal class DirectServiceProvider : IDisposable
    {

        private static readonly string BaseUrl = "https://geocode-maps.yandex.ru/1.x/?format=json&geocode=";

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
            if (!(cacheProvider is DummyCacheProvider))
                _clearCacheTask(_clearCacheCancellationTokenSource.Token);
        }


        /// <summary>
        /// Checks for correct connection with the service
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<bool> CheckConnection(int timeout)
        {
            var cts = new CancellationTokenSource();

            try
            {
                cts.CancelAfter(timeout);

                var resp = await _client.GetAsync(BaseUrl, cts.Token).ConfigureAwait(false);
                return resp.StatusCode == HttpStatusCode.OK;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Checks for correct connection with the service
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<bool> CheckConnection()
        {
            var resp = await _client.GetAsync(BaseUrl).ConfigureAwait(false);
            return resp.StatusCode == HttpStatusCode.OK;
        }

        public async Task<IEnumerable<GeoPoint>> GetPoints(string address, CancellationToken cToken)
        {
            if (_cacheProvider.ContainsAddress(address))
            {
                return _cacheProvider.Get(address);
            }

            cToken.ThrowIfCancellationRequested();
            await _cacheLock.WaitAsync(cToken).ConfigureAwait(false);
            try
            {
                if (_cacheProvider.ContainsAddress(address))
                {
                    return _cacheProvider.Get(address);
                }

                cToken.ThrowIfCancellationRequested();
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

        public async Task<IEnumerable<KeyValuePair<string, IEnumerable<GeoPoint>>>> GetPointsByAddressList(IEnumerable<string> addresses, CancellationToken cToken, IProgress<int> progress)
        {
            var progressValue = 0;
            var emptyValues = addresses.Where(x => !_cacheProvider.ContainsAddress(x));
            var inCacheAddresses = addresses.Where(x => _cacheProvider.ContainsAddress(x));

            Task<IEnumerable<KeyValuePair<string, IEnumerable<GeoPoint>>>> fromCacheTask = null;
            if (inCacheAddresses.Count() > 0)
            {
                progressValue = inCacheAddresses.Count();
                progress?.Report(progressValue);
                fromCacheTask = Task.Run(() => _cacheProvider.Get(inCacheAddresses));
            }

            var progressLock = new object();
            var fillTasks = emptyValues
                .Select(async x =>
                {
                    cToken.ThrowIfCancellationRequested();
                    var points = await GetPoints(x, cToken).ConfigureAwait(false);

                    if (progress != null)
                    {
                        lock (progressLock)
                        {
                            progress?.Report(++progressValue);
                        }
                    }

                    return new KeyValuePair<string, IEnumerable<GeoPoint>>(x, points);
                });

            var filled = await Task.WhenAll(fillTasks).ConfigureAwait(false);
            _cacheProvider.Set(filled);
            if (fromCacheTask == null)
            {
                return filled;
            }

            cToken.ThrowIfCancellationRequested();
            var fromCache = await fromCacheTask.ConfigureAwait(false);
            return fromCache.Concat(filled);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task<JObject> _getResponseJObject(string address)
        {
            var jsonStringTask = _client.GetStringAsync(BaseUrl + address);
            lock (_counterLock)
            {
                _requestCount++;
            }

            return JObject.Parse(await jsonStringTask.ConfigureAwait(false));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static JToken _parseJObjectCommon(JObject jObject) => jObject["response"]["GeoObjectCollection"]["featureMember"];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string _parsePos(JToken featureMember) => (string)featureMember["GeoObject"]["Point"]["pos"];

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
                    _cacheLock.Dispose();
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