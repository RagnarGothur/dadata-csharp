﻿using Dadata.Model;
using System.Threading.Tasks;

namespace Dadata {
	public class GeolocateClient : ClientBase {
		const string BASE_URL = "https://suggestions.dadata.ru/suggestions/api/4_1/rs";

		public GeolocateClient(string token, string baseUrl = BASE_URL) : base(token, baseUrl) { }

		public async Task<SuggestResponse<Address>> Geolocate(double lat, double lon) {
			var request = new GeolocateRequest(lat, lon);
			return await Execute<SuggestResponse<Address>>(
				method: "geolocate",
				entity: "address",
				request: request
				);
		}
	}
}
