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
        private const string SupabaseUrl    = "https://rcvroqmhnlavcykviwkf.supabase.co";
        private const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InJjdnJvcW1obmxhdmN5a3Zpd2tmIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzU1NTMzMzIsImV4cCI6MjA5MTEyOTMzMn0.BaXpi731ybZ4WIOAiI3HAs5vEYPIIYooQAKjnjYDqmc"; 
        private const string StorageBucket  = "verification-documents";  

        public VerificationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // ── Upload a file to Supabase Storage ──────────────────────────────────────
        public async Task<string?> UploadFileAsync(IBrowserFile file, string folder, string userId)
        {
            try
            {
                var safeFileName = $"{userId}/{folder}/{Guid.NewGuid()}_{file.Name}";
                var uploadUrl = $"{SupabaseUrl}/storage/v1/object/{StorageBucket}/{safeFileName}";

                using var stream    = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10 MB
                using var content   = new StreamContent(stream);
                content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

                using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
                request.Headers.Add("apikey",        SupabaseAnonKey);
                request.Headers.Add("Authorization", $"Bearer {SupabaseAnonKey}");
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[VerificationService] Upload failed: {error}");
                    return null;
                }

                // Return the public URL
                var publicUrl = $"{SupabaseUrl}/storage/v1/object/public/{StorageBucket}/{safeFileName}";
                return publicUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VerificationService] Exception during upload: {ex.Message}");
                return null;
            }
        }

        // ── Create a verification ticket in the DB ─────────────────────────────────
        public async Task<bool> CreateTicketAsync(VerificationTicket ticket)
        {
            try
            {
                var insertUrl = $"{SupabaseUrl}/rest/v1/verification_tickets";

                var payload = JsonSerializer.Serialize(ticket);
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, insertUrl);
                request.Headers.Add("apikey",        SupabaseAnonKey);
                request.Headers.Add("Authorization", $"Bearer {SupabaseAnonKey}");
                request.Headers.Add("Prefer",        "return=representation");
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
        public async Task<string?> GetUserVerificationStatusAsync(string userId)
        {
            try
            {
                var queryUrl = $"{SupabaseUrl}/rest/v1/verification_tickets?user_id=eq.{userId}&select=status&order=created_date.desc&limit=1";

                using var request = new HttpRequestMessage(HttpMethod.Get, queryUrl);
                request.Headers.Add("apikey",        SupabaseAnonKey);
                request.Headers.Add("Authorization", $"Bearer {SupabaseAnonKey}");

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
