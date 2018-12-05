using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IRTech.YandexGeocoder.CacheProvider;
using YandexGeocoder.ServiceProvider;

namespace IRTech.YandexGeocoder
{
    public class Geocoder : IDisposable
    {
        private readonly DirectServiceProvider _directService;
        private readonly ICacheProvider _cacheProvider;

        public int RequestCount => _directService.RequestCount;

        public ICacheProvider СacheProvider => _cacheProvider;

        public Geocoder(ICacheProvider cacheProvider = null, FailureStrategy failureStrategy = FailureStrategy.ReturnDefault)
        {
            _cacheProvider = cacheProvider ?? new DummyCacheProvider();
            _directService = new DirectServiceProvider(_cacheProvider, failureStrategy);
        }

        /// <summary>
        /// Checks for correct connection with the service
        /// </summary>
        /// <returns></returns>
        public Task<bool> CheckConnection() => _directService.CheckConnection();

        /// <summary>
        /// Checks for correct connection with the service
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public Task<bool> CheckConnection(int timeout) => _directService.CheckConnection(timeout);

        public Task<IEnumerable<GeoPoint>> GetPoints(string address, CancellationToken cToken = default(CancellationToken)) => _directService.GetPoints(address, cToken);

        public async Task<GeoPoint> GetPoint(string address, CancellationToken cToken = default(CancellationToken))
        {
            var collection = await GetPoints(address, cToken).ConfigureAwait(false);

            return collection.FirstOrDefault();
        }

        public async Task<IDictionary<string, GeoPoint>> GetPointByAddresses(IEnumerable<string> addresses, CancellationToken cToken = default(CancellationToken), IProgress<int> progress = null)
        {
            var rawResult = await _directService.GetPointsByAddressList(addresses.Distinct(), cToken, progress).ConfigureAwait(false);

            return rawResult.ToDictionary(x => x.Key, x => x.Value.FirstOrDefault());
        }

        public async Task<IDictionary<string, IEnumerable<GeoPoint>>> GetPointsByAddresses(IEnumerable<string> addresses, CancellationToken cToken = default(CancellationToken), IProgress<int> progress = null)
        {
            var rawResult = await _directService.GetPointsByAddressList(addresses.Distinct(), cToken, progress).ConfigureAwait(false);

            return rawResult.ToDictionary(x => x.Key, x => x.Value);
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _directService.Dispose();
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