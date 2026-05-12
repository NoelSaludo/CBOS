using CBOS.Components.Model;
using CBOS.Components.Shared;
using Supabase;

namespace CBOS.Components.Services;

/// <summary>
/// Service for fetching approved community posts and user information for the Community Feed.
/// </summary>
public class CommunityFeedSupabase : ISupabase
{
    private const string PostTicketType = "post";
    private const string StatusApproved = "Approved";

    public CommunityFeedSupabase(Client supabase) : base(supabase)
    {
    }

    /// <summary>
    /// Gets all approved community posts with user names joined in.
    /// </summary>
    public async Task<List<CommunityPostDisplay>> GetApprovedPostsWithAuthorsAsync()
    {
        // Fetch all post tickets with "post" type
        var ticketResponse = await supabase.From<Ticket>()
            .Where(t => t.Type == PostTicketType)
            .Get();

        var tickets = ticketResponse.Models
            .Where(t => NormalizeStatus(t.Status) == StatusApproved)
            .ToList();

        if (tickets.Count == 0)
            return new List<CommunityPostDisplay>();

        // Fetch all posts and users
        var postIds = tickets.Select(t => t.SourceId).ToHashSet();
        var postResponse = await supabase.From<UserPost>().Get();
        var posts = postResponse.Models.Where(p => postIds.Contains(p.Id)).ToList();

        var userIds = posts.Select(p => p.UserId).ToHashSet();
        var userResponse = await supabase.From<UserProfile>().Get();
        var users = userResponse.Models.Where(u => userIds.Contains(u.Id)).ToList();

        var postMap = posts.ToDictionary(p => p.Id);
        var userMap = users.ToDictionary(u => u.Id);

        var results = new List<CommunityPostDisplay>();

        foreach (var ticket in tickets)
        {
            if (!postMap.TryGetValue(ticket.SourceId, out var post))
                continue;

            var author = userMap.TryGetValue(post.UserId, out var user)
                ? $"{user.FirstName} {user.LastName}".Trim()
                : post.Author;

            results.Add(new CommunityPostDisplay
            {
                PostId = post.Id,
                Title = post.Title,
                Author = author,
                Content = post.Description,
                PrimaryImageUrl = GetPrimaryImageUrl(post.MediaLink),
                CreatedAt = post.CreatedAt,
                LikeCount = 0, // Can be extended to fetch from a likes table if needed
                CommentCount = 0 // Can be extended to fetch from a comments table if needed
            });
        }

        // Sort by creation date, newest first
        return results.OrderByDescending(p => p.CreatedAt).ToList();
    }

    /// <summary>
    /// Normalizes ticket status values for consistent filtering.
    /// </summary>
    private static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return "Pending";

        var normalized = status.Trim();

        if (normalized.Equals(StatusApproved, StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Accepted", StringComparison.OrdinalIgnoreCase))
            return StatusApproved;

        return normalized;
    }

    /// <summary>
    /// Extracts the first image URL from a serialized media list.
    /// </summary>
    private static string? GetPrimaryImageUrl(IReadOnlyCollection<string>? mediaUrls)
    {
        if (mediaUrls == null || mediaUrls.Count == 0)
            return null;

        return mediaUrls.FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));
    }
}

/// <summary>
/// Display model for community posts with all needed information for the UI.
/// </summary>
public class CommunityPostDisplay
{
    public long PostId { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Content { get; set; } = "";
    public string? PrimaryImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
}

