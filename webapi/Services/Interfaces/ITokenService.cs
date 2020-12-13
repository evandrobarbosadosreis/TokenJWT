using System.Collections.Generic;
using System.Security.Claims;

namespace webapi.Services.Interfaces
{    
    public interface ITokenService
    {
        string GerarRefreshToken();
        string GerarToken(IEnumerable<Claim> claims);
        ClaimsPrincipal ObterClaimPrincipal(string tokenExpirado);
    }
}