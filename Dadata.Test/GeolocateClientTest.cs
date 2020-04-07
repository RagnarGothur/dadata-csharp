using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Dadata.Test
{
    [TestFixture]
    public class GeolocateClientTest
    {
        public GeolocateClient api { get; set; }

        [SetUp]
        public void SetUp()
        {
            Environment.SetEnvironmentVariable("DADATA.API_KEY", "45d04a6b125b08de35f93f0914a37ac7a7e21ede");
            var token = Environment.GetEnvironmentVariable("DADATA.API_KEY");
            this.api = new GeolocateClient(token);
        }

        [Test]
        public async Task GeolocateTest()
        {
            var response = await api.Geolocate(lat: 55.7366021, lon: 37.597643);
            var address = response.suggestions[0].data;
            Assert.AreEqual(address.city, "Москва");
            Assert.AreEqual(address.street, "Турчанинов");
        }

        [Test]
        public async Task NotFoundTest()
        {
            var response = await api.Geolocate(lat: 0, lon: 0);
            Assert.AreEqual(response.suggestions.Count, 0);
        }
    }
}

