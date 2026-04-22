using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Globalization;
namespace Meteo
{
    class Program
    {
        // API-key for Open-Meteo
        // Using Geocoding API for getting coordinates

        //HTTP client instance 
        private static readonly HttpClient client = new HttpClient();
        static async Task Main(string[] args)
        {
            Console.WriteLine("Weather app");
            Console.Write("City/location: ");
            string paikkakunta = Console.ReadLine()?.Trim() ?? "";

            // Validate that the user enters location
            if (string.IsNullOrEmpty(paikkakunta))
            {
                Console.WriteLine("Virhe: Paikkakunta ei voi olla tyhjä.");
                return;
            }

            Console.WriteLine($"Retrieving information for location: {paikkakunta}...");

            // retrieving coordinates according to location
            var koordinaatit = await HaeKoordinaatit(paikkakunta);
            if (koordinaatit == null)
            {
                Console.WriteLine("Virhe: Paikkakuntaa ei löydy.");
                return;
            }

            // fetch and display weather data
            await HaeSaatiedot(koordinaatit.Value.lat, koordinaatit.Value.lon, koordinaatit.Value.nimi);
        }

        //calls the api and returns lat, lon and name for location
        static async Task<(double lat, double lon, string nimi)?>
        HaeKoordinaatit(string paikkakunta)
        {
            string url = $"https://geocoding-api.open-meteo.com/v1/search?name={paikkakunta}&count=1&language=fi&format=json";

            HttpResponseMessage vastaus = await client.GetAsync(url);

            if (!vastaus.IsSuccessStatusCode) return null;
            string json = await vastaus.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            //return null if no results found
            if (!root.TryGetProperty("results", out var tulokset) ||
                tulokset.GetArrayLength() == 0)
                return null;

            var tulos = tulokset[0];
            double lat = tulos.GetProperty("latitude").GetDouble();
            double lon = tulos.GetProperty("longitude").GetDouble();
            string nimi = tulos.GetProperty("name").GetString() ?? paikkakunta;
            return (lat, lon, nimi);
        }

        //calls the api and prints weather conditions
        static async Task HaeSaatiedot(double lat, double lon, string nimi)
        {
            //build the url with coordinates
            string url = "https://api.open-meteo.com/v1/forecast" +
            $"?latitude={lat.ToString(CultureInfo.InvariantCulture)}" +
            $"&longitude={lon.ToString(CultureInfo.InvariantCulture)}" +
            $"&current_weather=true" +
            $"&hourly=temperature_2m";


            HttpResponseMessage vastaus = await client.GetAsync(url);
            vastaus.EnsureSuccessStatusCode();

            string json = await vastaus.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            //parse weather values 
            var nykyinen = root.GetProperty("current_weather");
            double lampotila = nykyinen.GetProperty("temperature").GetDouble();
            double tuulinopeus = nykyinen.GetProperty("windspeed").GetDouble();
            int saakoodis = nykyinen.GetProperty("weathercode").GetInt32();

            //display weather info
            Console.WriteLine();
            Console.WriteLine($"Location: {nimi}");
            Console.WriteLine(new string('-', 30));
            Console.WriteLine($"Temperature: {lampotila} °C");
            Console.WriteLine($"Wind speed: {tuulinopeus} km/h");
            Console.WriteLine($"Weather code: {saakoodis} ({TulkitseSaakoodi(saakoodis)})");
        }
        static string TulkitseSaakoodi(int koodi) => koodi switch
        {
            0 => "Clear sky",
            1 or 2 or 3 => "Cloudy",
            45 or 48 => "Foggy",
            51 or 53 or 55 => "Drizzle",
            61 or 63 or 65 => "Rain",
            71 or 73 or 75 => "Snowfall",
            80 or 81 or 82 => "Rain showers",
            95 => "Thunderstorm",
            _ => "Unknown",
        };
    }
}