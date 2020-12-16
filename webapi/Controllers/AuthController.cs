using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
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
    // apenas mostrar da forma MAIS DIDÁTICA POSSÍVEL o funcionamento 
    // da configuração e geração de Token JWT.
    // Recomendo estudar bem a implementação da rotina (que já é 
    // complexa por si só) antes de aplicar esta lógica em alguma 
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
        
        private List<Claim> GerarClaims(Usuario usuario)
        {
            return new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.UniqueName, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            };
        }

        private async Task AtualizarUsuario(Usuario usuario, string novoRefreshToken, DateTime? novaDataExpiracao)
        {
            usuario.RefreshToken  = novoRefreshToken;
            usuario.DataExpiracao = novaDataExpiracao;
            await _context.SaveChangesAsync();
        }

        public TokenDTO GerarTokenDTO(string token, string refresh, DateTime dataCriacao, DateTime dataExpiracao)
        {
            return new TokenDTO
            {
                Autenticado  = true,
                AccessToken  = token,
                RefreshToken = refresh,
                Criacao      = dataCriacao,
                Expiracao    = dataExpiracao,
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
            var claims  = GerarClaims(usuario);
            var token   = _geradorDeToken.GerarToken(claims);
            var refresh = _geradorDeToken.GerarRefreshToken();

            // Atualiza a tabela de usuários com o novo refresh token + data de expiração
            await AtualizarUsuario(
                usuario,
                refresh,
                DateTime.Now.AddDays(_config.DaysToRefresh));

            // Determina a data de criação e expiração do token e ...
            var dataCriacao   = DateTime.Now;
            var dataExpiracao = dataCriacao.AddMinutes(_config.MinutesToExpire);

            // ... retorna o DTO com tudo OK
            var resultado = GerarTokenDTO(
                token, 
                refresh, 
                dataCriacao, 
                dataExpiracao);

            return Ok(resultado);
        }

        /// <summary>
        /// Realiza a renovação de um token expirado baseado
        /// no refreshtoken gerado na primeira autentaicação
        /// </summary>
        [HttpPost]
        [Route("renovar")]
        public async Task<IActionResult> Renovar(RefreshDTO refreshDTO)
        {
            // Recupera o token, o refreshtoken e as claimns 
            var token     = refreshDTO.AccessToken;
            var refresh   = refreshDTO.RefreshToken;
            var principal = _geradorDeToken.ObterClaimPrincipal(token);
        
            // Recupera o ID do usuário...
            if (!int.TryParse(principal.Identity.Name, out var idUsuario))
            {
                return BadRequest("Token inválido");
            }

            // ... e busca do banco de dados
            var usuario = await _context.Usuarios.FindAsync(idUsuario);

            // Determina que tudo está ok com o refresh token informado  
            if (usuario == null || usuario.RefreshToken != refresh || usuario.DataExpiracao < DateTime.Now)
            {
                return BadRequest("É necessário realizar uma nova autenticação");
            }

            // Gera novos tokens
            token   = _geradorDeToken.GerarToken(principal.Claims);
            refresh = _geradorDeToken.GerarRefreshToken();

            // Atualiza a tabela de usuários com o novo refresh token + data de expiração
            await AtualizarUsuario(
                usuario, 
                refresh, 
                DateTime.Now.AddDays(_config.DaysToRefresh)
            );

            // Determina data de criação e expiração do token e ...
            var dataCriacao   = DateTime.Now;
            var dataExpiracao = dataCriacao.AddMinutes(_config.MinutesToExpire);

            // ... retorna o um novo DTO atualizado com tudo OK
            var resultado = GerarTokenDTO(
                token, 
                refresh, 
                dataCriacao, 
                dataExpiracao);

            return Ok(resultado);
        }

        /// <summary>
        /// Apenas revoga o refresh token do 
        /// usuário em um eventual logoff
        /// </summary>
        [HttpGet]
        [Route("revogar")]
        [Authorize]
        public async Task<IActionResult> Revogar()
        {
            // Recupera o ID do usuário logado...
            var idUsuario = int.Parse(User.Identity.Name);
            var usuario   = await _context.Usuarios.FindAsync(idUsuario);
            await AtualizarUsuario(usuario, null, null);
            return NoContent();
        }

    }
}