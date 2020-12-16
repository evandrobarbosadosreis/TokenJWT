using System;

namespace webapi.DTO
{
    public class TokenDTO
    {
        public bool Autenticado { get; set; }
        public DateTime Criacao { get; set; }
        public DateTime Expiracao { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}