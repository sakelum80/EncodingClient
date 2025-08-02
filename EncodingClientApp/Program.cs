
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

    // Read input text
    Console.Write("Enter text to encode: ");
    var inputText = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(inputText))
    {
        Console.WriteLine("No input provided");
        return;
    }

    // Call Encoding API
    var encodedResult = await EncodeText(httpClient, token, inputText);
    Console.WriteLine($"Encoded result: {encodedResult}");
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