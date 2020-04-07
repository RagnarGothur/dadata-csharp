using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Dadata.Test
{
    [TestFixture]
    public class IplocateClientTest
    {
        public IplocateClient api { get; set; }

        [SetUp]
        public void SetUp()
        {
            Environment.SetEnvironmentVariable("DADATA.API_KEY", "45d04a6b125b08de35f93f0914a37ac7a7e21ede");
            var token = Environment.GetEnvironmentVariable("DADATA.API_KEY");
            this.api = new IplocateClient(token);
        }

        [Test]
        public async Task IplocateTest()
        {
            var response = await api.Iplocate("213.180.193.3");
            Assert.AreEqual(response.location.data.city, "Москва");
        }

        [Test]
        public async Task NotFoundTest()
        {
            var response = await api.Iplocate("192.168.0.1");
            Assert.AreEqual(response.location, null);
        }
    }
}
