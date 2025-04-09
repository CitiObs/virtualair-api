using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace VirtualAirDataApi.Controllers
{
	public class Observation
	{
		public DateTime PhenomenonTimeStart { get; set; }
		public DateTime? PhenomenonTimeEnd { get; set; }
		public double Result { get; set; }
	}

	public class HourlyData
	{
		public DateTime Hour { get; set; }
		public double AverageResult { get; set; }
		public int Count { get; set; }
	}

	[ApiController]
	public class ObservationsController : BaseController
	{

		[HttpGet("v1.1/Things({tid})/Datastreams({did})/Observations")]
		public async Task<JObject> GetDatastreamForThing(string tid, string did)
		{

            string requestUrl = GetModifiedRequestUrl();

            var observationsJson = await ProcessRequest(requestUrl, Request.QueryString.Value);

			// Extract the "value" array from the JObject
			var valueArray = (JArray)observationsJson["value"];

			if(valueArray != null)
			{
				var observations = valueArray
				.Select(o => new Observation
				{
					PhenomenonTimeStart = ParsePhenomenonTime(o["phenomenonTime"].ToString(Newtonsoft.Json.Formatting.None)).Item1,
					PhenomenonTimeEnd = ParsePhenomenonTime(o["phenomenonTime"].ToString(Newtonsoft.Json.Formatting.None)).Item2,
					Result = o["result"].ToObject<double>()
				})
				.ToList();

				var hourlyData = GenerateHourlyData(observations);

				foreach (var data in hourlyData)
				{
					//Console.WriteLine($"Hour: {data.Hour}, Average Result: {data.AverageResult}, Count: {data.Count}");
				}
			}
			else
			{
				Console.WriteLine("User is requesting Observations for Thing with ID: " + tid + " and Datastream with ID: " + did + " but no observations were found");
			}


			
			return await ProcessRequest(requestUrl, Request.QueryString.Value);

		}

		private static Tuple<DateTime, DateTime?> ParsePhenomenonTime(string phenomenonTime)
		{
			// Remove surrounding quotes if they exist
			phenomenonTime = phenomenonTime.Trim('"');

			if (phenomenonTime.Contains("/"))
			{
				var times = phenomenonTime.Split('/');
				var start = DateTime.Parse(times[0], null, DateTimeStyles.RoundtripKind);
				var end = DateTime.Parse(times[1], null, DateTimeStyles.RoundtripKind);
				return new Tuple<DateTime, DateTime?>(start, end);
			}
			else
			{
				var start = DateTime.Parse(phenomenonTime, null, DateTimeStyles.RoundtripKind);
				return new Tuple<DateTime, DateTime?>(start, null);
			}
		}

		private static List<HourlyData> GenerateHourlyData(List<Observation> observations)
		{
			var hourlyData = new Dictionary<DateTime, List<double>>();

			foreach (var observation in observations)
			{
				var start = observation.PhenomenonTimeStart;
				var end = observation.PhenomenonTimeEnd ?? start;

				for (var dt = new DateTime(start.Year, start.Month, start.Day, start.Hour, 0, 0); dt <= end; dt = dt.AddHours(1))
				{
					if (!hourlyData.ContainsKey(dt))
					{
						hourlyData[dt] = new List<double>();
					}
					hourlyData[dt].Add(observation.Result);
				}
			}

			return hourlyData.Select(kvp => new HourlyData
			{
				Hour = kvp.Key,
				AverageResult = kvp.Value.Average(),
				Count = kvp.Value.Count
			}).ToList();
		}

	}
}
