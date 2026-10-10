using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using CampusFix.Api.Services;

// Runs the real middleware/controllers against an explicitly isolated local database.
sealed class QaHttpApi : IAsyncDisposable
{
    readonly Process process;
    readonly HttpClient client;
    QaHttpApi(Process process, HttpClient client) { this.process = process; this.client = client; }

    public static async Task<QaHttpApi> Start(string connection, string key)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var dll = Path.Combine(root, "backend/CampusFix.Api/bin/" + (typeof(QaHttpApi).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyConfigurationAttribute), false).Cast<System.Reflection.AssemblyConfigurationAttribute>().Single().Configuration) + "/net10.0/CampusFix.Api.dll");
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();var port = ((IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();
        var url = $"http://127.0.0.1:{port}";
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(dll)!, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(dll);
        start.Environment["ASPNETCORE_URLS"] = url;
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "QaTesting";
        start.Environment["ConnectionStrings__CampusFix"] = connection;
        start.Environment["Jwt__Key"] = key;
        start.Environment["Mail__EncryptionKey"] = key;
        var process = Process.Start(start)!;
        process.OutputDataReceived += (_, _) => { };process.ErrorDataReceived += (_, _) => { };
        process.BeginOutputReadLine();process.BeginErrorReadLine();
        var api = new QaHttpApi(process, new HttpClient { BaseAddress = new Uri(url + "/api/"), Timeout = TimeSpan.FromSeconds(15) });
        try
        {
            for (var i = 0; i < 80; i++)
            {
                if (process.HasExited) throw new Exception("The isolated QA API exited before readiness.");
                try { using var response = await api.client.GetAsync("health");if (response.IsSuccessStatusCode) return api; }
                catch (HttpRequestException) { }
                await Task.Delay(250);
            }
            throw new Exception("The isolated QA API did not become ready.");
        }
        catch { await api.DisposeAsync();throw; }
    }

    public async Task<JsonNode> Call(string path, string method, JsonObject? body, string? token)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (method is not "GET" and not "DELETE") request.Content = JsonContent.Create(body ?? new());
        using var response = await client.SendAsync(request);
        var result = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        if (!response.IsSuccessStatusCode) throw new ApiError((int)response.StatusCode, result["title"]?.ToString() ?? "HTTP error");
        if (method == "POST" && path == "reportes" && response.StatusCode != HttpStatusCode.Created) throw new Exception("Ticket creation must return HTTP 201.");
        return result;
    }

    public async Task ProtocolChecks()
    {
        using var health = await client.GetAsync("health");
        if (health.StatusCode != HttpStatusCode.OK) throw new Exception("Health failed.");
        Console.WriteLine("PASS HTTP health checks PostgreSQL");
        using var malformed = await client.PostAsync("reportes", new StringContent("{broken", Encoding.UTF8, "application/json"));
        if (malformed.StatusCode != HttpStatusCode.BadRequest) throw new Exception("Malformed JSON must return 400.");
        Console.WriteLine("PASS HTTP malformed JSON returns 400");
        using var invalid = new HttpRequestMessage(HttpMethod.Get, "data");invalid.Headers.Authorization = new("Bearer", "invalid-token");
        using var denied = await client.SendAsync(invalid);
        if (denied.StatusCode != HttpStatusCode.Unauthorized) throw new Exception("Invalid JWT must return 401.");
        Console.WriteLine("PASS HTTP JWT middleware rejects invalid signature");
        using var unknown = await client.GetAsync("unknown-qa-route");
        if (unknown.StatusCode != HttpStatusCode.NotFound) throw new Exception("Unknown route must return 404.");
        Console.WriteLine("PASS HTTP unknown API route returns 404");
    }

    public async ValueTask DisposeAsync()
    {
        client.Dispose();
        if (!process.HasExited) { process.Kill(entireProcessTree: true);await process.WaitForExitAsync(); }
        process.Dispose();
    }
}
