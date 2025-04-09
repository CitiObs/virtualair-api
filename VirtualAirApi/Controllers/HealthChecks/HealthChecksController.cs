using Dapper.Contrib.Extensions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using VirtualAirApi.Common;

namespace VirtualAirApi.Controllers.HealthChecks
{
	[ApiController]
	public class HealthChecksController : ControllerBase
	{
		private readonly ILogger<HealthChecksController> _logger;

		public HealthChecksController(ILogger<HealthChecksController> logger)
		{
			_logger = logger;
		}
		[HttpGet("healthchecks")]
		public async Task<JObject> Get([FromQuery] string url)
		{
			HttpClient client = new HttpClient();
			JObject combined = new JObject();
			combined["value"] = new JArray();

			try
			{
				string response = await client.GetStringAsync(url);
				JObject json = JObject.Parse(response);
				JArray dataArray = (JArray)json["value"];

				foreach (JObject item in dataArray)
				{
					((JArray)combined["value"]).Add(item);
				}
			}
			catch (Exception)
			{
				// Ignore any errors and continue with the next URL
			}

			return combined;


			//return await CombineJsonFromUrls(url);
		}

		[HttpGet("healthchecks/responseTop100Things")]
		public async Task<IActionResult> CheckResponseTime(string url)
		{
			var client = new HttpClient();
			var watch = System.Diagnostics.Stopwatch.StartNew();

			var response = await client.GetAsync($"{url}/Things?$top=100");

			watch.Stop();

			if (response.IsSuccessStatusCode)
			{
				var elapsedMs = watch.ElapsedMilliseconds;
				return Ok(elapsedMs / 1000.0); // return time in seconds
			}
			else
			{
				return BadRequest("Request was not successful");
			}
		}

		private async Task<JObject> CombineJsonFromUrls(string url)
		{
			HttpClient client = new HttpClient();
			JObject combined = new JObject();
			combined["value"] = new JArray();

			try
			{
				string response = await client.GetStringAsync(url);
				JObject json = JObject.Parse(response);
				JArray dataArray = (JArray)json["value"];

				foreach (JObject item in dataArray)
				{
					((JArray)combined["value"]).Add(item);
				}
			}
			catch (Exception)
			{
				// Ignore any errors and continue with the next URL
			}

			return combined;
		}

	}
}
