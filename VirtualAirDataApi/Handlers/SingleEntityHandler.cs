using Newtonsoft.Json.Linq;
using System;
using System.Text.RegularExpressions;
using System.Web;
using VirtualAirDataApi.Handlers;
using VirtualAirDataApi.Models;

namespace VirtualAirDataApi.Handlers
{
	public class SingleEntityHandler : EntityHandlerBase
	{
		private readonly HttpClient _client = new HttpClient();

		public async Task<JObject> GetSingleEntity(string url, List<Observatory> observatories, string querystring, bool isSingleEntity)
		{
			var (endpointCode, actualId) = ExtractEndpointCodeAndId(url);

			// If endpointCode or actualId is null, return an empty JObject
			if (endpointCode == null || actualId == null)
			{
				return new JObject();
			}

			var observatory = observatories.Find(o => o.code == endpointCode);
			if (observatory != null)
			{
				string newUrl = url.Replace(endpointCode + "_", "");
                // Handle numeric identifiers
                if (int.TryParse(actualId, out int numericId))
                {
                    newUrl = newUrl.Replace($"'{actualId}'", numericId.ToString());
                }
                return await QueryEndpoint(observatory, newUrl, querystring, isSingleEntity);
			}

			// Return empty object if no entity was found
			return  new JObject();
		}

		private (string, string) ExtractEndpointCodeAndId(string url)
		{
			// Extract the ID from the URL
			Match match = Regex.Match(url, @"\(([^)]+)\)");
			string id = match.Groups[1].Value;

			// Split the ID into parts
			string[] parts = id.Split('_');

			if (parts.Length >= 2)
			{
				string endpointCode = parts[0]; // "xxxx"
                //Remove all ' from endpointCode
                endpointCode = endpointCode.Replace("'", "");

                // Reassemble the actual ID from all parts except the first one
                string actualId = string.Join("_", parts, 1, parts.Length - 1); // "458_ogc_345345"
                                                                                //Remove all ' from actualId
                actualId = actualId.Replace("'", "");
                return (endpointCode, actualId);
			}

			return (null, null);
		}

		private async Task<JObject> QueryEndpoint(Observatory observatory, string url, string querystring, bool isSingleEntity)
		{
			try
			{
                querystring = HandleUrlExceptions(observatory.baseurl, querystring);

                string decodedUrl = HttpUtility.UrlDecode(observatory.baseurl + "/" + url + querystring);
				string response = await _client.GetStringAsync(decodedUrl);

				JObject json = JObject.Parse(response);
				JArray dataArray = (JArray)json["value"];

				ModifyIotId(json, observatory.code, observatory.baseurl);
				return json;
			}
			catch (Exception)
			{
                if (isSingleEntity)
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
		}
    }
}