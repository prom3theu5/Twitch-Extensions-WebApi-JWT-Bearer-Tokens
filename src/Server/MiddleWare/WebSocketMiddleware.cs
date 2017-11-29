using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;

namespace Server
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;

        public WebSocketMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        protected async Task<SecurityToken> ValidateBearerTokenAsync(string auth)
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


        /// <summary>
        /// Checks all requests. If a websocket request is found, goes on to check the QueryString. To send the Bearer token in the query string, append ?bearer_token=#tokenHere# to the end of the websocket url.
        /// <para>For Example: http://localhost:5000/?bearer_token=eybdhjhjsahjdhjkashdkjhaskjdhkjashdkjh......</para>
        /// </summary>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                await _next.Invoke(context);
                return;
            }

            if (!context.Request.QueryString.HasValue)
            {
                context.Response.StatusCode = 401;
                return;
            }

            var token = await ValidateBearerTokenAsync(context.Request.QueryString.Value);
            if (token == null)
            {
                context.Response.StatusCode = 401;
                return;
            }

            await ProcessRequest(context);
        }

        #region Websocket Example

        private async Task ProcessRequest(HttpContext context)
        {
            var ct = context.RequestAborted;
            var ws = await context.WebSockets.AcceptWebSocketAsync();
            await Task.WhenAll(WriteTask(ws), ReadTask(ws));
        }

        // MUST read if we want the socket state to be updated
        private async Task ReadTask(WebSocket ws)
        {
            var buffer = new ArraySegment<byte>(new byte[1024]);
            while (true)
            {
                await ws.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                if (ws.State != WebSocketState.Open) break;
            }
        }

        private async Task WriteTask(WebSocket ws)
        {
            while (true)
            {
                var timeStr = DateTime.UtcNow.ToString("MMM dd yyyy HH:mm:ss.fff UTC", CultureInfo.InvariantCulture);
                var buffer = Encoding.UTF8.GetBytes(timeStr);
                if (ws.State != WebSocketState.Open) break;
                var sendTask = ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                await sendTask.ConfigureAwait(false);
                if (ws.State != WebSocketState.Open) break;
                await Task.Delay(1000).ConfigureAwait(false); // this is NOT ideal
            }
        }
        
        #endregion
    }
}
