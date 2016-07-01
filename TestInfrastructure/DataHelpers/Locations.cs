namespace TestInfrastructure.DataHelpers
{
    public class Locations
    {
        public class Location
        {
            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="latitude">Широта</param>
            /// <param name="longitude">Долгота</param>
            public Location(double latitude, double longitude) 
            {
                Longitude = longitude;
                Latitude = latitude;
            }
            /// <summary>
            /// Долгота
            /// </summary>
            public double Longitude { get; set; }
            /// <summary>
            /// Широта
            /// </summary>
            public double Latitude { get; set; }
        }
        public static Location Google = new Location(37.422847, -122.084914);
        public static Location Google1 = new Location(37.423264, -122.085638);
        public static Location Google2 = new Location(37.421284, -122.085314);
        public static Location Google3 = new Location(37.421307, -122.082685);

        public static Location Apple = new Location(37.387298, -122.038892);
    }
}