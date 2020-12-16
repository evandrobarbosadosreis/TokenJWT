using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace webapi.Services.Config
{
    public static class StartupConfiguraToken
    {

        public static ConfiguracaoDoToken LerConfiguracaoDoToken(this IConfiguration configuration)
        {
                var sessao = configuration.GetSection("TokenConfigurations");
                var configurador = new ConfigureFromConfigurationOptions<ConfiguracaoDoToken>(sessao);
                var configuracao = new ConfiguracaoDoToken();
                configurador.Configure(configuracao);
                return configuracao;
        }

        public static void ConfigurarJWT(this IServiceCollection services, ConfiguracaoDoToken configuracoes)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuracoes.Secret));

            services.AddAuthentication(options => 
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options => 
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer   = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuer      = configuracoes.Issuer,
                    ValidAudience    = configuracoes.Audience,
                    IssuerSigningKey = key,
                    ValidateIssuerSigningKey = true,
                };
            });
        }

    }
}