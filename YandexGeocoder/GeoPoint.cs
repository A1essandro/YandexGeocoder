using System.Globalization;

namespace YandexGeocoder
{
    public class GeoPoint
    {

        internal GeoPoint(double latitude, double longitude)
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

        public double Latitude { get; }
        public double Longitude { get; }

    }
}