using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.TestHost;
using serverServer.Domain;

namespace TestInfrastructure
{
    public class SocketClientFactory : BaseClientFactory<SocketClient>
    {
        public SocketClientFactory(ServerMode serverMode): base(serverMode) { }

        private Microsoft.AspNetCore.WebSockets.Client.WebSocketClient RealClient =>  new Microsoft.AspNetCore.WebSockets.Client.WebSocketClient();

        private WebSocketClient LocalClient => Server.CreateWebSocketClient();

        protected override string Protocol => "ws";

        public override SocketClient Create(ClientType clientType = ClientType.Driver)
        {
            return Mode == ServerMode.InMemory 
                ? new SocketClient(async (token) => await ConfigureClient(LocalClient, clientType, token), clientType) 
                : new SocketClient(async (token) => await ConfigureClient(RealClient, clientType, token), clientType);
        }

        private Task<WebSocket> ConfigureClient(WebSocketClient socketClient, ClientType clientType, string token)
        {
            socketClient.ConfigureRequest = request =>
            {
                request.Headers.Add("Authorization", $"{JwtBearerDefaults.AuthenticationScheme} {token}");
            };
            return socketClient.ConnectAsync(new Uri($"{Host}/ws?type={clientType}"), CancellationToken.None);
        }
        private Task<WebSocket> ConfigureClient(Microsoft.AspNetCore.WebSockets.Client.WebSocketClient socketClient, ClientType clientType, string token)
        {
            socketClient.ConfigureRequest = request =>
            {
                request.Headers.Add("Authorization", $"{JwtBearerDefaults.AuthenticationScheme} {token}");
            };
            return socketClient.ConnectAsync(new Uri($"{Host}/ws?type={clientType}"), CancellationToken.None);
        }
    }
}