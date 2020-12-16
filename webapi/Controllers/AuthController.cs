using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webapi.Data;
using webapi.DTO;
using webapi.Models;
using webapi.Services;
using webapi.Services.Config;
using webapi.Services.Interfaces;

namespace webapi.Controllers
{

    // Não me preocupei em arquitetura. A ideia deste projeto é 
    // apenas mostrar o funcionamento do serviço de geração de Token.
    // Recomendo estudar bem o funcionamento da rotina, que já é 
    // complexa por si só, antes de aplicar esta lógica em alguma 
    // arquitetura mais complexa.

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ConfiguracaoDoToken _config;
        private readonly IGeradorDeTokenService _geradorDeToken;

        public AuthController(DataContext context, ConfiguracaoDoToken config, IGeradorDeTokenService geradorDeToken)
        {
            _context = context;
            _config  = config;
            _geradorDeToken = geradorDeToken;
        }
        
        private static List<Claim> GerarClaimsUsuario(Usuario usuario)
        {
            return new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sid, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim(JwtRegisteredClaimNames.UniqueName, usuario.Nome),
            };
        }

        /// <summary>
        /// Apenas um xemplo de método que só poderá 
        /// ser acessado após a autenticação do usuário
        /// </summary>
        [HttpGet]
        [Route("buscar")]
        [Authorize]
        public async Task<IActionResult> Get() => Ok(await _context.Usuarios.ToListAsync());
        
        /// <summary>
        /// Realiza a autenticação gerando um token
        /// e um refresh token para o usuário
        /// </summary>
        [HttpPost]
        [Route("autenticar")]
        public async Task<IActionResult> Autenticar(AuthDTO auth)
        {
            // Criptografa a senha do DTO
            var hashSenha = HashService.GerarHash(auth.Senha);

            // Autentica o usuário
            var usuario = await _context
                .Usuarios
                .FirstOrDefaultAsync(u => u.Email == auth.Email && u.Senha == hashSenha);

            if (usuario == null)
            {
                return NotFound();
            }

            // Gera as claims, token de autenticação e refresh token
            var claims  = GerarClaimsUsuario(usuario);
            var token   = _geradorDeToken.GerarToken(claims);
            var refresh = _geradorDeToken.GerarRefreshToken();

            // Persiste o refresh token + data de expiração no DB
            usuario.RefreshToken  = refresh;
            usuario.DataExpiracao = DateTime.Now.AddDays(_config.DaysToRefresh);
            await _context.SaveChangesAsync();

            // Popula data de criação e expiração do token e ...
            var dataCriacao   = DateTime.Now;
            var dataExpiracao = dataCriacao.AddMinutes(_config.MinutesToExpire);

            // ... retorna o DTO com tudo OK
            return Ok(new TokenDTO
            {
                Autenticado  = true,
                AccessToken  = token,
                RefreshToken = refresh,
                Criacao      = dataCriacao,
                Expiracao    = dataExpiracao,
            });
        }

    }
}