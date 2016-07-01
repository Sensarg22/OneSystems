# OneSystems
###Integration test infrastructure

Проект инфраструктуры для написания интеграционных тестов к серверу имеющий 2 интерфейса WebApi и WebSocken

Пример написания теста для WebSocket c использованием данной инфраструктуры

```
 [Fact]
 public async Task VehicleTest()
 {
     var driver = SocketClientFactory.Create();
     driver.Connect();

     await driver.Service<IAccountPublicService>().Auth(x => x.SignIn(TestUsers.D1));

     var vehicle = new VehicleViewModel()
     {
         Brand = "Ford",
         Model = "Focus",
         Color = "Red",
         Year = 2014,
         LicensePlate = "ABB699",
         AmountOfSeats = 4
     };

     await driver.Service<IVehiclePublicService>().Data(x => x.Save(vehicle));
     var vehicleSaved = await driver.Service<IVehiclePublicService>().Data(x => x.Get(null));

     Assert.Equal(vehicleSaved.Brand, vehicle.Brand);
     Assert.Equal(vehicleSaved.Model, vehicle.Model);
     Assert.Equal(vehicleSaved.Color, vehicle.Color);
     Assert.Equal(vehicleSaved.LicensePlate, vehicle.LicensePlate);
     Assert.Equal(vehicleSaved.Year, vehicle.Year);
     Assert.Equal(vehicleSaved.AmountOfSeats, vehicle.AmountOfSeats);
 }
```
