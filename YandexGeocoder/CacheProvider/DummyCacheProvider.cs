using System;
using System.Collections.Generic;

namespace IRTech.YandexGeocoder.CacheProvider
{
    internal class DummyCacheProvider : ICacheProvider
    {

        private static readonly string InvalidOperationMessage = "This method should never be called";

        public bool ContainsAddress(string address) => false;

        public IEnumerable<GeoPoint> Get(string x) => throw new InvalidOperationException(InvalidOperationMessage);

        public IEnumerable<KeyValuePair<string, IEnumerable<GeoPoint>>> Get(IEnumerable<string> enumerable) => throw new InvalidOperationException(InvalidOperationMessage);

        public void Set(IEnumerable<KeyValuePair<string, IEnumerable<GeoPoint>>> data)
        {
            //empty
        }

        public void Set(KeyValuePair<string, IEnumerable<GeoPoint>> data)
        {
            //empty
        }

        public void Set(string address, IEnumerable<GeoPoint> point)
        {
            //empty
        }
    }
}