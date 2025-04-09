using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using VirtualAirApi.Controllers.Authentication;

namespace VirtualAirApi.Common
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
	public class AuthorizeAttribute : Attribute, IAuthorizationFilter
	{
		public void OnAuthorization(AuthorizationFilterContext context)
		{
			var user = context.HttpContext.Items["User"] as Models.User;
			if (user == null)
			{
				// not logged in
				context.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
			}
		}
	}
}
