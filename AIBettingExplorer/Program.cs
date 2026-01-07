using AIBettingExplorer;
using AIBettingCore.Interfaces;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

IMarketStreamClient stream = new BetfairMarketStreamClient();
ICacheBus bus = new InMemoryCacheBus();
var explorer = new ExplorerService(stream, bus);

await explorer.RunAsync(cts.Token);
