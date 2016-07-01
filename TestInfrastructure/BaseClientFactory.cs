using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Server.Domain;

namespace TestInfrastructure
{
    public abstract class BaseClientFactory<T>
    {
        protected readonly ServerMode Mode;
        protected readonly string ServerHost = "***dev.azurewebsites.net";

        protected BaseClientFactory(ServerMode mode)
        {
            Mode = mode;
        }

        protected string Host => Mode != ServerMode.Real ? $"{Protocol}://localhost:5000" : $"{Protocol}s://{ServerHost}";
        protected abstract string Protocol { get; }
        private TestServer _server;
        protected TestServer Server => _server ?? (_server = new TestServer(new WebHostBuilder()
                .UseStartup<Startup>()
                .UseEnvironment("Development")));

        public abstract T Create(ClientType clientType = ClientType.Driver);
    }
}