using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace VirtualAirApi.Common
{
	public static class Security
	{
		public static string GenerateSalt()
		{
			return Guid.NewGuid().ToString().Substring(0, 8);
		}


		public static bool Verify(string password, string salt, string saltedPassword)
		{
			var saltedUserPassword = GenerateSha256Password(password, salt);
			return saltedUserPassword == saltedPassword;
		}

		public static string GenerateSha256Password(string password, string salt)
		{
			return sha256(sha256(salt) + sha256(password));
		}

		public static string GetToken(List<Claim> claims)
		{
			var Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Common.AppSettings.SecurityKey));

			var Token = new JwtSecurityToken(
				Common.AppSettings.SecurityTokenIssuer,
				Common.AppSettings.SecurityTokenIssuer,
				claims,
				expires: null, //DateTime.Now.AddDays(30.0),
				signingCredentials: new SigningCredentials(Key, SecurityAlgorithms.HmacSha256)
			);

			return new JwtSecurityTokenHandler().WriteToken(Token);
		}

		private static string sha256(string password)
		{
			byte[] input, output;
			input = System.Text.Encoding.UTF8.GetBytes(password);
			SHA256 hashAlgo = SHA256.Create();
			output = hashAlgo.ComputeHash(input);
			return Convert.ToBase64String(output);
		}

		public static bool IsValidEmail(string email)
		{
			var trimmedEmail = email.Trim();

			if (trimmedEmail.EndsWith("."))
			{
				return false; // suggested by @TK-421
			}
			try
			{
				var addr = new System.Net.Mail.MailAddress(email);
				return addr.Address == trimmedEmail;
			}
			catch
			{
				return false;
			}
		}
	}

}
