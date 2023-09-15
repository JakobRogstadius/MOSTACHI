using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commons
{
    public class EquidistantGeoRaster
    {
        public static List<(double, double)> GenerateGrid(double lat, double lon, double kmWidth, double kmHeight, double kmResolution)
        {
            List<(double, double)> grid = new List<(double, double)>();

            double deltaLat = kmResolution / 111.319;

            int rows = (int)(kmHeight / kmResolution);
            int cols = (int)(kmWidth / kmResolution);

            double deltaLon = kmResolution / (111.319 * Math.Cos(lat * Math.PI / 180.0));
            double centerLon = lon + deltaLon * (cols / 2);

            for (int i = 0; i < rows; i++)
            {
                double newLat = lat + i * deltaLat;
                deltaLon = kmResolution / (111.319 * Math.Cos(newLat * Math.PI / 180.0));

                for (int j = -(cols / 2); j < (cols / 2); j++)
                {
                    double newLon = centerLon + j * deltaLon;

                    grid.Add((newLat, newLon));
                }
            }

            return grid;
        }

        /*
         * What am I even trying to accomplish?
         * 
         * I want to rasterize the infrastructure use into an equidistant grid.
         * To do this, I should presumably reproject the coordinates into a new equidistant coordinate system.
         * Once I've done that, it would be very easy to rasterize the values.
         * latlon -> custom -> raster -> latlon -> sweref?
         * However, I am looking to create a very specific raster. I need to know which one.
         */
    }
}
