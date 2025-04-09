using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;
using VirtualAirApi.Common;

namespace VirtualAirApi.Controllers.Authentication
{
	[ApiController]
	[Route("[controller]")]
	public class UserController : ControllerBase
	{
		[HttpPost]
		[Route("[action]")]
		public IActionResult Register([FromBody] Models.UserLogin user)
		{
			if (!Security.IsValidEmail(user.email)) return Problem("Not a valid email", statusCode: 400);
			if (user.password.Length < 4) return Problem("Too short password", statusCode: 400);

			using IDbConnection con = Db.GetConnection();

			Models.User u = new Models.User();
			u.id = Guid.NewGuid();
			u.salt = Security.GenerateSalt();
			u.password = Security.GenerateSha256Password(user.password, u.salt);
			u.email = user.email;

			//con.Insert<Models.User>(u);
			string sql = @"INSERT into ""user"" (id, salt, password, email) values (:id, :salt, :password, :email)";

			try
			{
				con.Execute(sql, u);
			}
			catch (Exception ex)
			{
				if (ex.Message.IndexOf("23505") > -1)
					return Problem("Email already registered", statusCode: 400);

				return Problem(ex.Message);
			}


			var newUser = con.QueryFirstOrDefault<Models.UserOut>(@"select * from ""user"" where email=:email",
				new { Email = user.email, Pwd = user.password });

			if (newUser == null) return Problem();
			

			var claims = new List<Claim>
			{
				new Claim("type", "User"),
			};
			newUser.jwt = Security.GetToken(claims);

			return new OkObjectResult(newUser);
		}


		[HttpPost]
		[Authorize]
		[Route("[action]")]
		public IActionResult Delete()
		{
			if (HttpContext.Items["User"] is Models.User user)
			{
				// Use 'user' here.

				using IDbConnection con = Db.GetConnection();

				//con.Insert<Models.User>(u);
				string sql = @"DELETE from ""user"" where id=:id ";

				try
				{
					con.Execute(sql, new { id = user.id });
				}
				catch (Exception ex)
				{
					return Problem(ex.Message);
				}

				return new OkResult();
			}
			else
			{
				return Problem();
			}


		}
	}
}
