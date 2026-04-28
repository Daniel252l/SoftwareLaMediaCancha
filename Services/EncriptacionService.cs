using System;
using System.Linq;
using System.Security.Cryptography;

namespace LaMediaCancha.Services
{
    public class EncriptacionService
    {
        private const int SaltSize = 32;
        private const int Iterations = 10_000;

        public string GenerarSalt()
        {
            var bytes = new byte[SaltSize];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        public string HashPassword(string password, string salt)
        {
            var saltBytes = Convert.FromBase64String(salt);
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations))
                return Convert.ToBase64String(pbkdf2.GetBytes(32));
        }

        public bool VerificarPassword(string password, string salt, string hashGuardado)
        {
            var hashNuevo = HashPassword(password, salt);
            return CryptographicEquals(hashNuevo, hashGuardado);
        }

        private static bool CryptographicEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }

        public bool ValidarFortalezaPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return false;
            if (password.Length < 8) return false;
            if (password.Any(char.IsWhiteSpace)) return false;
            if (!password.Any(char.IsUpper)) return false;
            if (!password.Any(char.IsLower)) return false;
            if (!password.Any(char.IsDigit)) return false;
            if (!password.Any(c => !char.IsLetterOrDigit(c))) return false;
            return true;
        }

        public bool EsPasswordTemporalVigente(DateTime? fecha)
            => fecha.HasValue && DateTime.Now <= fecha.Value.AddMonths(6);
    }
}