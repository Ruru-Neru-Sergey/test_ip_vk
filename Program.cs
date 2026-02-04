using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

// Класс для данных от API ipinfo.io
public class IpInfo
{
    [JsonProperty("country")]  // чтобы правильно сопоставлялось из JSON
    public string? Country { get; set; }

    [JsonProperty("city")]
    public string? City { get; set; }
}

class Program
{
    // HttpClient для обработки IP адресов
    private static readonly HttpClient httpClient = new HttpClient();

    static async Task Main(string[] args)
    {

        // 1. Читаем IP-адреса из файла
        string filePath = "IPs.txt";

        if (!File.Exists(filePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ОШИБКА: Файл '{filePath}' не найден!");
            Console.WriteLine("Положите файл рядом с программой.");
            Console.ResetColor();
            return; // завершаем программу без ожидания нажатия
        }

        string[] lines = File.ReadAllLines(filePath);
        List<string> ipList = lines
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        if (ipList.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Файл пуст или не содержит IP-адресов :(");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Найдено {ipList.Count} IP-адресов.");
        Console.ResetColor();
        Console.WriteLine();

        // 2. Собираем данные по каждому IP
        List<IpInfo> allIpData = new List<IpInfo>();

        foreach (string ip in ipList)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"Обрабатываю {ip}... ");
            Console.ResetColor();

            IpInfo? info = await GetIpInfoFromApi(ip);

            if (info != null && info.Country != null)
            {
                allIpData.Add(info);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("ОК");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ошибка");
                Console.ResetColor();
            }

            // Задержка, чтобы не превысить лимит запросов ipinfo.io
            await Task.Delay(1200); // 1.2 секунды между запросами
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Успешно обработано: {allIpData.Count} из {ipList.Count}");
        Console.ResetColor();
        Console.WriteLine();

        if (allIpData.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("К сожалению, данные не получены ни для одного IP.");
            Console.ResetColor();
            return;
        }

        // 3. Считаем количество IP по странам
        var countryCount = allIpData
            .GroupBy(ip => ip.Country!)
            .Select(group => new
            {
                Country = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(x => x.Count);

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Страны и количество IP-адресов:");
        Console.ResetColor();

        foreach (var item in countryCount)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"{item.Country,-4}");
            Console.ResetColor();
            Console.WriteLine($" — {item.Count}");
        }

        // 4. Страна с максимальным количеством IP и её города
        var topCountry = countryCount.First();

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Страна с наибольшим количеством IP: {topCountry.Country} ({topCountry.Count} шт.)");
        Console.ResetColor();

        var cities = allIpData
            .Where(ip => ip.Country == topCountry.Country && ip.City != null)
            .Select(ip => ip.City!)
            .Distinct()
            .OrderBy(city => city);

        if (cities.Any())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{topCountry.Country}: {string.Join(", ", cities)}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Города для этой страны не определены (все city = null)");
            Console.ResetColor();
        }

        // Финальное сообщение — программа завершится автоматически
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Программа завершена успешно!");
        Console.WriteLine("Спасибо за внимание к заданию :)");
        Console.ResetColor();

        // Программа закроется сама через 3 секунды
        await Task.Delay(3000);
    }

    // Функция, которая делает GET-запрос к API и возвращает объект IpInfo
    private static async Task<IpInfo?> GetIpInfoFromApi(string ip)
    {
        try
        {
            string url = $"https://ipinfo.io/{ip}/json";
            string jsonResponse = await httpClient.GetStringAsync(url);

            // Десериализуем JSON в наш класс
            return JsonConvert.DeserializeObject<IpInfo>(jsonResponse);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Ошибка при запросе {ip}: {ex.Message}");
            Console.ResetColor();
            return null;
        }
    }
}