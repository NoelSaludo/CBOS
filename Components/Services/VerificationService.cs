using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CBOS.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace CBOS.Services
{
    public class VerificationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _supabaseUrl;
        private readonly string _supabaseAnonKey;
        private const string StorageBucket  = "verification-documents";

        public VerificationService(HttpClient httpClient)
        {
            _httpClient = httpClient;

            // Read Supabase configuration from environment (.env loaded in Program.cs)
            _supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL")
                ?? throw new InvalidOperationException("SUPABASE_URL environment variable must be set.");
            _supabaseAnonKey = Environment.GetEnvironmentVariable("SUPABASE_KEY")
                ?? throw new InvalidOperationException("SUPABASE_KEY environment variable must be set.");
        }

        private void ApplyAuthHeaders(HttpRequestMessage request, string? accessToken)
        {
            var token = string.IsNullOrWhiteSpace(accessToken) ? _supabaseAnonKey : accessToken;
            request.Headers.Add("apikey", _supabaseAnonKey);
            request.Headers.Add("Authorization", $"Bearer {token}");
        }

        // ── Upload a file to Supabase Storage ──────────────────────────────────────
        public async Task<string?> UploadFileAsync(IBrowserFile file, string folder, string userId, string? accessToken)
        {
            try
            {
                var safeFileName = $"{userId}/{folder}/{Guid.NewGuid()}_{file.Name}";
                var uploadUrl = $"{_supabaseUrl}/storage/v1/object/{StorageBucket}/{safeFileName}";

                using var stream    = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10 MB
                using var content   = new StreamContent(stream);
                content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

                using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
                ApplyAuthHeaders(request, accessToken);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[VerificationService] Upload failed: {error}");
                    return null;
                }

                // Return the public URL
                var publicUrl = $"{_supabaseUrl}/storage/v1/object/public/{StorageBucket}/{safeFileName}";
                return publicUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VerificationService] Exception during upload: {ex.Message}");
                return null;
            }
        }

        // ── Create a verification ticket in the DB ─────────────────────────────────
        public async Task<bool> CreateTicketAsync(VerificationTicket ticket, string? accessToken)
        {
            try
            {
                var insertUrl = $"{_supabaseUrl}/rest/v1/verification_tickets";

                var payload = JsonSerializer.Serialize(ticket);
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, insertUrl);
                ApplyAuthHeaders(request, accessToken);
                request.Headers.Add("Prefer", "return=representation");
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[VerificationService] Ticket creation failed: {error}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VerificationService] Exception creating ticket: {ex.Message}");
                return false;
            }
        }

        // ── Check if user already has a pending/approved ticket ────────────────────
        public async Task<string?> GetUserVerificationStatusAsync(string userId, string? accessToken)
        {
            try
            {
                var queryUrl = $"{_supabaseUrl}/rest/v1/verification_tickets?user_id=eq.{userId}&select=status&order=created_date.desc&limit=1";

                using var request = new HttpRequestMessage(HttpMethod.Get, queryUrl);
                ApplyAuthHeaders(request, accessToken);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;

                var json    = await response.Content.ReadAsStringAsync();
                var tickets = JsonSerializer.Deserialize<VerificationTicket[]>(json);
                return tickets?.Length > 0 ? tickets[0].Status : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VerificationService] Exception checking status: {ex.Message}");
                return null;
            }
        }
    }
}
