using Dapper.Contrib.Extensions;

namespace VirtualAirApi.Controllers.Observatory
{
	public class Models
	{
		[Dapper.Contrib.Extensions.Table("observatory")]
		public class Observatory
		{
			[Key]
			public int id { get; set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
			public string baseurl { get; set; }
			public string version { get; set; }
			public string code { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
			public string? extension { get; set; }
			public string? formatnavlinks { get; set; }
			

		}
	}
}
