using Microsoft.AspNetCore.Http;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;

namespace Server.SocketHandlers
{
    public class WebSocketClockHandler
    {
        private readonly RequestDelegate _next;
        private CancellationToken _cancellationToken;

        public WebSocketClockHandler(RequestDelegate next)
        {
            _next = next;
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

            var keys = context.Request.Cookies.Keys;

            var token = await WebsocketHelpers.ValidateBearerTokenAsync(context.Request.QueryString.Value);
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
            _cancellationToken = context.RequestAborted;
            var ws = await context.WebSockets.AcceptWebSocketAsync();
            await Task.WhenAll(WriteTask(ws), ReadTask(ws));
        }

        private async Task ReadTask(WebSocket ws)
        {
            var buffer = new ArraySegment<byte>(new byte[1024]);
            while (!_cancellationToken.IsCancellationRequested)
            {
                await ws.ReceiveAsync(buffer, _cancellationToken).ConfigureAwait(false);
                if (ws.State != WebSocketState.Open) break;
            }
        }

        private async Task WriteTask(WebSocket ws)
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                var timeStr = DateTime.UtcNow.ToString("MMM dd yyyy HH:mm:ss.fff UTC", CultureInfo.InvariantCulture);
                var buffer = Encoding.UTF8.GetBytes(timeStr);
                if (ws.State != WebSocketState.Open) break;
                var sendTask = ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cancellationToken);
                await sendTask.ConfigureAwait(false);
                if (ws.State != WebSocketState.Open) break;
                await Task.Delay(1000).ConfigureAwait(false);
            }
        }
        
        #endregion
    }
}
