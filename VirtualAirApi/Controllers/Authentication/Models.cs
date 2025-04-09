using Dapper.Contrib.Extensions;
using System.Text.Json.Serialization;

namespace VirtualAirApi.Controllers.Authentication
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public class Models
	{
		public class UserLogin
		{

			public string email { get; set; }

			public string password { get; set; }

		}
		public class UserOut
		{
			public Guid id { get; set; }
			public string email { get; set; }
			public string jwt { get; set; }
		}

		[Dapper.Contrib.Extensions.Table("user")]
		public class User
		{
			[ExplicitKey]
			public Guid id { get; set; }
			public string email { get; set; }
			[JsonIgnore]
			public string password { get; set; }
			[JsonIgnore]
			public string salt { get; set; }
		}
	}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}
