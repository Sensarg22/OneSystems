using serverServer.Domain;
using serverServer.ViewModels.Account;

namespace TestInfrastructure
{
    public static class TestUsers
    {
        public static LoginViewModel Default = new LoginViewModel()
        {
            PhoneNumber = "12345678900",
            Password = "gosetn",
            
            DeviceInfo = new DeviceInfoViewModel
            {
                Model = "Test Device",
                OS = DeviceOS.Empty,
                OSVersion = "1.0"
            }
        };
        public static LoginViewModel P1 = new LoginViewModel { PhoneNumber = "+88811100010", Email = "ScenarioP1@server.com", Password = "Scenario" };
        public static LoginViewModel P2 = new LoginViewModel { PhoneNumber = "+88811100020", Email = "ScenarioP2@server.com", Password = "Scenario" };
        public static LoginViewModel P3 = new LoginViewModel { PhoneNumber = "+88811100030", Email = "ScenarioP3@server.com", Password = "Scenario" };

        public static LoginViewModel D1 = new LoginViewModel { PhoneNumber = "+88822200010", Email = "ScenarioD1@server.com", Password = "Scenario" };
        public static LoginViewModel D2 = new LoginViewModel { PhoneNumber = "+88822200020", Email = "ScenarioD2@server.com", Password = "Scenario" };
        public static LoginViewModel D3 = new LoginViewModel { PhoneNumber = "+88822200030", Email = "ScenarioD3@server.com", Password = "Scenario" };

        public static readonly LoginViewModel[] Users = { P1, P2, P3, D1, D2, D3 };

        public static string GetUserName(this LoginViewModel model)
        {
            return model?.PhoneNumber.Replace("+", string.Empty);
        }
    }
}