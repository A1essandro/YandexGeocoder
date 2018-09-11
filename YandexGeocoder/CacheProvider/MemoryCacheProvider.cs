using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace YandexGeocoder.CacheProvider
{
    public class MemoryCacheProvider : ICacheProvider
    {

        private readonly IDictionary<string, IEnumerable<GeoPoint>> _cache = new ConcurrentDictionary<string, IEnumerable<GeoPoint>>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsAddress(string address)
        {
            return _cache.ContainsKey(address);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<GeoPoint> Get(string x)
        {
            return _cache[x];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<KeyValuePair<string, IEnumerable<GeoPoint>>> Get(IEnumerable<string> enumerable)
        {
            return enumerable.Select(x => new KeyValuePair<string, IEnumerable<GeoPoint>>(x, _cache[x]));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(IEnumerable<KeyValuePair<string, IEnumerable<GeoPoint>>> data)
        {
            foreach (var item in data)
            {
                _cache.Add(item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(KeyValuePair<string, IEnumerable<GeoPoint>> data)
        {
            _cache.Add(data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(string address, IEnumerable<GeoPoint> point)
        {
            _cache.Add(address, point);
        }

    }
}