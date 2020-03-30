﻿using Dadata.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dadata.Test {

	[TestFixture]
	public class CleanClientTest {

		public CleanClient api { get; set; }

		[SetUp]
		public void SetUp() {
			var token = Environment.GetEnvironmentVariable("DADATA_API_KEY");
			var secret = Environment.GetEnvironmentVariable("DADATA_SECRET_KEY");
			api = new CleanClient(token, secret);
		}

		[Test]
		public async Task CleanAsIsTest() {
			var cleaned = await api.Clean<AsIs>("Омномном");
			Assert.AreEqual(cleaned.source, "Омномном");
		}

		[Test]
		public async Task CleanAddressTest() {
			var cleaned = await api.Clean<Address>("Москва Милютинский 13");
			Assert.AreEqual(cleaned.street, "Милютинский");
			Assert.AreEqual(cleaned.qc, "0");
		}

		[Test]
		public async Task CleanBirthdateTest() {
			var cleaned = await api.Clean<Birthdate>("12.03.1990");
			Assert.AreEqual(cleaned.birthdate, new DateTime(1990, 3, 12));
			Assert.AreEqual(cleaned.qc, "0");
		}

		[Test]
		public async Task CleanEmailTest() {
			var cleaned = await api.Clean<Email>("anderson@matrix.ru");
			Assert.AreEqual(cleaned.email, "anderson@matrix.ru");
			Assert.AreEqual(cleaned.qc, "0");

		}

		[Test]
		public async Task CleanNameTest() {
			var cleaned = await api.Clean<Fullname>("Ольга Викторовна Раздербань");
			Assert.AreEqual(cleaned.name, "Ольга");
			Assert.AreEqual(cleaned.qc, "0");
		}

		[Test]
		public async Task CleanPhoneTest() {
			var cleaned = await api.Clean<Phone>("89168459285");
			Assert.AreEqual(cleaned.number, "8459285");
			Assert.AreEqual(cleaned.qc, "0");
		}

		[Test]
		public async Task CleanPassportTest() {
			var cleaned = await api.Clean<Passport>("4506 629672");
			Assert.AreEqual(cleaned.series, "45 06");
			Assert.AreEqual(cleaned.qc, "10");
		}

		[Test]
		public async Task CleanVehicleTest() {
			var cleaned = await api.Clean<Vehicle>("форд фокус");
			Assert.AreEqual(cleaned.brand, "FORD");
			Assert.AreEqual(cleaned.qc, "0");
		}

		[Test]
		public async Task CleanTest() {
			var structure = new List<StructureType> { StructureType.NAME, StructureType.ADDRESS };
			var data = new List<string> { "Кузнецов Петр Алексеич", "Москва Милютинский 13" };

			var cleaned = await api.Clean(structure, data);
			Assert.AreEqual(cleaned.Count, 2);

			Assert.IsInstanceOf<Fullname>(cleaned[0], "Expected [0] entity to be a Fullname");
			var firstName = (Fullname)cleaned[0];
			Assert.AreEqual(firstName.name, "Петр");
			Assert.AreEqual(firstName.patronymic, "Алексеевич");
			Assert.AreEqual(firstName.surname, "Кузнецов");

			Assert.IsInstanceOf<Address>(cleaned[1], "Expected [1] entity to be an Address");
			var address = (Address)cleaned[1];
			Assert.AreEqual(address.kladr_id, "77000000000717100");
			Assert.AreEqual(address.metro[0].name, "Сретенский бульвар");
		}
	}
}

