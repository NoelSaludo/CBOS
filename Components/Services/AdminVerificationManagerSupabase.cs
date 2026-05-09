using CBOS.Components.Model;
using CBOS.Components.Shared;
using Supabase;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace CBOS.Components.Services;

public class AdminVerificationManagerSupabase : ISupabase
{
    private const string StatusPending = "Pending";
    private const string StatusApproved = "Approved";
    private const string StatusRejected = "Rejected";
    private const string VerificationDocumentsBucket = "verification-documents";
    private const int SignedUrlExpiresInSeconds = 600;
    
    public event Action? OnVerificationChanged;

    public void NotifyVerificationChanged() => OnVerificationChanged?.Invoke();

    private readonly HttpClient _httpClient;
    private readonly string _supabaseUrl;
    private readonly string _supabaseKey;

    public AdminVerificationManagerSupabase(AdminSupabaseClient adminSupabaseClient, IHttpClientFactory httpClientFactory)
        : base(adminSupabaseClient.Client)
    {
        _httpClient = httpClientFactory.CreateClient();
        _supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL")?.TrimEnd('/')
            ?? throw new InvalidOperationException("SUPABASE_URL environment variable must be set.");
        _supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_ADMIN_KEY")
            ?? Environment.GetEnvironmentVariable("SUPABASE_KEY")
            ?? throw new InvalidOperationException("SUPABASE_KEY environment variable must be set.");
    }

