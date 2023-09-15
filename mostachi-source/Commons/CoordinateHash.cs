using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Commons
{
    public class CoordinateHash
    {
        public const int DEFAULT_BINS_PER_DEGREE = 25;
        static readonly GaussKreuger gaussKreuger = new GaussKreuger("sweref_99_tm");

        public int Index { get; }
        public double ExactLongitude { get; set; }
        public double ExactLatitude { get; }
        public bool IsInSweden { get; private set; }

        public CoordinateHash(double latitude, double longitude, int binsPerDegree = DEFAULT_BINS_PER_DEGREE)
        {
            ExactLatitude = latitude;
            ExactLongitude = longitude;
            int resolution = binsPerDegree * 360;
            int x = (int)Math.Floor(resolution * longitude / 360); 
            int y = (int)Math.Floor(resolution * latitude / 180); //double the resolution per degree latitude gives approximately square bins in Sweden
            Index = x * resolution + y;

            IsInSweden = DataReader.IsInSweden(ExactLatitude, ExactLongitude);
        }

        public (double lat, double lon) GetHashCoordinate(int binsPerDegree = DEFAULT_BINS_PER_DEGREE)
        {
            //Hash = x * resolution + y => y = Hash - x * resolution, x = (Hash - y) / resolution
            int resolution = binsPerDegree * 360;
            int y = Index % resolution;
            int x = Index / resolution;
            return (y * 180.0 / resolution, x * 360.0 / resolution);
        }

        public static (double lat, double lon) GetHashCoordinate(int hash, int binsPerDegree = DEFAULT_BINS_PER_DEGREE)
        {
            //Hash = x * resolution + y => y = Hash - x * resolution, x = (Hash - y) / resolution
            int resolution = binsPerDegree * 360;
            int y = hash % resolution;
            int x = hash / resolution;
            return (y * 180.0 / resolution, x * 360.0 / resolution);
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is CoordinateHash))
                return false;
            return ((CoordinateHash)obj).Index == Index;
        }

        public override int GetHashCode()
        {
            return Index.GetHashCode();
        }

        public override string ToString()
        {
            return ExactLatitude + "," + ExactLongitude;
        }

        public double MetersTo(CoordinateHash other)
        {
            return HaversineDistance_meters(this.ExactLatitude, other.ExactLatitude, this.ExactLongitude, other.ExactLongitude);
        }

        public static double HaversineDistance_meters(double lat1, double lat2, double lon1, double lon2)
        {
            lat1 = ToRadians(lat1);
            lat2 = ToRadians(lat2);
            lon1 = ToRadians(lon1);
            lon2 = ToRadians(lon2);

            const double r = 6378100; // meters

            var sdlat = Math.Sin((lat2 - lat1) / 2);
            var sdlon = Math.Sin((lon2 - lon1) / 2);
            var q = sdlat * sdlat + Math.Cos(lat1) * Math.Cos(lat2) * sdlon * sdlon;
            var d = 2 * r * Math.Asin(Math.Sqrt(q));

            return d;
        }

        public static double ToRadians(double angle)
        {
            return angle * (Math.PI / 180.0);
        }

        public (double x, double y, string cell_id) To_Sweref99TMRaster_1km()
        {
            return LatLon_to_Sweref99TMRaster_1km(ExactLatitude, ExactLongitude);
        }

        public static CoordinateHash Sweref99TMIndex_toCoordinate(string sweref99Tm_1km_index)
        {
            double y = double.Parse(sweref99Tm_1km_index.Substring(0, 6));
            double x = double.Parse(sweref99Tm_1km_index.Substring(6));
            var latlon = gaussKreuger.grid_to_geodetic(x, y);
            return new CoordinateHash(latlon[0], latlon[1]);
        }

        public static (double x, double y, string cell_id) LatLon_to_Sweref99TMRaster_1km(double latitude, double longitude)
        {
            var xy = gaussKreuger.geodetic_to_grid(latitude, longitude);
            var rut_id = (1000 * (int)(xy[1] / 1000.0)).ToString() + (1000 * (int)(xy[0] / 1000.0)).ToString();
            return (xy[0], xy[1], rut_id);
        }
    }
}
