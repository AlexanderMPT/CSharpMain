using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace IpGeolocationAnalyzer
{
    class Program
    {
        private static readonly HttpClient httpClient = new HttpClient();

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IPs.txt");

            if (!File.Exists(filePath))
            {
                Console.WriteLine("Ошибка: Файл IPs.txt не найден рядом с exe!");
                return;
            }

            string[] ips = (await File.ReadAllLinesAsync(filePath))
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Trim())
                .ToArray();

            Console.WriteLine($"Загружено IP-адресов: {ips.Length}\n");

            List<IpData> ipDataList = new List<IpData>();

            foreach (string ip in ips)
            {
                IpData? data = await GetIpInfoAsync(ip);
                if (data != null && !string.IsNullOrEmpty(data.Country))
                {
                    ipDataList.Add(data);
                }
            }

            // Подсчёт стран
            var countryCount = ipDataList
                .GroupBy(x => x.Country)
                .Select(g => new { Country = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            Console.WriteLine("Представленные страны:");
            foreach (var item in countryCount)
            {
                Console.WriteLine($"{item.Country} → {item.Count} IP");
            }

            // Топ страна
            if (countryCount.Count > 0)
            {
                var top = countryCount.First();
                var topCountryIps = ipDataList.Where(x => x.Country == top.Country).ToList();

                var uniqueCities = topCountryIps
                    .Where(x => !string.IsNullOrWhiteSpace(x.City))
                    .Select(x => x.City.Trim())
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                Console.WriteLine($"\nСтрана с наибольшим количеством IP: {top.Country} ({top.Count} шт.)");
                Console.WriteLine("Города: " + string.Join(", ", uniqueCities));
            }
        }

        private static async Task<IpData?> GetIpInfoAsync(string ip)
        {
            try
            {
                string url = $"https://ipinfo.io/{ip}/json";
                string response = await httpClient.GetStringAsync(url);
                return JsonSerializer.Deserialize<IpData>(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Ошибка {ip}: {ex.Message}");
                return null;
            }
        }
    }

    public class IpData
    {
        public string? ip { get; set; }
        public string? city { get; set; }
        public string? country { get; set; }
    }
}