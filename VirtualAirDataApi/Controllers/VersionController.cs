using Microsoft.AspNetCore.Mvc;

namespace VirtualAirDataApi.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class VersionController : ControllerBase
	{
		[HttpGet]
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public async Task<IActionResult> GetOk()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			return Ok("v1.24");
		}
	}
}
