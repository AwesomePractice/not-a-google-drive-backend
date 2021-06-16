using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using not_a_google_drive_backend.DTO.Request;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace not_a_google_drive_backend.Tools
{
    public class AuthenticationManager
    {
        public static object GenerateJWT(Credentials cred, string userId, string pwdHash, string pwdSalt)
        {
            var identity = GetIdentity(cred, userId, pwdHash, pwdSalt);
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


        private static ClaimsIdentity GetIdentity(Credentials cred, string userId, string pwdHash, string pwdSalt)
        {
            byte[] byteArr = Encoding.ASCII.GetBytes(pwdSalt);
            string passwordHashed = PasswordManager.GeneratePasswordHash(cred.Password, byteArr);

            if (passwordHashed == pwdHash)
            {
                var claims = new List<Claim>
                {
                    new Claim("id", userId),
                    new Claim(ClaimsIdentity.DefaultNameClaimType, cred.Login),
                    new Claim(ClaimsIdentity.DefaultRoleClaimType, cred.Role)
                };
                ClaimsIdentity claimsIdentity =
                new ClaimsIdentity(claims, "Token", ClaimsIdentity.DefaultNameClaimType,
                    ClaimsIdentity.DefaultRoleClaimType);
                return claimsIdentity;
            }
            return null;
        }

        public static ObjectId GetUserId(ClaimsPrincipal User)
        {
            return new ObjectId(User.FindFirst("id").Value);
        }

        internal static DatabaseModule.VO.GoogleBucketConfigData GoogleBucketConfigData(GoogleBucketConfigData data)
        {
            return new DatabaseModule.VO.GoogleBucketConfigData()
            {
                Id = ObjectId.GenerateNewId(),
                ClientId = data.ClientId,
                Secret = data.Secret,
                Email = data.Email,
                ProjectId = data.ProjectId,
                SelectedBucket = data.SelectedBucket
            };
        }

        internal static async Task<GoogleBucketConfigData> GetGoogleBucketDefault()
        {
            string fileName = "google_bucket.json";
            string jsonString = await File.ReadAllTextAsync(fileName);
            GoogleBucketConfigData data = JsonSerializer.Deserialize<GoogleBucketConfigData>(jsonString);
            return data;
        }
    }
}
