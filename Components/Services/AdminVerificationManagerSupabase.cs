using CBOS.Components.Model;
using CBOS.Components.Shared;
using Supabase;
using System.Net.Http;

namespace CBOS.Components.Services;

public class AdminVerificationManagerSupabase : ISupabase
{
    private const string StatusPending = "Pending";
    private const string StatusApproved = "Approved";
    private const string StatusRejected = "Rejected";

    public AdminVerificationManagerSupabase(AdminSupabaseClient adminSupabaseClient) : base(adminSupabaseClient.Client)
    {
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

            Console.WriteLine($"[AdminVerification] Get tickets OK ({status}). Count: {response.Models.Count}.");
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

    public Task RejectTicketAsync(Guid ticketId, long adminId)
    {
        return UpdateTicketStatusAsync(ticketId, StatusRejected, adminId, false);
    }

    public Task SetPendingAsync(Guid ticketId, long adminId)
    {
        return UpdateTicketStatusAsync(ticketId, StatusPending, adminId, null, clearApproval: true);
    }

    private async Task UpdateTicketStatusAsync(Guid ticketId, string status, long adminId, bool? isApproved,
        bool clearApproval = false)
    {
        var update = supabase.From<VerificationTicketRecord>()
            .Where(t => t.Id == ticketId)
            .Set(t => t.Status, status)
            .Set(t => t.ApprovedDate, DateTime.UtcNow)
            .Set(t => t.ApprovedBy, adminId.ToString())
            .Set(t => t.IsApproved, isApproved);

        if (clearApproval)
        {
            update = update
                .Set(t => t.ApprovedBy, null)
                .Set(t => t.ApprovedDate, null)
                .Set(t => t.IsApproved, null);
        }

        await update.Update();
    }

    private async Task EnsureVerifiedUserAsync(VerificationTicketRecord ticket, long adminId)
    {
        if (string.IsNullOrWhiteSpace(ticket.UserId))
            return;

        var existing = await supabase.From<VerifiedUserRecord>()
            .Where(u => u.UserId == ticket.UserId)
            .Get();

        if (existing.Models.Count > 0)
            return;

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
}
