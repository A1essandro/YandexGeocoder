using IRTech.YandexGeocoder;
using Newtonsoft.Json;
using Xunit;

namespace Tests
{

    public class GeoPointTest
    {
        
        [Fact]
        public void JSONDeserializeTest()
        {
            var json = "{\"Latitude\":12.112227,\"Longitude\":23.477299}";
            var point = JsonConvert.DeserializeObject<GeoPoint>(json);

            Assert.IsType<GeoPoint>(point);
            Assert.Equal(12.112227, point.Latitude);
            Assert.Equal(23.477299, point.Longitude);
        }

    }
}