using Dapper;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using VirtualAirApi.Controllers.Authentication;

namespace VirtualAirApi.Common
{
	public class JwtMiddleware
	{
		private readonly RequestDelegate _next;

		public JwtMiddleware(RequestDelegate next)
		{
			_next = next;
		}

		public async Task Invoke(HttpContext context)
		{
			var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

			if (token != null)
				attachUserToContext(context, token);

			await _next(context);
		}

		private void attachUserToContext(HttpContext context, string token)
		{
			try
			{
				var tokenHandler = new JwtSecurityTokenHandler();
				var key = Encoding.ASCII.GetBytes(AppSettings.SecurityKey);
				tokenHandler.ValidateToken(token, new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(key),
					ValidateIssuer = false,
					ValidateAudience = false,
					ValidateLifetime = false,
					// set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
					//ClockSkew = TimeSpan.Zero
				}, out SecurityToken validatedToken);

				var jwtToken = (JwtSecurityToken)validatedToken;
				var userId = jwtToken.Claims.First(x => x.Type == "id").Value;

				// attach user to context on successful jwt validation
				using IDbConnection con = Db.GetConnection();
				var dbUser = con.QueryFirstOrDefault<Models.User>(@"select * from ""user"" where id=:id",
					new { id = Guid.Parse(userId) });

				context.Items["User"] = dbUser;
			}
			catch
			{
				// do nothing if jwt validation fails
				// user is not attached to context so request won't have access to secure routes
			}
		}
	}
}
