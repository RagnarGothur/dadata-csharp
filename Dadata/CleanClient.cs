﻿using Dadata.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Dadata
{

	/// <summary>
	/// DaData Clean API (https://dadata.ru/api/clean/)
	/// </summary>
	public class CleanClient : ClientBase
	{
		const string BASE_URL = "https://dadata.ru/api/v2/clean";
		protected new const uint maxRequestsPerSecond = 20;

		string secret;
		CustomCreationConverter<IDadataEntity> converter;

		// maps concrete IDadataEntity types to corresponding structure types
		static Dictionary<Type, StructureType> TYPE_TO_STRUCTURE = new Dictionary<Type, StructureType>() {
			{ typeof(Address), StructureType.ADDRESS },
			{ typeof(AsIs), StructureType.AS_IS },
			{ typeof(Birthdate), StructureType.BIRTHDATE },
			{ typeof(Email), StructureType.EMAIL },
			{ typeof(Fullname), StructureType.NAME },
			{ typeof(Passport), StructureType.PASSPORT },
			{ typeof(Phone), StructureType.PHONE },
			{ typeof(Vehicle), StructureType.VEHICLE }
		};


		public CleanClient(string token, string secret, string baseUrl = BASE_URL) : base(token, baseUrl, maxRequestsPerSecond)
		{
			this.secret = secret;
			// all response data entities look the same (IDadataEntity), 
			// need to manually convert them to specific types (address, phone etc)
			this.converter = new CleanResponseConverter();
			// need to serialize StructureType as string, not int
			serializer.Converters.Add(new StringEnumConverter());
		}

		public async Task<T> Clean<T>(string source) where T : IDadataEntity
		{
			// infer structure from target entity type
			var structure = new List<StructureType>(
				new StructureType[] { TYPE_TO_STRUCTURE[typeof(T)] }
			);
			// transform enity list to CleanRequest data structure
			var data = new string[] { source };
			var response = await Clean(structure, data);
			return (T)response[0];
		}

		public async Task<IList<IDadataEntity>> Clean(IEnumerable<StructureType> structure, IEnumerable<string> data)
		{
			var request = new CleanRequest(structure, data);
			var httpRequest = CreateHttpRequest();
			httpRequest = SerializeRequest(httpRequest, request);
			var httpResponse = (HttpWebResponse)await httpRequest.GetResponseAsync();
			var response = await Deserialize<CleanResponse>(httpResponse);
			return response.data[0];
		}

		protected HttpWebRequest CreateHttpRequest()
		{
			var request = base.CreateHttpRequest(verb: "POST", url: baseUrl);
			if (secret != null)
			{
				request.Headers.Add("X-Secret", this.secret);
			}
			return request;
		}

		protected override async Task<T> Deserialize<T>(HttpWebResponse httpResponse)
		{
			using (var r = new StreamReader(httpResponse.GetResponseStream()))
			{
				string responseText = await r.ReadToEndAsync();
				return JsonConvert.DeserializeObject<T>(responseText, this.converter);
			}
		}

	}
}
