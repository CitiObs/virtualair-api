using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VirtualAirDataApi.Models
{
	public class MultiDatastream
	{
		public int IotId { get; set; }
		public string IotSelfLink { get; set; }
		public string ThingIotNavigationLink { get; set; }
		public string SensorIotNavigationLink { get; set; }
		public string ObservedPropertyIotNavigationLink { get; set; }
		public string ObservationsIotNavigationLink { get; set; }
		public string Name { get { return this + _nameExtension; } set { } }
		public string Description { get { return this + _descriptionExtension; } set { } }
		public string ObservationType { get { return "http://www.opengis.net/def/observationType/OGC-OM/2.0/OM_ComplexObservation"; } }
		public static readonly List<string> MultiObservationDataTypes = new List<string>
		{
			"http://www.opengis.net/def/observationType/OGC-OM/2.0/OM_Measurement",
			"http://www.opengis.net/def/observationType/OGC-OM/2.0/OM_Measurement",
			"http://www.opengis.net/def/observationType/OGC-OM/2.0/OM_Measurement",
			"http://www.opengis.net/def/observationType/OGC-OM/2.0/OM_Measurement"
		};
		public static readonly List<UnitOfMeasurement> UnitOfMeasurements = new List<UnitOfMeasurement>
		{
			new UnitOfMeasurement
			{
				Name = "ug.m-3",
				Symbol = "ug.m-3",
				Definition = "http://dd.eionet.europa.eu/vocabulary/uom/concentration/ug.m-3"
			},
			new UnitOfMeasurement
			{
				Name = "ug.m-3",
				Symbol = "ug.m-3",
				Definition = "http://dd.eionet.europa.eu/vocabulary/uom/concentration/ug.m-3"
			},
			new UnitOfMeasurement
			{
				Name = "ug.m-3",
				Symbol = "ug.m-3",
				Definition = "http://dd.eionet.europa.eu/vocabulary/uom/concentration/ug.m-3"
			},
			new UnitOfMeasurement
			{
				Name = "ug.m-3",
				Symbol = "ug.m-3",
				Definition = "http://dd.eionet.europa.eu/vocabulary/uom/concentration/ug.m-3"
			}
		};
		public ObservedArea? ObservedArea { get; set; }
		public string? PhenomenonTime { get; set; }
		public string? ResultTime { get; set; }
		public Dictionary<string, object> Properties
		{
			get
			{
				return new Dictionary<string, object>
				{
					{ "aggregateAmount", 1 },
					{ "aggregateFor", $"/Datastreams({IotId})" },
					{ "aggregateSource.Datastream@iot.id", IotId },
					{ "aggregateSource.Datastream@iot.navigationLink", $"https://airquality-frost.k8s.ilt-dmz.iosb.fraunhofer.de/v1.1/Datastreams({IotId})" },
					{ "aggregateUnit", "Hours" }
				};
			}
		}

		private const string _nameExtension = "[Hours]";
		private const string _descriptionExtension = "-Average hourly data";
	}

	public class UnitOfMeasurement
	{
		public string Name { get; set; }
		public string Symbol { get; set; }
		public string Definition { get; set; }
	}

	public class ObservedArea
	{
		public string Type { get; set; }
		public List<List<List<double>>> Coordinates { get; set; }
	}

	public class Observatory
	{
		public int id { get; set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		public string baseurl { get; set; }
		public string version { get; set; }
		public string code { get; set; }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		public string? extension { get; set; }
		public string? formatnavlinks { get; set; }

	}

	public class Thing
	{
		[JsonPropertyName("@iot.id")]
		public int IotId { get; set; }

		[JsonPropertyName("@iot.selfLink")]
		public string IotSelfLink { get; set; }

		public string name { get; set; }
		public string description { get; set; }

		[JsonPropertyName("Locations@iot.navigationLink")]
		public string LocationsIotNavigationLink { get; set; }

		[JsonPropertyName("Datastreams@iot.navigationLink")]
		public string DatastreamsIotNavigationLink { get; set; }

		[JsonPropertyName("HistoricalLocations@iot.navigationLink")]
		public string HistoricalLocationsIotNavigationLink { get; set; }

		public Properties properties { get; set; }
	}

	public class Location
	{
		[JsonPropertyName("@iot.id")]
		public int IotId { get; set; }

		[JsonPropertyName("@iot.selfLink")]
		public string IotSelfLink { get; set; }

		public string name { get; set; }
		public string description { get; set; }
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public double? Elevation { get; set; }

		[JsonPropertyName("Things@iot.navigationLink")]
		public string ThingsIotNavigationLink { get; set; }

		[JsonPropertyName("HistoricalLocations@iot.navigationLink")]
		public string HistoricalLocationsIotNavigationLink { get; set; }
	}

	public class HistoricalLocation
	{
		[JsonPropertyName("@iot.id")]
		public int IotId { get; set; }

		[JsonPropertyName("@iot.selfLink")]
		public string IotSelfLink { get; set; }

		public DateTime Time { get; set; }
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public double? Elevation { get; set; }

		[JsonPropertyName("Thing@iot.navigationLink")]
		public string ThingIotNavigationLink { get; set; }
	}

	public class Sensor
	{
		[JsonPropertyName("@iot.id")]
		public int IotId { get; set; }

		[JsonPropertyName("@iot.selfLink")]
		public string IotSelfLink { get; set; }

		public string name { get; set; }
		public string description { get; set; }

		[JsonPropertyName("Datastreams@iot.navigationLink")]
		public string DatastreamsIotNavigationLink { get; set; }
	}

	public class ObservedProperty
	{
		[JsonPropertyName("@iot.id")]
		public int IotId { get; set; }

		[JsonPropertyName("@iot.selfLink")]
		public string IotSelfLink { get; set; }

		public string name { get; set; }
		public string description { get; set; }

		[JsonPropertyName("Datastreams@iot.navigationLink")]
		public string DatastreamsIotNavigationLink { get; set; }
	}

	public class Datastream
	{
		[JsonPropertyName("@iot.id")]
		public int IotId { get; set; }

		[JsonPropertyName("@iot.selfLink")]
		public string IotSelfLink { get; set; }

		public string name { get; set; }
		public string description { get; set; }

		// Foreign key properties
		public int ThingId { get; set; }
		public int SensorId { get; set; }
		public int ObservedPropertyId { get; set; }

		[JsonPropertyName("Thing@iot.navigationLink")]
		public string ThingIotNavigationLink { get; set; }

		[JsonPropertyName("Sensor@iot.navigationLink")]
		public string SensorIotNavigationLink { get; set; }

		[JsonPropertyName("ObservedProperty@iot.navigationLink")]
		public string ObservedPropertyIotNavigationLink { get; set; }

		[JsonPropertyName("Observations@iot.navigationLink")]
		public string ObservationsIotNavigationLink { get; set; }

		public Thing Thing { get; set; }
		public Sensor Sensor { get; set; }
		public ObservedProperty ObservedProperty { get; set; }
		public ICollection<Observation> Observations { get; set; }
	}

	public class Observation
	{
		[JsonPropertyName("@iot.id")]
		public int IotId { get; set; }

		[JsonPropertyName("@iot.selfLink")]
		public string IotSelfLink { get; set; }

		public DateTime PhenomenonTime { get; set; }
		public DateTime ResultTime { get; set; }
		public double Result { get; set; }

		// Foreign key properties
		public int DatastreamId { get; set; }

		[JsonPropertyName("Datastream@iot.navigationLink")]
		public string DatastreamIotNavigationLink { get; set; }

		public Datastream Datastream { get; set; }
	}

	public class Properties
	{
		// Define properties as needed
	}
}
