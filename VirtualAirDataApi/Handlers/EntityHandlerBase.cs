using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace VirtualAirDataApi.Handlers
{
	public abstract class EntityHandlerBase
	{
		protected const string VAENDPOINTURL = "https://api-virtualair.nilu.no/v1.1";
        protected string HandleUrlExceptions(string baseurl, string querystring)
        {
            if (baseurl == "https://api-samenmeten.rivm.nl/v1.0" && querystring.Contains("geography%27"))
            {
                querystring = HttpUtility.UrlDecode(querystring);
                querystring = querystring.Replace("geography'", "geography'SRID=4326;");
                querystring = HttpUtility.UrlEncode(querystring);
            }
            return querystring;
        }

        protected void ModifyIotId(JObject json, string code, string baseurl)
		{

			// Start the recursive update process
			UpdateIotIdAndLinks(json, code, baseurl);
		}

		private void UpdateIotIdAndLinks(JToken token, string code, string url)
		{
			if (token.Type == JTokenType.Object)
			{
				var obj = (JObject)token;
				foreach (var property in obj.Properties())
				{
					//Changing the @iot.id property to include the VirtualAir prefix
					if (property.Name == "@iot.id")
					{
						property.Value = code + "_" + property.Value.ToString();
					}
					else                // Check if the property is a navigation link
					if (property.Name.EndsWith("@iot.navigationLink") || property.Name.EndsWith("@iot.selfLink"))
					{
						// Extract the ID from the URL
						Match match = Regex.Match(property.Value.ToString(), @"\(([^)]+)\)");
						if (match.Success)
						{
							string id = match.Groups[1].Value;

							// Replace the ID in the URL with the code and ID
							string newUrl = property.Value.ToString().Replace("(" + id + ")", "('" + code + "_" + id + "')");

							// Replace the domain name in the URL
							newUrl = newUrl.Replace(url, VAENDPOINTURL);

							// Update the property value with the new URL
							property.Value = newUrl;
						}
					}

					else
					{
						UpdateIotIdAndLinks(property.Value, code, url);
					}
				}
			}
			else if (token.Type == JTokenType.Array)
			{
				var array = (JArray)token;
				foreach (var item in array)
				{
					UpdateIotIdAndLinks(item, code, url	);
				}
			}
		}

	}
}
