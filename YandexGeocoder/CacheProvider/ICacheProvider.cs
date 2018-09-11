using System.Collections.Generic;

namespace YandexGeocoder.CacheProvider
{
    public interface ICacheProvider
    {

        bool ContainsAddress(string address);

        void Set(IEnumerable<KeyValuePair<string, IEnumerable<GeoPoint>>> data);

        void Set(KeyValuePair<string, IEnumerable<GeoPoint>> data);

        void Set(string address, IEnumerable<GeoPoint> point);

        IEnumerable<GeoPoint> Get(string x);

        IEnumerable<KeyValuePair<string, IEnumerable<GeoPoint>>> Get(IEnumerable<string> enumerable);

    }
}