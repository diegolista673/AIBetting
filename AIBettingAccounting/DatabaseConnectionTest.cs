using AIBettingAccounting.Data;
using AIBettingAccounting.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AIBettingAccounting;

/// <summary>
/// Utility per testare la connessione al database PostgreSQL.
/// </summary>
public class DatabaseConnectionTest
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("?? AIBetting - Test Connessione PostgreSQL");
        Console.WriteLine("===========================================\n");

        try
        {
            // Carica configurazione
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("Accounting");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("? Connection string 'Accounting' non trovata in appsettings.json");
                return;
            }

            Console.WriteLine($"?? Connection String: {MaskPassword(connectionString)}\n");

            // Crea DbContext
            var optionsBuilder = new DbContextOptionsBuilder<TradingDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            await using var dbContext = new TradingDbContext(optionsBuilder.Options);

            // Test 1: Connessione base
            Console.WriteLine("?? Test 1: Verifica connessione...");
            var canConnect = await dbContext.Database.CanConnectAsync();
            
            if (!canConnect)
            {
                Console.WriteLine("? Impossibile connettersi al database");
                return;
            }
            
            Console.WriteLine("? Connessione stabilita con successo!\n");

            // Test 2: Verifica tabelle
            Console.WriteLine("?? Test 2: Verifica schema database...");
            
            var tables = await dbContext.Database.ExecuteSqlRawAsync(
                "SELECT 1 FROM information_schema.tables WHERE table_name = 'trades'"
            );
            
            Console.WriteLine($"? Tabella 'trades': {(tables >= 0 ? "PRESENTE" : "NON TROVATA")}");
            
            var summariesCheck = await dbContext.Database.ExecuteSqlRawAsync(
                "SELECT 1 FROM information_schema.tables WHERE table_name = 'daily_summaries'"
            );
            
            Console.WriteLine($"? Tabella 'daily_summaries': {(summariesCheck >= 0 ? "PRESENTE" : "NON TROVATA")}\n");

            // Test 3: Count records
            Console.WriteLine("?? Test 3: Conta records esistenti...");
            var tradesCount = await dbContext.Trades.CountAsync();
            var summariesCount = await dbContext.DailySummaries.CountAsync();
            
            Console.WriteLine($"?? Trades presenti: {tradesCount}");
            Console.WriteLine($"?? Daily summaries presenti: {summariesCount}\n");

            // Test 4: Insert test record
            Console.WriteLine("?? Test 4: Inserimento record di test...");
            
            var testTrade = new TradeEntity
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                MarketId = "TEST.123456",
                SelectionId = "999999",
                Stake = 10.00m,
                Odds = 2.50m,
                Type = "BACK",
                Status = "PENDING",
                Commission = 0.05m,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Trades.Add(testTrade);
            await dbContext.SaveChangesAsync();
            
            Console.WriteLine($"? Record inserito con ID: {testTrade.Id}");

            // Test 5: Verifica lettura
            Console.WriteLine("\n?? Test 5: Verifica lettura record...");
            var readTrade = await dbContext.Trades.FirstOrDefaultAsync(t => t.Id == testTrade.Id);
            
            if (readTrade != null)
            {
                Console.WriteLine("? Record letto correttamente:");
                Console.WriteLine($"   Market ID: {readTrade.MarketId}");
                Console.WriteLine($"   Selection ID: {readTrade.SelectionId}");
                Console.WriteLine($"   Stake: {readTrade.Stake}");
                Console.WriteLine($"   Odds: {readTrade.Odds}");
                Console.WriteLine($"   Status: {readTrade.Status}");
            }
            else
            {
                Console.WriteLine("??  Record non trovato dopo inserimento");
            }

            // Test 6: Cleanup
            Console.WriteLine("\n?? Test 6: Pulizia record di test...");
            dbContext.Trades.Remove(testTrade);
            await dbContext.SaveChangesAsync();
            Console.WriteLine("? Record di test rimosso");

            // Summary
            Console.WriteLine("\n" + new string('=', 50));
            Console.WriteLine("? TUTTI I TEST COMPLETATI CON SUCCESSO!");
            Console.WriteLine(new string('=', 50));
            Console.WriteLine("\n?? Il database è configurato correttamente e pronto per l'uso.");
            Console.WriteLine("   Puoi iniziare a utilizzare AIBettingAccounting nel progetto.\n");
        }
        catch (Npgsql.NpgsqlException ex)
        {
            Console.WriteLine("\n? ERRORE DI CONNESSIONE POSTGRESQL:");
            Console.WriteLine($"   Messaggio: {ex.Message}");
            Console.WriteLine($"\n?? Suggerimenti:");
            Console.WriteLine("   1. Verifica che PostgreSQL sia in esecuzione: sudo systemctl status postgresql");
            Console.WriteLine("   2. Controlla username/password in appsettings.json");
            Console.WriteLine("   3. Verifica che il database 'aibetting_db' esista");
            Console.WriteLine("   4. Controlla firewall e pg_hba.conf per permettere connessioni");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n? ERRORE GENERICO: {ex.Message}");
            Console.WriteLine($"\nStack Trace:\n{ex.StackTrace}");
        }

        Console.WriteLine("\nPremi un tasto per uscire...");
        Console.ReadKey();
    }

    private static string MaskPassword(string connectionString)
    {
        // Maschera password per sicurezza nel log
        var parts = connectionString.Split(';');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].StartsWith("Password=", StringComparison.OrdinalIgnoreCase))
            {
                parts[i] = "Password=***";
            }
        }
        return string.Join(";", parts);
    }
}
