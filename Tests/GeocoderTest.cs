using System;
using System.Linq;
using System.Threading.Tasks;
using IRTech.YandexGeocoder;
using IRTech.YandexGeocoder.CacheProvider;
using Xunit;

namespace YandexGeocoder.Test
{
    public class GeocoderTest
    {

        [Fact]
        public async Task PointTest()
        {
            var geocoder = new Geocoder();
            var samara = await geocoder.GetPoint("Самара");

            Assert.Equal(53.195538, samara.Latitude);
            Assert.Equal(50.101783, samara.Longitude);
        }

        [Fact]
        public async Task PointsTest()
        {
            var geocoder = new Geocoder();
            var samara = await geocoder.GetPoints("Самара");

            Assert.True(samara.Count() > 3);
            Assert.Equal(53.195538, samara.First().Latitude);
            Assert.Equal(50.101783, samara.First().Longitude);
        }

        [Fact]
        public async Task RequestCountTest()
        {
            var geocoder = new Geocoder();
            Assert.Equal(0, geocoder.RequestCount);
            await geocoder.GetPoints("Брест");
            Assert.Equal(1, geocoder.RequestCount);
            await geocoder.GetPoints("Брест");
            Assert.Equal(2, geocoder.RequestCount);

            geocoder = new Geocoder(new MemoryCacheProvider());
            Assert.Equal(0, geocoder.RequestCount);
            await geocoder.GetPoints("Якутск");
            Assert.Equal(1, geocoder.RequestCount);
            await geocoder.GetPoints("Якутск");
            Assert.Equal(1, geocoder.RequestCount);
            await geocoder.GetPoints("Якутск, улица Хабарова, д 5");
            Assert.Equal(2, geocoder.RequestCount);
        }

        [Fact]
        public async Task FailureStrategyTest()
        {
            var fakeAddress = "qwaszx";
            var geocoderDefault = new Geocoder(null, FailureStrategy.ReturnDefault);
            var geocoderException = new Geocoder(null, FailureStrategy.ThrowException);

            Assert.True(await geocoderDefault.GetPoint(fakeAddress) == null);
            Assert.Equal(new GeoPoint[0], await geocoderDefault.GetPoints(fakeAddress));
            await Assert.ThrowsAnyAsync<Exception>(async () => await geocoderException.GetPoint(fakeAddress));
            await Assert.ThrowsAnyAsync<Exception>(async () => await geocoderException.GetPoints(fakeAddress));
        }


        [Fact]
        public async Task CheckConnectionTest()
        {
            var geocoder = new Geocoder();

            Assert.False(await geocoder.CheckConnection(1));
            Assert.True(await geocoder.CheckConnection());
            Assert.True(await geocoder.CheckConnection(10000));
        }

    }
}
