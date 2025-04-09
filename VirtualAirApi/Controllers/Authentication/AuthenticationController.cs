using System.Data;
using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Mvc;

using VirtualAirApi.Common;

namespace VirtualAirApi.Controllers.Authentication
{
	[ApiController]
	[Route("[controller]")]
	public class AuthenticationController : ControllerBase	
	{
		[HttpPost]
		[Route("action")]
		public IActionResult Login([FromBody] Models.UserLogin user)
		{
			using IDbConnection con = Db.GetConnection();
			var dbUser = con.QueryFirstOrDefault<Models.User>(@"select * from ""user"" where email=:email",
				new { email = user.email });

			if (dbUser == null) return Problem("Wrong email/password");

			if (!Security.Verify(user.password, dbUser.salt, dbUser.password))
				return new UnauthorizedObjectResult("Wrong email/password");


			var claims = new List<Claim>
			{
				new Claim("id", dbUser.id.ToString()),
			};

			var userOut = new Models.UserOut()
			{
				id = dbUser.id,
				email = user.email,
				jwt = Security.GetToken(claims)
			};

			HttpContext.Items["User"] = userOut;

			return new OkObjectResult(userOut);

		}
	}
}
