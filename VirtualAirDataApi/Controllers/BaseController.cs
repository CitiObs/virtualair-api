using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using VirtualAirDataApi.Handlers;
using VirtualAirDataApi.Models;

namespace VirtualAirDataApi.Controllers
{
	public class BaseController : ControllerBase
	{
		protected string _url { get; set; }
		protected const string VAENDPOINTURL = "https://api-virtualair.nilu.no/v1.1";

		public BaseController()
		{
			_url = "http://virtualairapi:6020/Observatory";

#if DEBUG
			_url = "http://host.docker.internal:6020/observatory";
#endif

		}

		protected async Task<JObject> ProcessRequest(string? url, string queryString)
		{
			try
			{
                bool isSingleEntity = IsSingleEntityRequest(url);

                var observatories = new List<Observatory>();
				// Get the list of Observatories by querying the API url
				using (var client = new HttpClient())
				{
					var response = await client.GetAsync(_url);
					// Make a list of observatories
					observatories = JsonConvert.DeserializeObject<List<Observatory>>(await response.Content.ReadAsStringAsync());
				}

				if (observatories.Count <= 0)
				{
					// Return an empty array
					return GetEmptyObject(isSingleEntity);
                }

				if (Regex.IsMatch(url, @"\([^)]+\)"))
				{
					SingleEntityHandler handler = new SingleEntityHandler();
                    JObject result = await handler.GetSingleEntity(url, observatories, queryString, isSingleEntity);

                    //Check if the result is empty. In some cases (nested) this should return a multiple entity response
                    if (result.Count == 0)
                    {
                        return GetEmptyObject(isSingleEntity);
                    }

                    return result;
                }
				else
				{
					MultipleEntitiesHandler handler = new MultipleEntitiesHandler();
					return await handler.GetMultipleEntities(url, observatories, queryString);
				}
			}
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return new JObject
                {
                    ["error"] = new JObject
                    {
                        ["code"] = "500",
                        ["message"] = e.Message
                    }
                };
            }
        }

        protected string GetModifiedRequestUrl()
        {
            // Capture the URL part from the request
            string requestUrl = HttpContext.Request.Path;
            // Remove the v1.1/ part of the requestUrl using the substring method
            return requestUrl.Substring(5);
        }

		private JObject GetEmptyObject(bool single)
        {
            if (single)
            {
                return new JObject();
            }
            else
            {
                JObject combined = new JObject();
                combined["value"] = new JArray();
                return combined;
            }
        }

        private bool IsSingleEntityRequest(string url)
        {
            // Remove query parameters and split the path
            var pathSegments = url.Split('?')[0].Trim('/').Split('/');

            if (pathSegments.Length == 0)
                return false; // No valid entity in path

            // Last segment in the URL
            var lastSegment = pathSegments.Last();
            var secondLastSegment = pathSegments.Length > 1 ? pathSegments[pathSegments.Length - 2] : null;

            // Check if the last segment is a collection (e.g., "Observations", "Datastreams")
            bool isCollectionRequest = secondLastSegment != null && IsEntitySet(lastSegment);

            return !isCollectionRequest;
        }

        // Helper method to check if the last segment is a known collection (plural entity)
        private bool IsEntitySet(string segment)
        {
            var entitySets = new HashSet<string>
			{
				"Things", "Datastreams", "Observations", "Locations",
				"Sensors", "FeaturesOfInterest", "ObservedProperties", "HistoricalLocations"
			};

            return entitySets.Contains(segment);
        }

    }
}
