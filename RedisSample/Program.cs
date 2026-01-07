using StackExchange.Redis;
using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "localhost:6379,abortConnect=false";
        Console.WriteLine($"Connecting to Redis: {connectionString}");

        using var mux = await ConnectionMultiplexer.ConnectAsync(connectionString);
        var db = mux.GetDatabase();
        var sub = mux.GetSubscriber();

        // Write & read a simple string
        await db.StringSetAsync("demo:key", "hello", flags: CommandFlags.FireAndForget);
        var value = await db.StringGetAsync("demo:key");
        Console.WriteLine($"StringGet demo:key = {value}");

        // Hash example (simulating a price snapshot)
        var hashKey = "prices:1.23456789:12345";
        await db.HashSetAsync(hashKey, new HashEntry[]
        {
            new("lpm", 2.10),
            new("wap", 2.11),
            new("ts", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
        }, flags: CommandFlags.FireAndForget);
        var entries = await db.HashGetAllAsync(hashKey);
        Console.WriteLine($"HashGetAll {hashKey}: {JsonSerializer.Serialize(entries.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString()))}");

        // Pub/Sub example
        var channel = "channel:price-updates";
        await sub.SubscribeAsync(channel, (chn, msg) =>
        {
            Console.WriteLine($"Received on {chn}: {msg}");
        });
        var payload = JsonSerializer.Serialize(new { marketId = "1.23456789", ts = DateTimeOffset.UtcNow });
        await sub.PublishAsync(channel, payload);

        Console.WriteLine("Press Ctrl+C to exit...");
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
        try { await Task.Delay(Timeout.Infinite, cts.Token); } catch { }
    }
}
