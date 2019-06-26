using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EchoBot1.Bots
{
    public static class Helper
    {
        public static ClaimsPrincipal Validate(string accessToken)
        {
            ClaimsPrincipal result = null;
            Task.Run(async () =>
            {
                string tenantId = "74c3a4b1-a2a5-4e48-9d7b-434f36d335ed";
                string stsDiscoveryEndpoint = $"https://login.microsoftonline.com/{tenantId}/.well-known/openid-configuration";

                ConfigurationManager<OpenIdConnectConfiguration> configManager = new ConfigurationManager<OpenIdConnectConfiguration>(stsDiscoveryEndpoint, new OpenIdConnectConfigurationRetriever());

                OpenIdConnectConfiguration config = await configManager.GetConfigurationAsync();

                TokenValidationParameters validationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    IssuerSigningKeys = config.SigningKeys,
                    ValidateLifetime = false
                };

                JwtSecurityTokenHandler tokendHandler = new JwtSecurityTokenHandler();

                Microsoft.IdentityModel.Tokens.SecurityToken jwt = new JwtSecurityToken();

                result = tokendHandler.ValidateToken(accessToken, validationParameters, out jwt);

            }).Wait();

            return result;
        }
    }
}
