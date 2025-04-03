using System;

namespace src
{
    public class MvtToLatLon
    {
        public static (
            double lonLeft,
            double lonRight,
            double latTop,
            double latBottom
        ) MvtToLatLonBounds(int zoom, int x, int y)
        {
            int numTiles = 1 << zoom;

            // Calculate longitude borders
            double lonLeft = (x / (double)numTiles) * 360 - 180;
            double lonRight = ((x + 1) / (double)numTiles) * 360 - 180;

            // Calculate latitude borders
            double latBottom = ToDegrees(Math.PI - (2 * Math.PI * y) / numTiles);
            double latTop = ToDegrees(Math.PI - (2 * Math.PI * (y + 1)) / numTiles);

            return (lonLeft, lonRight, latTop, latBottom);
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        private static double ToDegrees(double radians)
        {
            return 180 / Math.PI * (2 * Math.Atan(Math.Exp(radians)) - Math.PI / 2);
        }
    }
}