    public async Task<List<VerificationTicketRecord>> GetVerificationTicketsAsync()
    {
        try
        {
            var response = await supabase.From<VerificationTicketRecord>().Get();
            var responseMessage = GetResponseMessage(response);
            var status = GetResponseStatus(responseMessage);

            if (responseMessage != null && !responseMessage.IsSuccessStatusCode)
            {
                var body = await responseMessage.Content.ReadAsStringAsync();
                Console.WriteLine($"[AdminVerification] Get tickets failed ({status}). {body}");
            }

            return response.Models;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminVerification] Exception loading tickets: {ex.Message}");
            throw;
        }
    }

    public async Task ApproveTicketAsync(VerificationTicketRecord ticket, long adminId)
    {
        await UpdateTicketStatusAsync(ticket.Id, StatusApproved, adminId, true);
        await EnsureVerifiedUserAsync(ticket, adminId);
    }

    public Task RejectTicketAsync(long ticketId, long adminId)
    {
        return UpdateTicketStatusAsync(ticketId, StatusRejected, adminId, false);
    }

    public Task SetPendingAsync(long ticketId, long adminId)
    {
        return UpdateTicketStatusAsync(ticketId, StatusPending, adminId, null, clearApproval: true);
    }

    public async Task<string?> CreateSignedDocumentUrlAsync(string? storedDocumentUrl)
    {
        var documentPath = ExtractDocumentPath(storedDocumentUrl);
        if (string.IsNullOrWhiteSpace(documentPath))
            return null;

        var requestUrl = $"{_supabaseUrl}/storage/v1/object/sign/{VerificationDocumentsBucket}/{EncodeStoragePath(documentPath)}";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Headers.Add("apikey", _supabaseKey);
        request.Headers.Add("Authorization", $"Bearer {_supabaseKey}");
        request.Content = JsonContent.Create(new { expiresIn = SignedUrlExpiresInSeconds });

        var response = await _httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[AdminVerification] Create signed document URL failed ({(int)response.StatusCode}). {body}");
            return null;
        }

        using var json = JsonDocument.Parse(body);
        if (!TryGetSignedUrl(json.RootElement, out var signedUrl) || string.IsNullOrWhiteSpace(signedUrl))
            return null;

        return BuildAbsoluteStorageUrl(signedUrl);
    }

    private async Task UpdateTicketStatusAsync(long ticketId, string status, long adminId, bool? isApproved,
        bool clearApproval = false)
    {
        var update = supabase.From<VerificationTicketRecord>()
            .Where(t => t.Id == ticketId)
            .Set(t => t.Status, status);

        if (clearApproval)
        {
            update = update
                .Set(t => t.ApprovedBy, null)
                .Set(t => t.ApprovedDate, null)
                .Set(t => t.IsApproved, null);
        }
        else
        {
            update = update
                .Set(t => t.ApprovedDate, DateTime.UtcNow)
                .Set(t => t.ApprovedBy, adminId)
                .Set(t => t.IsApproved, isApproved);
        }

        await update.Update();
        NotifyVerificationChanged();
    }

    private async Task EnsureVerifiedUserAsync(VerificationTicketRecord ticket, long adminId)
    {
        if (ticket.UserId <= 0)
            return;

        var existing = await supabase.From<VerifiedUserRecord>()
            .Where(u => u.UserId == ticket.UserId)
            .Get();

        if (existing.Models.Count > 0)
            return;

        var record = new VerifiedUserRecord
        {
            UserId = ticket.UserId,
            CreatedAt = DateTime.UtcNow
        };

        await supabase.From<VerifiedUserRecord>().Insert(record);
    }

    private static string GetResponseStatus(HttpResponseMessage? responseMessage)
    {
        if (responseMessage == null)
        {
            return "unknown";
        }

        var statusCode = (int)responseMessage.StatusCode;
        return $"{statusCode} {responseMessage.StatusCode}";
    }

    private static HttpResponseMessage? GetResponseMessage(object response)
    {
        return response.GetType().GetProperty("ResponseMessage")?.GetValue(response) as HttpResponseMessage;
    }

    private static string? ExtractDocumentPath(string? storedDocumentUrl)
    {
        if (string.IsNullOrWhiteSpace(storedDocumentUrl))
            return null;

        var value = storedDocumentUrl.Trim();
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
            value = uri.AbsolutePath.TrimStart('/');
        else
            value = value.TrimStart('/');

        value = Uri.UnescapeDataString(value);
        var parts = value.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var objectIndex = Array.FindIndex(parts, p => p.Equals("object", StringComparison.OrdinalIgnoreCase));

        if (objectIndex >= 0)
        {
            var bucketIndex = objectIndex + 1;
            if (bucketIndex < parts.Length &&
                (parts[bucketIndex].Equals("public", StringComparison.OrdinalIgnoreCase) ||
                 parts[bucketIndex].Equals("sign", StringComparison.OrdinalIgnoreCase) ||
                 parts[bucketIndex].Equals("authenticated", StringComparison.OrdinalIgnoreCase)))
            {
                bucketIndex++;
            }

            var pathIndex = bucketIndex + 1;
            return pathIndex < parts.Length ? string.Join("/", parts.Skip(pathIndex)) : null;
        }

        var bucketIndexByName = Array.FindIndex(parts, p => p.Equals(VerificationDocumentsBucket, StringComparison.OrdinalIgnoreCase));
        if (bucketIndexByName >= 0)
            return bucketIndexByName + 1 < parts.Length ? string.Join("/", parts.Skip(bucketIndexByName + 1)) : null;

        return value;
    }

    private static string EncodeStoragePath(string storagePath)
    {
        return string.Join("/", storagePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString));
    }

    private static bool TryGetSignedUrl(JsonElement root, out string signedUrl)
    {
        signedUrl = string.Empty;

        foreach (var propertyName in new[] { "signedURL", "signedUrl", "url" })
        {
            if (root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String)
            {
                signedUrl = property.GetString() ?? string.Empty;
                return true;
            }
        }

        return false;
    }

    private string BuildAbsoluteStorageUrl(string signedUrl)
    {
        if (signedUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return signedUrl;

        if (signedUrl.StartsWith("/storage/v1/", StringComparison.OrdinalIgnoreCase))
            return $"{_supabaseUrl}{signedUrl}";

        return $"{_supabaseUrl}/storage/v1/{signedUrl.TrimStart('/')}";
    }
}
