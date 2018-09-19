using System.Globalization;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace IRTech.YandexGeocoder
{

    [DataContract]
    public class GeoPoint
    {

        [JsonConstructor]
        public GeoPoint(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        internal GeoPoint(string rawPos)
        {
            var arr = rawPos.Split(' ');
            Latitude = double.Parse(arr[1], CultureInfo.InvariantCulture);
            Longitude = double.Parse(arr[0], CultureInfo.InvariantCulture);
        }

        [DataMember]
        public double Latitude { get; }

        [DataMember]
        public double Longitude { get; }

    }
}