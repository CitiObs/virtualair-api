using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text.RegularExpressions;
using VirtualAirDataApi.Handlers;
using VirtualAirDataApi.Models;

namespace VirtualAirDataApi.Controllers
{
	[ApiController]
	public class ApiController : BaseController
	{

		[HttpGet("/v1.1/{*url}")]
		public async Task<JObject> Get(string? url)
		{
			//If the user asks for the root of the API, return the metadata
			if (string.IsNullOrEmpty(url))
			{
				return CreateMetaData();
			}

			return await ProcessRequest(url, Request.QueryString.Value);
		}

		private static JObject CreateMetaData()
		{
			var value = new List<object>
					{
						new { name = "Things", url = $"{VAENDPOINTURL}/Things" },
						new { name = "Datastreams", url = $"{VAENDPOINTURL}/Datastreams" },
						new { name = "Locations", url = $"{VAENDPOINTURL}/Locations" },
						new { name = "HistoricalLocations", url = $"{VAENDPOINTURL}/HistoricalLocations" },
						new { name = "ObservedProperties", url = $"{VAENDPOINTURL}/ObservedProperties" },
						new { name = "Sensors", url = $"{VAENDPOINTURL}/Sensors" },
						new { name = "Observations", url = $"{VAENDPOINTURL}/Observations" },
						new { name = "FeaturesOfInterest", url = $"{VAENDPOINTURL}/FeaturesOfInterest" }
					};

			var serverSettings = new
			{
				conformance = new List<string>
						{
							"http://www.opengis.net/spec/iot_sensing/1.1/req/datamodel",
							"http://www.opengis.net/spec/iot_sensing/1.1/req/request-data",
							"http://www.opengis.net/spec/iot_sensing/1.1/req/resource-path/resource-path-to-entities"
						}
			};

			// Wrap the list and serverSettings in an object
			var wrapper = new { value = value, serverSettings = serverSettings };

			// Convert the wrapper object to a JSON string
			string jsonString = JsonConvert.SerializeObject(wrapper);

			// Parse the JSON string to a JObject
			return JObject.Parse(jsonString);
		}
	}
}
