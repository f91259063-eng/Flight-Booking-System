
namespace Flight_Booking_System.Services
{
    public class DistanceCalculatorService
    {
        public double CalculateDistance(
            double fromLatitude,
            double fromLongitude,
            double toLatitude,
            double toLongitude)
        {
            const double earthRadiusKm = 6371.0;

            double lat1 = ToRadians(fromLatitude);
            double lat2 = ToRadians(toLatitude);

            double deltaLat = ToRadians(toLatitude - fromLatitude);
            double deltaLon = ToRadians(toLongitude - fromLongitude);

            double a =
                Math.Pow(Math.Sin(deltaLat / 2), 2) +
                Math.Cos(lat1) *
                Math.Cos(lat2) *
                Math.Pow(Math.Sin(deltaLon / 2), 2);

            double c = 2 * Math.Atan2(
                Math.Sqrt(a),
                Math.Sqrt(1 - a));

            return Math.Round(earthRadiusKm * c, 2);
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
