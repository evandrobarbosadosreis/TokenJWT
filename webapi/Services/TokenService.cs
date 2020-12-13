using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using webapi.Services.Configurations;
using webapi.Services.Interfaces;

namespace webapi.Services
{
    public class TokenService : ITokenService
    {
        private TokenConfiguration _config;

        public TokenService(TokenConfiguration configuration)
        {
            _config = configuration;
        }

        public ClaimsPrincipal ObterClaimPrincipal(string tokenExpirado)
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.Secret)),
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true
            };

            var handler = new JwtSecurityTokenHandler();

            SecurityToken securityToken;

            var principal = handler.ValidateToken(tokenExpirado, validationParameters, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;

            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCulture))
            {
                throw new SecurityTokenException("Token inválido");
            }

            return principal;
        }

        public string GerarRefreshToken()
        {
            var randonNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randonNumber);
                return Convert.ToBase64String(randonNumber);
            }
        }

        public string GerarToken(IEnumerable<Claim> claims)
        {
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.Secret));
            var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var options = new JwtSecurityToken(
                issuer: _config.Issuer,
                audience: _config.Audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(_config.MinutesToExpire),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(options);
        }
    }
}