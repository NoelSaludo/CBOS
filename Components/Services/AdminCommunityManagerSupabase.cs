using CBOS.Components.Model;
using CBOS.Components.Pages.Admin;
using CBOS.Components.Shared;
using Supabase;

namespace CBOS.Components.Services;

// Handles admin community post ticket operations with Supabase.
public class AdminCommunityManagerSupabase : ISupabase
{
    private const string PostTicketType = "post";
    private const string StatusPending = "Pending";
    private const string StatusRejected = "Rejected";
    private const string StatusApproved = "Approved";
    private const string StatusAccepted = "Accepted";

    // Raised whenever community post data changes so subscribers can refresh.
    public event Action? OnCommunityPostsChanged;

    // Notifies all listeners that community post data has been updated.
    public void NotifyCommunityPostsChanged() => OnCommunityPostsChanged?.Invoke();

    // Creates the Supabase-backed community manager.
    public AdminCommunityManagerSupabase(Client supabase) : base(supabase)
    {
    }

    // Gets all community post tickets across all statuses (for dashboard analytics).
    public Task<List<CommunityPostTicket>> GetAllCommunityPostTicketsAsync()
    {
        return GetPostTicketsByStatusesAsync(new[] { StatusPending, StatusApproved, StatusRejected });
    }

    // Gets approved community post tickets.
    public Task<List<CommunityPostTicket>> GetApprovedPostsAsync()
    {
        return GetPostTicketsByStatusesAsync(new[] { StatusApproved });
    }

    // Gets pending community post tickets.
    public Task<List<CommunityPostTicket>> GetPendingPostTicketsAsync()
    {
        return GetPostTicketsByStatusesAsync(new[] { StatusPending });
    }

    // Gets rejected community post tickets.
    public Task<List<CommunityPostTicket>> GetRejectedPostTicketsAsync()
    {
        return GetPostTicketsByStatusesAsync(new[] { StatusRejected });
    }

    // Updates the status of a post ticket with admin info.
    public async Task UpdatePostTicketStatusAsync(long ticketId, string status, long AdminId)
    {
        if (ticketId <= 0)
            throw new ArgumentException("Invalid ticket ID.", nameof(ticketId));

        await supabase.From<Ticket>()
            .Where(t => t.Id == ticketId)
            .Set(t => t.Status, status)
            .Set(t => t.ApprovedBy, AdminId)
            .Set(t => t.ApprovedAt, DateTime.UtcNow)
            .Update();
    }

    // Updates the status of a post ticket to rejected with admin info. 
    public async Task UpdatePostTicketStatusToApprovedAsync(long ticketId, long adminId)
    {
        if (ticketId <= 0)
            throw new ArgumentException("Invalid ticket ID.", nameof(ticketId));

        await supabase.From<Ticket>()
            .Where(t => t.Id == ticketId)
            .Set(t => t.Status, StatusApproved)
            .Set(t => t.ApprovedBy, adminId)
            .Set(t => t.ApprovedAt, DateTime.UtcNow)
            .Update();
    }

    // Updates the status of a post ticket to rejected with admin info.
    public async Task UpdatePostTicketStatusToRejectedAsync(long ticketId, long adminId)
    {
        if (ticketId <= 0)
            throw new ArgumentException("Invalid ticket ID.", nameof(ticketId));

        await supabase.From<Ticket>()
            .Where(t => t.Id == ticketId)
            .Set(t => t.Status, StatusRejected)
            .Set(t => t.ApprovedBy, adminId)
            .Set(t => t.ApprovedAt, DateTime.UtcNow)
            .Update();
    }

    // Updates the status of a post ticket to pending with admin info.
    public async Task UpdatePostTicketStatusToPendingAsync(long ticketId, long adminId)
    {
        if (ticketId <= 0)
            throw new ArgumentException("Invalid ticket ID.", nameof(ticketId));

        await supabase.From<Ticket>()
            .Where(t => t.Id == ticketId)
            .Set(t => t.Status, StatusPending)
            .Set(t => t.ApprovedBy, null)
            .Set(t => t.ApprovedAt, null)
            .Update();
    }

    // Loads post tickets filtered by a normalized status set.
    private async Task<List<CommunityPostTicket>> GetPostTicketsByStatusesAsync(IReadOnlyCollection<string> allowedStatuses)
    {
        var ticketResponse = await supabase.From<Ticket>()
            .Where(t => t.Type == PostTicketType)
            .Get();

        var tickets = ticketResponse.Models
            .Where(t => allowedStatuses.Contains(NormalizeStatus(t.Status)))
            .ToList();

        if (tickets.Count == 0)
            return new List<CommunityPostTicket>();

        var postIds = tickets.Select(t => t.SourceId).ToHashSet();
        var postResponse = await supabase.From<UserPost>().Get();
        var posts = postResponse.Models.Where(p => postIds.Contains(p.Id)).ToList();
        var postMap = posts.ToDictionary(p => p.Id);

        var results = new List<CommunityPostTicket>();

        foreach (var ticket in tickets)
        {
            if (!postMap.TryGetValue(ticket.SourceId, out var post))
                continue;

            results.Add(new CommunityPostTicket
            {
                TicketId = ticket.Id,
                PostId = post.Id,
                Title = string.IsNullOrWhiteSpace(post.Title) ? "Untitled" : post.Title,
                Author = string.IsNullOrWhiteSpace(post.Author) ? "Unknown" : post.Author,
                Description = post.Description ?? string.Empty,
                PrimaryImageUrl = GetPrimaryImageUrl(post.MediaLink),
                Status = NormalizeStatus(ticket.Status),
                CreatedAt = post.CreatedAt
            });
        }

        return results;
    }

    // Normalizes ticket status values for consistent filtering.
    private static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return StatusPending;

        var normalized = status.Trim();

        if (normalized.Equals(StatusApproved, StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals(StatusAccepted, StringComparison.OrdinalIgnoreCase))
            return StatusApproved;

        if (normalized.Equals(StatusRejected, StringComparison.OrdinalIgnoreCase))
            return StatusRejected;

        return StatusPending;
    }

    // Extracts the first image URL from a serialized media list.
    private static string? GetPrimaryImageUrl(IReadOnlyCollection<string>? mediaUrls)
    {
        if (mediaUrls == null || mediaUrls.Count == 0)
            return null;

        return mediaUrls.FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));
    }
}
