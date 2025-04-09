using Npgsql;
using System.Data;

namespace VirtualAirApi.Common
{
	public class Db
	{
		public static string ConnectionString { get; set; }
		public static IDbConnection GetConnection()
		{
			return new NpgsqlConnection(ConnectionString);
		}
	}

}
