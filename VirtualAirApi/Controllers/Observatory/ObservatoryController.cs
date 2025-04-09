
using Dapper.Contrib.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using VirtualAirApi.Common;
using VirtualAirApi.Controllers.Authentication;

namespace VirtualAirApi.Controllers.Observatory
{
	[ApiController]
	[Route("[controller]")]

	public class ObservatoryController : ControllerBase
	{
		private readonly ILogger<ObservatoryController> _logger;

		public ObservatoryController(ILogger<ObservatoryController> logger)
		{
			_logger = logger;
		}

		[HttpGet("{id}")]
		public async Task<Models.Observatory> GetObservatory(long id)
		{
			_logger.LogDebug($"Observatory queried at {DateTime.UtcNow.ToLongTimeString()}");
			using IDbConnection con = Db.GetConnection();
			return await con.GetAsync<Models.Observatory>(id);
		}

		[HttpGet]
		public async Task<IEnumerable<Models.Observatory>> GetObservatories()
		{
			_logger.LogDebug($"Observatories queried at {DateTime.UtcNow.ToLongTimeString()}");
			using IDbConnection con = Db.GetConnection();
			return await con.GetAllAsync<Models.Observatory>();
		}
		
		[HttpPost]
		[Authorize]
		public async Task<ActionResult<Models.Observatory>> PostObservatory(Models.Observatory Observatory)
		{
			_logger.LogDebug($" Post Observatory at {DateTime.UtcNow.ToLongTimeString()}");

			// Generate a 4-letter code for the observatory
			Observatory.code = await GenerateCode();

			using IDbConnection con = Db.GetConnection();
			var id = await con.InsertAsync<Models.Observatory>(Observatory);

			var insertedObservatory = await GetObservatory(id);

			return new OkObjectResult(insertedObservatory);
		}

		private async Task<string> GenerateCode()
		{
			var random = new Random();
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
			string code;

			using IDbConnection con = Db.GetConnection();
			var existingCodes = (await con.GetAllAsync<Models.Observatory>()).Select(o => o.code).ToHashSet();

			do
			{
				code = new string(Enumerable.Repeat(chars, 4)
					.Select(s => s[random.Next(s.Length)]).ToArray());
			} while (existingCodes.Contains(code));

			return code;
		}

	}
}
