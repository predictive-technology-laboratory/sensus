

namespace AdapEMASensus
{
    public class Place
    {

        public string ID { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }

        public Place(string id, double longitude, double latitude)
        {
            ID = id;
            Longitude = longitude;
            Latitude = latitude;
        }

        public Place(string id)
        {
            ID = id;
        }

    }
}
