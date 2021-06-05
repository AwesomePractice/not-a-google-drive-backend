using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace not_a_google_drive_backend.Tools
{
    public class PasswordManager
    {

        private const string PasswordStrengthRegex = "(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[^A-Za-z0-9])(?=.{8,})";

        public static string GeneratePasswordHash(string password, byte[] salt)
        {
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8));
            return hashed;
        }

        public static string GeneratePasswordHash(string password, string salt)
        {
            var saltBytes = Encoding.ASCII.GetBytes(salt);
            return GeneratePasswordHash(password, saltBytes);
        }

        public static string GenerateSalt_128()
        {
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            for (int i = 0; i < salt.Length; i++)
            {
                salt[i] = (byte)((int)salt[i] % 64);
            }
            return Convert.ToBase64String(salt);
        }

        /*
            The password is at least 8 characters long (?=.{8,}).
            The password has at least one uppercase letter(?=.*[A-Z]).
            The password has at least one lowercase letter(?=.*[a-z]).
            The password has at least one digit(?=.*[0-9]).
            The password has at least one special character([^A-Za-z0-9]).
        */
        public static bool ValidatePasswordStrength(string password)
        {
            Regex regex = new Regex(PasswordStrengthRegex);
            if (regex.Match(password).Success)
            {
                return true;
            }
            return false;
        }

    }
}
