
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;


// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();


// Get API settings
var apiSettings = configuration.GetSection("ApiSettings");
var baseUrl = apiSettings["BaseUrl"];
var authEndpoint = apiSettings["AuthEndpoint"];
var encodeEndpoint = apiSettings["EncodeEndpoint"];
var email = apiSettings["Credentials:Email"];
var password = apiSettings["Credentials:Password"];


// Validate configuration
if (string.IsNullOrEmpty(baseUrl) ||
    string.IsNullOrEmpty(authEndpoint) ||
    string.IsNullOrEmpty(encodeEndpoint) ||
    string.IsNullOrEmpty(email) ||
    string.IsNullOrEmpty(password))
{
    Console.WriteLine("Missing required configuration in appsettings.json");
    return;
}

using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
httpClient.DefaultRequestHeaders.Accept.Add(
    new MediaTypeWithQualityHeaderValue("application/json"));

try
{
    //  Get JWT Token
    var token = await GetJwtToken(httpClient, email, password);
    if (string.IsNullOrEmpty(token))
    {
        Console.WriteLine("Failed to get token");
        return;
    }

    while (true)
    {
        Console.Clear();
        Console.WriteLine("=== Run-Length Encoding Client ===");
        Console.WriteLine("1. Encode text");
        Console.WriteLine("2. Exit");
        Console.Write("Choose an option (1-2): ");

        var choice = Console.ReadLine();

        if (choice == "2")
        {
            Console.WriteLine("Exiting application...");
            break;
        }

        if (choice != "1")
        {
            Console.WriteLine("Invalid option. Press any key to continue...");
            Console.ReadKey();
            continue;
        }

        Console.Write("\nEnter text to encode: ");
        var inputText = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(inputText))
        {
            Console.WriteLine("No input provided. Press any key to continue...");
            Console.ReadKey();
            continue;
        }

        Console.WriteLine("\nEncoding text...");
        var encodedResult = await EncodeText(httpClient,token, inputText);

        if (!string.IsNullOrEmpty(encodedResult))
        {
            Console.WriteLine($"\nEncoded result: {encodedResult}");
        }

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

// Helper methods
async Task<string?> GetJwtToken(HttpClient client, string email, string password)
{
    var authRequest = new { email, password };
    var response = await client.PostAsJsonAsync(authEndpoint, authRequest);

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"Auth failed: {response.StatusCode}");
        return null;
    }

    using var responseStream = await response.Content.ReadAsStreamAsync();
    using var jsonDoc = await JsonDocument.ParseAsync(responseStream);

    return jsonDoc.RootElement.GetProperty("token").GetString();
}

async Task<string?> EncodeText(HttpClient client, string token, string text)
{
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);

    var encodeRequest = new { text };
    var response = await client.PostAsJsonAsync(encodeEndpoint, encodeRequest);

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"Encoding failed: {response.StatusCode}");
        return null;
    }

    using var responseStream = await response.Content.ReadAsStreamAsync();
    using var jsonDoc = await JsonDocument.ParseAsync(responseStream);

    return jsonDoc.RootElement.GetProperty("encoded").GetString();
}