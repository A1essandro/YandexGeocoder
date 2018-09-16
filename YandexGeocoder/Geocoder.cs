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

        public async Task<IEnumerable<GeoPoint>> GetPoints(string address, CancellationToken cToken = default(CancellationToken))
        {
            return await _directService.GetPoints(address, cToken).ConfigureAwait(false);
        }

        public async Task<GeoPoint> GetPoint(string address, CancellationToken cToken = default(CancellationToken))
        {
            var collection = await GetPoints(address, cToken).ConfigureAwait(false);
            if (collection.Count() == 0)
            {
                return null;
            }

            return collection.First();
        }

        public async Task<IDictionary<string, GeoPoint>> GetPointByAddresses(IEnumerable<string> addresses, CancellationToken cToken = default(CancellationToken))
        {
            var rawResult = await _directService.GetPointsByAddressList(addresses.Distinct(), cToken).ConfigureAwait(false);
            return rawResult.ToDictionary(x => x.Key, x => x.Value.First());
        }

        public async Task<IDictionary<string, IEnumerable<GeoPoint>>> GetPointsByAddresses(IEnumerable<string> addresses, CancellationToken cToken = default(CancellationToken))
        {
            var rawResult = await _directService.GetPointsByAddressList(addresses.Distinct(), cToken).ConfigureAwait(false);
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