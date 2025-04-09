using Newtonsoft.Json.Linq;
using System.Web;
using VirtualAirDataApi.Models;

namespace VirtualAirDataApi.Handlers
{
    public class MultipleEntitiesHandler : EntityHandlerBase
    {
        private readonly HttpClient _client = new HttpClient();
        private const int _TOP = 300;

        public async Task<JObject> GetMultipleEntities(string url, List<Observatory> observatories, string querystring)
        {
            JObject combined = new JObject();
            combined["value"] = new JArray();

            var queryParameters = HttpUtility.ParseQueryString(querystring);
            (int top, int skip, int originalTop, int originalSkip) = AdjustTopAndSkip(queryParameters, observatories.Count);

            querystring = UpdateQueryString(queryParameters, top, skip);

            int maxSkip = 0;
            int numberOfEntities = 0;
            bool nextLinkFound = false;

            foreach (Observatory ob in observatories)
            {
                try
                {
                    string checkedqueryString = HandleUrlExceptions(ob.baseurl, querystring);

                    string decodedUrl = HttpUtility.UrlDecode(ob.baseurl + "/" + url + checkedqueryString);
                    string response = await _client.GetStringAsync(decodedUrl);

                    JObject json = JObject.Parse(response);
                    JArray dataArray = (JArray)json["value"];

                    if (json["@iot.nextLink"] != null)
                    {
                        var nextLinkParams = HttpUtility.ParseQueryString(new Uri(json["@iot.nextLink"].ToString()).Query);
                        int nextSkip = int.Parse(nextLinkParams["$skip"] ?? "0");
                        maxSkip = Math.Max(maxSkip, nextSkip);
                        nextLinkFound = true;
                    }

                    //If the json array has a property "@iot.count": 10071. I need to summarise this to to get to totl number of counts
                    if (json["@iot.count"] != null)
                    {
                        numberOfEntities += int.Parse(json["@iot.count"].ToString());
                    }

                    if (dataArray != null)
                    {
                        foreach (JObject item in dataArray)
                        {
                            ProcessItem(item, ob);
                            ((JArray)combined["value"]).Add(item);
                        }
                    }
                }
                catch (Exception e)
                {
                    // Ignore any errors and continue with the next URL
                    var t = e.Message;
                    continue;
                }
            }

            if (maxSkip > 0 && nextLinkFound)
            {
                queryParameters["$top"] = originalTop.ToString();
                queryParameters["$skip"] = (originalSkip + originalTop).ToString();
                combined["@iot.nextLink"] = $"{VAENDPOINTURL}/{url}?{queryParameters}";
            }

            //Only add this if the numberOfEntities > 0:
            if (numberOfEntities > 0)
            {
                combined["@iot.count"] = numberOfEntities;
            }

            return combined;
        }

        private (int top, int skip, int originalTop, int originalSkip) AdjustTopAndSkip(System.Collections.Specialized.NameValueCollection queryParameters, int observatoryCount)
        {
            int originalTop = int.Parse(queryParameters["$top"] ?? _TOP.ToString());
            int originalSkip = int.Parse(queryParameters["$skip"] ?? "0");

            int top = Math.Min(originalTop, _TOP);
            originalTop = (top / observatoryCount) * observatoryCount;
            originalSkip = (originalSkip / observatoryCount) * observatoryCount;

            top /= observatoryCount;
            int skip = originalSkip / observatoryCount;

            return (top, skip, originalTop, originalSkip);
        }

        private string UpdateQueryString(System.Collections.Specialized.NameValueCollection queryParameters, int top, int skip)
        {
            queryParameters["$top"] = top.ToString();
            queryParameters["$skip"] = skip.ToString();
            return "?" + queryParameters.ToString();
        }

        private void ProcessItem(JObject item, Observatory ob)
        {
            ModifyIotId(item, ob.code, ob.baseurl);

            if (item["properties"] == null)
            {
                item["properties"] = new JObject();
            }

            ((JObject)item["properties"])["origin"] = ob.baseurl;
        }
    }
}
