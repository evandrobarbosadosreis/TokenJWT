using System;
using System.Security.Cryptography;
using System.Text;

namespace webapi.Services
{
    public static class HashService
    {
        public static string GerarHash(string valor)
        {
            // Lógica de criptografia da senha vai aqui
            var sha256 = new SHA256CryptoServiceProvider();
            var input = Encoding.UTF8.GetBytes(valor);
            var hash  = sha256.ComputeHash(input);
            return BitConverter.ToString(hash);
        }
    }
}