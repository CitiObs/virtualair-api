using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VirtualAirDataApi.Handlers;

namespace VirtualAirDataApi.Controllers
{
    public class MultiDatastreamsController : BaseController
    {

        [HttpGet("v1.1/MultiDatastreams")]
        public async Task<JObject> GetMultiDatastreams()
        {
            var filter = HttpContext.Request.Query["$filter"].FirstOrDefault();
            var expand = HttpContext.Request.Query["$expand"].FirstOrDefault();

            if (!IsValidFilter(filter) || !IsValidExpand(expand))
            {
                return await Task.FromResult(new JObject(new JProperty("Error", "Invalid query parameters")));
            }

            return await ProcessFilterAndExpandAsync(filter, expand);
        }

        private bool IsValidFilter(string filter)
        {
            var aggregateUnitPattern = @"properties/aggregateUnit eq 'Hours'";
            var aggregateForPattern = @"properties/aggregateFor eq '/Datastreams\([^)]+\)'";

            return !string.IsNullOrEmpty(filter) &&
                   Regex.IsMatch(filter, aggregateUnitPattern) &&
                   Regex.IsMatch(filter, aggregateForPattern);
        }

        private bool IsValidExpand(string expand)
        {
            return !string.IsNullOrEmpty(expand) && expand.StartsWith("Observations");
        }

        private async Task<JObject> ProcessFilterAndExpandAsync(string filter, string expand)
        {
            var datastreamId = ExtractDatastreamId(filter);
            var querystring = PrepareQueryString(expand);
            var datastream = await ProcessRequest($"/Datastreams({datastreamId})", "");

            var allObservations = await GetAllObservations(datastream, $"/Datastreams({datastreamId})/Observations", querystring);
            var phenomenonTime = ExtractPhenomenonTime(querystring);

            return CreateParentObject(datastreamId, datastream, phenomenonTime, allObservations);
        }

        private string ExtractDatastreamId(string filter)
        {
            return Regex.Match(filter, @"(?<=/Datastreams\().+?(?=\))").Value;
        }

        private string PrepareQueryString(string expand)
        {
            var trimmedExpand = Regex.Replace(expand, @"Observations\(\$filter=(.*)\)", "Observations?$filter=$1");
            return trimmedExpand.Replace("Observations", "");
        }

        private string ExtractPhenomenonTime(string querystring)
        {
            var match = Regex.Match(querystring, @"phenomenonTime ge (.*?) and phenomenonTime le (.*?)$");
            return match.Success
                ? $"{match.Groups[1].Value}/{match.Groups[2].Value}"
                : "2018-01-01T23:00:00Z/2024-09-11T23:00:00Z"; // Default value if not found
        }

        private JObject CreateParentObject(string datastreamId, JObject datastream, string phenomenonTime, JArray allObservations)
        {
            return new JObject
            {
                ["value"] = new JArray
                {
                    new JObject
                    {
                        ["@iot.selfLink"] = $"{VAENDPOINTURL}/MultiDatastreams({datastreamId})",
                        ["@iot.id"] = datastreamId,
                        ["name"] = $"{datastream["name"]} [ 1 Hours ]",
                        ["description"] = $"{datastream["description"]}  aggregated per 1 hours",
                        ["observationType"] = "http://www.opengis.net/def/observationType/OGC-OM/2.0/OM_ComplexObservation",
                        ["multiObservationDataTypes"] = new JArray
                        {
                            "http://www.opengis.net/def/observationType/OGC-OM/2.0/OM_Measurement",
                            "http://www.opengis.net/def/observationType/OGC-OM/2.0/OM_Measurement",
                            "http://www.opengis.net/def/observationType/OGC-OM/2.0/OM_Measurement",
                            "http://www.opengis.net/def/observationType/OGC-OM/2.0/OM_Measurement"
                        },
                        ["unitOfMeasurements"] = new JArray
                        {
                            CreateUnitOfMeasurement("ug.m-3"),
                            CreateUnitOfMeasurement("ug.m-3"),
                            CreateUnitOfMeasurement("ug.m-3"),
                            CreateUnitOfMeasurement("ug.m-3")
                        },
                        ["phenomenonTime"] = phenomenonTime,
                        ["properties"] = new JObject
                        {
                            ["aggregateAmount"] = 1,
                            ["aggregateFor"] = $"/Datastreams({datastreamId})",
                            ["aggregateSource.Datastream@iot.id"] = datastreamId,
                            ["aggregateSource.Datastream@iot.navigationLink"] = $"{VAENDPOINTURL}/Datastreams({datastreamId})",
                            ["aggregateUnit"] = "Hours"
                        },
                        ["Observations"] = allObservations
                    }
                }
            };
        }

        private JObject CreateUnitOfMeasurement(string symbol)
        {
            return new JObject
            {
                ["name"] = symbol,
                ["symbol"] = symbol,
                ["definition"] = "http://dd.eionet.europa.eu/vocabulary/uom/concentration/ug.m-3"
            };
        }

        private async Task<JArray> GetAllObservations(JObject datastream, string url, string queryString)
        {
            var allResults = new JArray();
            string nextLink = null;

            var result = await ProcessRequest(url, queryString);

            do
            {
                var valueArray = (JArray)result["value"];
                if (valueArray != null)
                {
                    allResults.Merge(valueArray);
                }

                nextLink = result["@iot.nextLink"]?.ToString();
                if (nextLink != null)
                {
                    result = await MakeHttpRequest(UpdateNextLink(nextLink));
                }
            } while (nextLink != null);

            return  CalculateHourlyAverages(allResults) ;
        }

        private string UpdateNextLink(string nextLink)
        {
            var uri = new Uri(nextLink);
            var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
            int skip = int.Parse(queryParams.Get("$skip") ?? "0");
            skip += 300;
            queryParams.Set("$skip", skip.ToString());
            queryParams.Set("$top", "300");

            return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}?{queryParams}";
        }

        private JArray CalculateHourlyAverages(JArray allResults)
        {
            var groupedResults = allResults
                .GroupBy(item => DateTime.Parse(item["phenomenonTime"].ToString()).ToString("yyyy-MM-ddTHH:00:00Z"))
                .Select(group =>
                {
                    var startTime = DateTime.Parse(group.Key);
                    var endTime = startTime.AddHours(1);
                    var resultCount = group.Count();
                    return new JObject
                    {
                        ["phenomenonTime"] = $"{startTime:yyyy-MM-ddTHH:00:00Z}/{endTime:yyyy-MM-ddTHH:00:00Z}",
                        ["actual"] = group.Average(item => (double)item["result"]),
                        ["min"] = group.Min(item => (double)item["result"]),
                        ["max"] = group.Max(item => (double)item["result"]),
                        ["dev"] = CalculateStandardDeviation(group.Select(item => (double)item["result"])),
                        ["parameters"] = new JObject
                        {
                            ["resultCount"] = resultCount
                        }
                    };
                })
                .OrderByDescending(item => DateTime.Parse(item["phenomenonTime"].ToString().Split('/')[0]));

            return new JArray(groupedResults);
        }

        private double CalculateStandardDeviation(IEnumerable<double> values)
        {
            double avg = values.Average();
            double sumOfSquaresOfDifferences = values.Select(val => (val - avg) * (val - avg)).Sum();
            return Math.Sqrt(sumOfSquaresOfDifferences / values.Count());
        }

        private async Task<JObject> MakeHttpRequest(string url)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetStringAsync(url);
                return JObject.Parse(response);
            }
        }
    }
}
