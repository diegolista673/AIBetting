using AIBettingCore.Models;
using AIBettingExecutor.Interfaces;
using Serilog;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace AIBettingExecutor.BetfairAPI;

/// <summary>
/// Implementation of Betfair Exchange API client with SSL certificate authentication.
/// </summary>
public class BetfairClient : IBetfairClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _appKey;
    private readonly string _certPath;
    private readonly string _certPassword;
    private readonly Serilog.ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private string? _sessionToken;
    private DateTimeOffset _sessionExpiry;

    private const string ApiBaseUrl = "https://api.betfair.com/exchange/betting/rest/v1.0/";
    private const string AuthUrl = "https://identitysso-cert.betfair.com/api/certlogin";

    public bool IsAuthenticated => !string.IsNullOrEmpty(_sessionToken) && DateTimeOffset.UtcNow < _sessionExpiry;
    public string? SessionToken => _sessionToken;

    public BetfairClient(string appKey, string certPath, string certPassword, Serilog.ILogger? logger = null)
    {
        _appKey = appKey;
        _certPath = certPath;
        _certPassword = certPassword;
        _logger = logger ?? Log.ForContext<BetfairClient>();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        // Create HttpClient with certificate handler
        var handler = new HttpClientHandler();
        try
        {
            var cert = new X509Certificate2(_certPath, _certPassword);
            handler.ClientCertificates.Add(cert);
            _logger.Information("Loaded SSL certificate from: {Path}", _certPath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load SSL certificate from: {Path}", _certPath);
            throw;
        }

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task<bool> AuthenticateAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.Information("Authenticating with Betfair API...");

            var request = new HttpRequestMessage(HttpMethod.Post, AuthUrl);
            request.Headers.Add("X-Application", _appKey);
            request.Content = new StringContent($"username={Uri.EscapeDataString("")}&password={Uri.EscapeDataString("")}", Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await _httpClient.SendAsync(request, ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Error("Authentication failed: {StatusCode} - {Content}", response.StatusCode, content);
                return false;
            }

            var authResponse = JsonSerializer.Deserialize<BetfairAuthResponse>(content, _jsonOptions);
            if (authResponse?.SessionToken == null)
            {
                _logger.Error("Authentication response missing session token");
                return false;
            }

            _sessionToken = authResponse.SessionToken;
            _sessionExpiry = DateTimeOffset.UtcNow.AddHours(8); // Betfair sessions typically last 8 hours
            
            _logger.Information("Authentication successful. Session expires: {Expiry}", _sessionExpiry);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Authentication error");
            return false;
        }
    }

    public async Task<OrderResult> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct = default)
    {
        if (!IsAuthenticated)
            throw new InvalidOperationException("Not authenticated. Call AuthenticateAsync first.");

        try
        {
            _logger.Information("Placing order: {Side} {Stake}@{Odds} on {Selection} in {Market}",
                request.Side, request.Stake, request.Odds, request.SelectionId.Value, request.MarketId.Value);

            var betfairRequest = new
            {
                marketId = request.MarketId.Value,
                instructions = new[]
                {
                    new
                    {
                        selectionId = long.Parse(request.SelectionId.Value),
                        side = request.Side.ToString().ToUpperInvariant(),
                        orderType = "LIMIT",
                        limitOrder = new
                        {
                            size = request.Stake,
                            price = request.Odds,
                            persistenceType = "LAPSE"
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(betfairRequest, _jsonOptions);
            var response = await SendBetfairRequestAsync("placeOrders", json, ct);

            var result = JsonSerializer.Deserialize<BetfairPlaceOrderResponse>(response, _jsonOptions);

            if (result?.InstructionReports?.FirstOrDefault() is { } report)
            {
                var orderStatus = report.Status == "SUCCESS" ? OrderStatus.Pending : OrderStatus.Cancelled;
                
                _logger.Information("Order placed: {OrderId}, Status: {Status}", report.BetId, orderStatus);

                return new OrderResult
                {
                    OrderId = report.BetId ?? Guid.NewGuid().ToString(),
                    Status = orderStatus,
                    MatchedSize = report.SizeMatched,
                    AveragePriceMatched = report.AveragePriceMatched,
                    Message = report.Status
                };
            }

            throw new InvalidOperationException("Invalid response from Betfair API");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error placing order");
            return new OrderResult
            {
                OrderId = Guid.NewGuid().ToString(),
                Status = OrderStatus.Cancelled,
                Message = ex.Message
            };
        }
    }

    public async Task<CancelOrderResult> CancelOrderAsync(string marketId, string orderId, CancellationToken ct = default)
    {
        if (!IsAuthenticated)
            throw new InvalidOperationException("Not authenticated");

        try
        {
            _logger.Information("Cancelling order: {OrderId} in market {Market}", orderId, marketId);

            var betfairRequest = new
            {
                marketId,
                instructions = new[]
                {
                    new { betId = orderId }
                }
            };

            var json = JsonSerializer.Serialize(betfairRequest, _jsonOptions);
            var response = await SendBetfairRequestAsync("cancelOrders", json, ct);

            var result = JsonSerializer.Deserialize<BetfairCancelOrderResponse>(response, _jsonOptions);
            var success = result?.Status == "SUCCESS";

            _logger.Information("Cancel order result: {Success}", success);

            return new CancelOrderResult
            {
                OrderId = orderId,
                Success = success,
                Message = result?.Status
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error cancelling order");
            return new CancelOrderResult
            {
                OrderId = orderId,
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<OrderResult> UpdateOrderAsync(string marketId, string orderId, decimal? newStake = null, decimal? newOdds = null, CancellationToken ct = default)
    {
        if (!IsAuthenticated)
            throw new InvalidOperationException("Not authenticated");

        try
        {
            _logger.Information("Updating order: {OrderId}, NewStake: {Stake}, NewOdds: {Odds}", orderId, newStake, newOdds);

            var betfairRequest = new
            {
                marketId,
                instructions = new[]
                {
                    new
                    {
                        betId = orderId,
                        newPrice = newOdds
                    }
                }
            };

            var json = JsonSerializer.Serialize(betfairRequest, _jsonOptions);
            var response = await SendBetfairRequestAsync("replaceOrders", json, ct);

            var result = JsonSerializer.Deserialize<BetfairPlaceOrderResponse>(response, _jsonOptions);

            if (result?.InstructionReports?.FirstOrDefault() is { } report)
            {
                return new OrderResult
                {
                    OrderId = report.BetId ?? orderId,
                    Status = report.Status == "SUCCESS" ? OrderStatus.Pending : OrderStatus.Cancelled,
                    Message = report.Status
                };
            }

            throw new InvalidOperationException("Invalid response from Betfair API");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error updating order");
            return new OrderResult
            {
                OrderId = orderId,
                Status = OrderStatus.Cancelled,
                Message = ex.Message
            };
        }
    }

    public async Task<IEnumerable<CurrentOrder>> GetCurrentOrdersAsync(string marketId, CancellationToken ct = default)
    {
        if (!IsAuthenticated)
            throw new InvalidOperationException("Not authenticated");

        try
        {
            var betfairRequest = new
            {
                marketIds = new[] { marketId }
            };

            var json = JsonSerializer.Serialize(betfairRequest, _jsonOptions);
            var response = await SendBetfairRequestAsync("listCurrentOrders", json, ct);

            var result = JsonSerializer.Deserialize<BetfairCurrentOrdersResponse>(response, _jsonOptions);

            return result?.CurrentOrders?.Select(o => new CurrentOrder
            {
                OrderId = o.BetId,
                MarketId = new MarketId(o.MarketId),
                SelectionId = new SelectionId(o.SelectionId.ToString()),
                Side = Enum.Parse<TradeSide>(o.Side, true),
                Status = ParseOrderStatus(o.Status),
                SizeRemaining = o.SizeRemaining,
                Price = o.PriceSize?.Price ?? 0
            }) ?? Enumerable.Empty<CurrentOrder>();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting current orders");
            return Enumerable.Empty<CurrentOrder>();
        }
    }

    public async Task<AccountBalance> GetAccountBalanceAsync(CancellationToken ct = default)
    {
        if (!IsAuthenticated)
            throw new InvalidOperationException("Not authenticated");

        try
        {
            var response = await SendBetfairRequestAsync("getAccountFunds", "{}", ct);
            var result = JsonSerializer.Deserialize<BetfairAccountFundsResponse>(response, _jsonOptions);

            return new AccountBalance
            {
                AvailableToBet = result?.AvailableToBetBalance ?? 0,
                Exposure = result?.Exposure ?? 0,
                Balance = result?.Balance ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting account balance");
            return new AccountBalance();
        }
    }

    private async Task<string> SendBetfairRequestAsync(string method, string jsonBody, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBaseUrl}{method}/");
        request.Headers.Add("X-Application", _appKey);
        request.Headers.Add("X-Authentication", _sessionToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.Error("Betfair API error: {StatusCode} - {Content}", response.StatusCode, content);
            throw new HttpRequestException($"Betfair API returned {response.StatusCode}");
        }

        return content;
    }

    private static OrderStatus ParseOrderStatus(string status)
    {
        return status?.ToUpperInvariant() switch
        {
            "EXECUTABLE" => OrderStatus.Pending,
            "EXECUTION_COMPLETE" => OrderStatus.Matched,
            "CANCELLED" => OrderStatus.Cancelled,
            _ => OrderStatus.Unmatched
        };
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    // Betfair API response models
    private record BetfairAuthResponse
    {
        public string? SessionToken { get; init; }
        public string? LoginStatus { get; init; }
    }

    private record BetfairPlaceOrderResponse
    {
        public string? Status { get; init; }
        public List<InstructionReport>? InstructionReports { get; init; }
    }

    private record InstructionReport
    {
        public string? Status { get; init; }
        public string? BetId { get; init; }
        public decimal SizeMatched { get; init; }
        public decimal AveragePriceMatched { get; init; }
    }

    private record BetfairCancelOrderResponse
    {
        public string? Status { get; init; }
    }

    private record BetfairCurrentOrdersResponse
    {
        public List<BetfairCurrentOrder>? CurrentOrders { get; init; }
    }

    private record BetfairCurrentOrder
    {
        public string BetId { get; init; } = string.Empty;
        public string MarketId { get; init; } = string.Empty;
        public long SelectionId { get; init; }
        public string Side { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public decimal SizeRemaining { get; init; }
        public PriceSize? PriceSize { get; init; }
    }

    private record PriceSize
    {
        public decimal Price { get; init; }
        public decimal Size { get; init; }
    }

    private record BetfairAccountFundsResponse
    {
        public decimal AvailableToBetBalance { get; init; }
        public decimal Exposure { get; init; }
        public decimal Balance { get; init; }
    }
}
