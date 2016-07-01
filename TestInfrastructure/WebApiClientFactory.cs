using System;
using System.Net.Http;
using System.Net.Http.Headers;
using serverServer.Domain;

namespace TestInfrastructure
{
    public class WebApiClientFactory: BaseClientFactory<HttpClient>
    {
        public WebApiClientFactory(ServerMode serverMode) : base(serverMode) { }

        private HttpClient RealClient => new HttpClient { BaseAddress = new Uri($"{Host}") };
        private HttpClient LocalClient => Server.CreateClient();
        protected override string Protocol => "http";

        public override HttpClient Create(ClientType clientType = ClientType.Driver)
        {
            var client = Mode == ServerMode.InMemory ? LocalClient : RealClient;

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
    }
}