using System.Collections.Generic;

namespace IRTech.YandexGeocoder.CacheProvider
{
    public interface ICacheProvider
    {

        bool ContainsAddress(string address);

        void Set(IEnumerable<KeyValuePair<string, IEnumerable<GeoPoint>>> data);

        IEnumerable<GeoPoint> Get(string x);

        IEnumerable<KeyValuePair<string, IEnumerable<GeoPoint>>> Get(IEnumerable<string> enumerable);

        void Clear();

    }
}