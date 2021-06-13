using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using not_a_google_drive_backend.DTO.Request;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace not_a_google_drive_backend.Tools
{
    public class AuthenticationManager
    {
        public static object GenerateJWT(Credentials cred, string pwdHash, string pwdSalt, ObjectId id)
        {
            var identity = GetIdentity(cred, pwdHash, pwdSalt, id);
            if (identity == null)
            {
                return null;
            }

            var now = DateTime.UtcNow;
            // создаем JWT-токен
            var jwt = new JwtSecurityToken(
                    issuer: AuthOptions.ISSUER,
                    audience: AuthOptions.AUDIENCE,
                    notBefore: now,
                    claims: identity.Claims,
                    expires: now.Add(TimeSpan.FromMinutes(AuthOptions.LIFETIME)),
                    signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            var response = new
            {
                access_token = encodedJwt,
                username = identity.Name
            };
            return response;
        }


        private static ClaimsIdentity GetIdentity(Credentials cred, string pwdHash, string pwdSalt, ObjectId id)
        {
            byte[] byteArr = Encoding.ASCII.GetBytes(pwdSalt);
            string passwordHashed = PasswordManager.GeneratePasswordHash(cred.Password, byteArr);

            if (passwordHashed == pwdHash)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimsIdentity.DefaultNameClaimType, cred.Login),
                    new Claim(ClaimsIdentity.DefaultRoleClaimType, cred.Role),
                    new Claim("UserId", id.ToString())
                };
                ClaimsIdentity claimsIdentity =
                new ClaimsIdentity(claims, "Token", ClaimsIdentity.DefaultNameClaimType,
                    ClaimsIdentity.DefaultRoleClaimType);
                return claimsIdentity;
            }
            return null;
        }
    }
}
