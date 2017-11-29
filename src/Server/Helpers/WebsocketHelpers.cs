using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public static class WebsocketHelpers
    {
        public static async Task<SecurityToken> ValidateBearerTokenAsync(string auth)
        {
            if (auth?.StartsWith("?bearer_token=", StringComparison.OrdinalIgnoreCase) == true)
            {
                var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>("https://api.twitch.tv/api/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever());

                OpenIdConnectConfiguration openIdConfig = await configurationManager.GetConfigurationAsync(CancellationToken.None);

                TokenValidationParameters validationParameters = Startup.TKP;
                validationParameters.IssuerSigningKeys = openIdConfig.SigningKeys;

                var token = auth.Substring("?bearer_token=".Length).Trim();
                new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                return validatedToken ?? null;
            }
            return null;
        }

    }
}
