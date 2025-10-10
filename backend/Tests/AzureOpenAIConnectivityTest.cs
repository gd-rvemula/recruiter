using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RecruiterApi.Tests;

/// <summary>
/// Test connectivity to Azure OpenAI and verify embedding generation
/// Run this before starting Phase 1 implementation
/// </summary>
public class AzureOpenAIConnectivityTest
{
    private static readonly string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") 
        ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT environment variable is required");
    private static readonly string deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") 
        ?? "scm_test";
    private static readonly string apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") 
        ?? throw new InvalidOperationException("AZURE_OPENAI_API_KEY environment variable is required");

    public static async Task Main(string[] args)
    {
        // Load .env file if it exists
        LoadEnvironmentFile();
        
        Console.WriteLine("=== Azure OpenAI Connectivity Test ===\n");
        
        await TestConnection();
        await TestEmbeddingGeneration();
        await TestBatchEmbeddings();
        
        Console.WriteLine("\n=== Test Complete ===");
    }

    private static void LoadEnvironmentFile()
    {
        var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (!File.Exists(envFile))
        {
            Console.WriteLine("ℹ️  No .env file found, using system environment variables only.");
            return;
        }

        Console.WriteLine($"📁 Loading environment variables from: {envFile}");
        
        foreach (var line in File.ReadAllLines(envFile))
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                continue;

            var separatorIndex = trimmedLine.IndexOf('=');
            if (separatorIndex <= 0)
                continue;

            var key = trimmedLine.Substring(0, separatorIndex).Trim();
            var value = trimmedLine.Substring(separatorIndex + 1).Trim();
            
            // Remove quotes if present
            if (value.StartsWith("\"") && value.EndsWith("\""))
                value = value.Substring(1, value.Length - 2);

            Environment.SetEnvironmentVariable(key, value);
            Console.WriteLine($"   ✅ Loaded: {key}");
        }
        Console.WriteLine();
    }

    private static async Task TestConnection()
    {
        Console.WriteLine("1. Testing Azure OpenAI Connection...");
        
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("api-key", apiKey);
            
            var url = $"{endpoint}/openai/deployments?api-version=2023-05-15";
            var response = await client.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("   ✅ Connection successful!");
                Console.WriteLine($"   Endpoint: {endpoint}");
                Console.WriteLine($"   Deployment: {deployment}");
            }
            else
            {
                Console.WriteLine($"   ❌ Connection failed: {response.StatusCode}");
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"   Error: {error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Exception: {ex.Message}");
        }
    }

    private static async Task TestEmbeddingGeneration()
    {
        Console.WriteLine("\n2. Testing Embedding Generation...");
        
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("api-key", apiKey);
            
            var url = $"{endpoint}/openai/deployments/{deployment}/embeddings?api-version=2023-05-15";
            
            var requestBody = new
            {
                input = "Senior Software Engineer with 10 years experience in C#, .NET, React, and Azure"
            };
            
            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );
            
            var response = await client.PostAsync(url, content);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(result);
                
                if (jsonDoc.RootElement.TryGetProperty("data", out var data) && 
                    data.GetArrayLength() > 0)
                {
                    var embedding = data[0].GetProperty("embedding");
                    var dimensions = embedding.GetArrayLength();
                    
                    Console.WriteLine("   ✅ Embedding generated successfully!");
                    Console.WriteLine($"   Dimensions: {dimensions}");
                    Console.WriteLine($"   First 5 values: [{embedding[0].GetDouble():F6}, " +
                        $"{embedding[1].GetDouble():F6}, {embedding[2].GetDouble():F6}, " +
                        $"{embedding[3].GetDouble():F6}, {embedding[4].GetDouble():F6}]");
                }
            }
            else
            {
                Console.WriteLine($"   ❌ Failed: {response.StatusCode}");
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"   Error: {error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Exception: {ex.Message}");
        }
    }

    private static async Task TestBatchEmbeddings()
    {
        Console.WriteLine("\n3. Testing Batch Embedding Generation...");
        
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("api-key", apiKey);
            client.Timeout = TimeSpan.FromMinutes(2);
            
            var url = $"{endpoint}/openai/deployments/{deployment}/embeddings?api-version=2023-05-15";
            
            var requestBody = new
            {
                input = new[]
                {
                    "Python developer with Django and Flask experience",
                    "Java developer skilled in Spring Boot and microservices",
                    "DevOps engineer with Kubernetes and AWS expertise"
                }
            };
            
            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await client.PostAsync(url, content);
            stopwatch.Stop();
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(result);
                
                if (jsonDoc.RootElement.TryGetProperty("data", out var data))
                {
                    var count = data.GetArrayLength();
                    
                    Console.WriteLine("   ✅ Batch embeddings generated successfully!");
                    Console.WriteLine($"   Count: {count} embeddings");
                    Console.WriteLine($"   Time: {stopwatch.ElapsedMilliseconds}ms");
                    Console.WriteLine($"   Avg per embedding: {stopwatch.ElapsedMilliseconds / count}ms");
                }
            }
            else
            {
                Console.WriteLine($"   ❌ Failed: {response.StatusCode}");
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"   Error: {error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Exception: {ex.Message}");
        }
    }
}
