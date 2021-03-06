using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace not_a_google_drive_backend.Tools
{
    public class AuthOptions
    {
        public const string ISSUER = "MyAuthServer"; 
        public const string AUDIENCE = "MyAuthClient"; 
        const string KEY = "mysupersecret_secretkey!123"; 
        public const int LIFETIME = 1440; // token is valid 24 hours
        public static SymmetricSecurityKey GetSymmetricSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(KEY));
        }
    }
}
