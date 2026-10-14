
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Flight_Booking_System.Services
{
    public class AirportCoordinateService
    {
        private readonly HttpClient _httpClient;

        public AirportCoordinateService(HttpClient httpClient)
        {
            _httpClient = httpClient;

            _httpClient.BaseAddress =
                new Uri("https://nominatim.openstreetmap.org/");

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Flight_Booking_System/1.0");
        }

        public async Task<AirportCoordinates?> GetCoordinatesAsync(
            string airportName,
            string cityName,
            CancellationToken cancellationToken = default)
        {
            string query =
                $"{airportName} airport, {cityName}, Egypt";

            string url =
                $"search?format=jsonv2&limit=5&q={Uri.EscapeDataString(query)}";

            using var response = await _httpClient.GetAsync(
                url, cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(
                cancellationToken);

            var results =
                JsonSerializer.Deserialize<List<NominatimResult>>(json);

            if (results == null)
                return null;

            foreach (var result in results)
            {
                // نقبل النتائج المصنفة كمطار فقط
                bool isAirport =
                    result.Class == "aeroway" ||
                    result.Type == "aerodrome";

                if (!isAirport)
                    continue;

                if (!double.TryParse(
                        result.Lat,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double latitude))
                    continue;

                if (!double.TryParse(
                        result.Lon,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double longitude))
                    continue;

                return new AirportCoordinates
                {
                    Latitude = latitude,
                    Longitude = longitude
                };
            }

            return null;
        }

        private class NominatimResult
        {
            [JsonPropertyName("lat")]
            public string Lat { get; set; } = "";

            [JsonPropertyName("lon")]
            public string Lon { get; set; } = "";

            [JsonPropertyName("class")]
            public string Class { get; set; } = "";

            [JsonPropertyName("type")]
            public string Type { get; set; } = "";
        }
    }

    public class AirportCoordinates
    {
        public double Latitude { get; set; }

        public double Longitude { get; set; }
    }
}
