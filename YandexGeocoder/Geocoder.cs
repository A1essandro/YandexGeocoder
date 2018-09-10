using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using YandexGeocoder.CacheProvider;

namespace YandexGeocoder
{
    public class Geocoder : IDisposable
    {

        private static readonly string BaseUrl = "http://geocode-maps.yandex.ru/1.x/?format=json&geocode=";
        private static int _requestCount = 0;
        private static readonly object _counterLock = new object();

        private readonly HttpClient _client;
        private readonly ICacheProvider _cacheProvider;
        private readonly bool _hasCacheProvider;
        private readonly FailureStrategy _failureStrategy;

        public static int RequestCounter => _requestCount;

        public Geocoder(ICacheProvider cacheProvider = null, FailureStrategy failureStrategy = FailureStrategy.ReturnDefault)
        {
            _client = new HttpClient();
            _cacheProvider = cacheProvider;
            _hasCacheProvider = _cacheProvider != null;
        }

        public async Task<IEnumerable<GeoPoint>> GetPoints(string address)
        {
            var jsonObject = await _getResponseJObject(address);
            try
            {
                var rawPoints = _parseJObjectCommon(jsonObject);
                return rawPoints.Select(x => new GeoPoint(_parsePos(x)));
            }
            catch (Exception ex)
            {
                return _handleParsingException(ex, () => new GeoPoint[0]);
            }
        }

        public async Task<GeoPoint> GetPoint(string address)
        {
            var jsonObject = await _getResponseJObject(address);
            try
            {
                var rawPoint = _parseJObjectCommon(jsonObject).First();
                return new GeoPoint(_parsePos(rawPoint));
            }
            catch (Exception ex)
            {
                return _handleParsingException<GeoPoint>(ex, () => null);
            }
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        #region private

        private async Task<JObject> _getResponseJObject(string address)
        {
            lock (_counterLock)
            {
                _requestCount++;
            }

            var jsonString = await _client.GetStringAsync(BaseUrl + address);
            return JObject.Parse(jsonString);
        }

        private T _handleParsingException<T>(Exception ex, Func<T> defaultCreator)
        {
            switch (_failureStrategy)
            {
                case FailureStrategy.ReturnDefault:
                    return defaultCreator();
                case FailureStrategy.ThrowException:
                default:
                    throw new Exception("JSON parse error", ex);
            }
        }

        private static JToken _parseJObjectCommon(JObject jObject)
        {
            return jObject["response"]["GeoObjectCollection"]["featureMember"];
        }

        private static string _parsePos(JToken featureMember)
        {
            return featureMember["GeoObject"]["Point"]["pos"].ToString();
        }

        #endregion

    }
}