using CBOS.Components.Model;
using CBOS.Components.Shared;
using Supabase;

namespace CBOS.Components.Services;

/// <summary>
/// Service for fetching approved user posts from Supabase, ordered by recency.
/// </summary>
public class UserPostSupabaseImpl : ISupabase
{
    private const string PostTicketType = "post";
    private const string StatusApproved = "Approved";
    private const string StatusAccepted = "Accepted";

    public UserPostSupabaseImpl(Client supabase) : base(supabase)
    {
    }

    /// <summary>
    /// Gets approved user posts ordered by creation date (most recent first),
    /// including author name from the users table.
    /// </summary>
    public async Task<List<CommunityPostViewModel>> GetApprovedPostsWithAuthorsAsync()
    {
        // Fetch all post tickets with approved status
        var ticketResponse = await supabase.From<Ticket>()
            .Where(t => t.Type == PostTicketType)
            .Get();

        var approvedTickets = ticketResponse.Models
            .Where(t => IsApproved(t.Status))
            .ToList();

        if (approvedTickets.Count == 0)
            return new List<CommunityPostViewModel>();

        // Get post IDs from approved tickets
        var postIds = approvedTickets.Select(t => t.SourceId).ToHashSet();

        // Fetch all user posts and filter by approved IDs
        var postResponse = await supabase.From<UserPost>().Get();
        var posts = postResponse.Models
            .Where(p => postIds.Contains(p.Id))
            .ToList();

        if (posts.Count == 0)
            return new List<CommunityPostViewModel>();

        // Get user IDs from posts
        var userIds = posts.Select(p => p.UserId).ToHashSet();

        // Fetch all users and filter by needed IDs
        var userResponse = await supabase.From<UserProfile>().Get();
        var users = userResponse.Models
            .Where(u => userIds.Contains(u.Id))
            .ToList();

        var userMap = users.ToDictionary(u => u.Id);
        var postMap = posts.ToDictionary(p => p.Id);

        var results = new List<CommunityPostViewModel>();

        foreach (var ticket in approvedTickets)
        {
            if (!postMap.TryGetValue(ticket.SourceId, out var post))
                continue;

            var authorName = "Unknown";
            if (userMap.TryGetValue(post.UserId, out var user))
            {
                var nameParts = new[] { user.FirstName, user.MiddleName, user.LastName }
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
                authorName = string.Join(" ", nameParts);
            }

            results.Add(new CommunityPostViewModel
            {
                PostId = post.Id,
                Title = string.IsNullOrWhiteSpace(post.Title) ? "Untitled" : post.Title,
                Author = authorName,
                Description = post.Description ?? string.Empty,
                MediaLinks = post.MediaLink ?? new List<string>(),
                CreatedAt = post.CreatedAt,
                PrimaryImageUrl = GetPrimaryImageUrl(post.MediaLink)
            });
        }

        // Sort by creation date (most recent first)
        results = results.OrderByDescending(p => p.CreatedAt).ToList();

        return results;
    }

    /// <summary>
    /// Gets a user profile by their email address.
    /// </summary>
    public async Task<UserProfile?> GetUserByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var response = await supabase.From<UserProfile>()
            .Where(u => u.EmailAddress == email)
            .Get();

        return response.Models.FirstOrDefault();
    }

    /// <summary>
    /// Checks if a ticket status represents an approved state.
    /// </summary>
    private static bool IsApproved(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return false;

        var normalized = status.Trim();
        return normalized.Equals(StatusApproved, StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals(StatusAccepted, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extracts the first image URL from a media list.
    /// </summary>
    private static string? GetPrimaryImageUrl(IReadOnlyCollection<string>? mediaUrls)
    {
        if (mediaUrls == null || mediaUrls.Count == 0)
            return null;

        return mediaUrls.FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));
    }

    /// <summary>
    /// Gets all approved reports ordered by creation date (most recent first).
    /// </summary>
    public async Task<List<ReportViewModel>> GetReportsAsync()
    {
        try
        {
            var reportResponse = await supabase.From<Report>().Get();
            var approvedReports = reportResponse.Models
                .Where(r => r.ApprovedAt != null)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            if (approvedReports.Count == 0)
                return new List<ReportViewModel>();

            // Get user information
            var userIds = approvedReports.Select(r => r.UserId).ToHashSet();
            var userResponse = await supabase.From<UserProfile>().Get();
            var users = userResponse.Models
                .Where(u => userIds.Contains(u.Id))
                .ToDictionary(u => u.Id);

            var results = new List<ReportViewModel>();

            foreach (var report in approvedReports)
            {
                var authorName = "Unknown";
                if (users.TryGetValue(report.UserId, out var user))
                {
                    var nameParts = new[] { user.FirstName, user.MiddleName, user.LastName }
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();
                    authorName = string.Join(" ", nameParts);
                }

                results.Add(new ReportViewModel
                {
                    Id = report.Id,
                    Title = report.Title,
                    Author = authorName,
                    Category = report.Category ?? "General",
                    CreatedAt = report.CreatedAt
                });
            }

            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching reports: {ex.Message}");
            return new List<ReportViewModel>();
        }
    }

    /// <summary>
    /// Gets all appointments ordered by creation date (most recent first).
    /// </summary>
    public async Task<List<AppointmentViewModel>> GetAppointmentsAsync()
    {
        try
        {
            var appointmentResponse = await supabase.From<Appointment>().Get();
            var appointments = appointmentResponse.Models
                .OrderByDescending(a => a.CreatedAt)
                .ToList();

            if (appointments.Count == 0)
                return new List<AppointmentViewModel>();

            // Get user information
            var userIds = appointments.Select(a => a.UserId).ToHashSet();
            var userResponse = await supabase.From<UserProfile>().Get();
            var users = userResponse.Models
                .Where(u => userIds.Contains(u.Id))
                .ToDictionary(u => u.Id);

            var results = new List<AppointmentViewModel>();

            foreach (var appointment in appointments)
            {
                var authorName = "Unknown";
                if (users.TryGetValue(appointment.UserId, out var user))
                {
                    var nameParts = new[] { user.FirstName, user.MiddleName, user.LastName }
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();
                    authorName = string.Join(" ", nameParts);
                }

                results.Add(new AppointmentViewModel
                {
                    Id = appointment.Id,
                    Title = $"{appointment.ScheduledDate:MMMM dd, yyyy} - {appointment.AppointmentType}",
                    Type = appointment.AppointmentType,
                    ScheduledDate = appointment.ScheduledDate,
                    CreatedAt = appointment.CreatedAt
                });
            }

            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching appointments: {ex.Message}");
            return new List<AppointmentViewModel>();
        }
    }

    /// <summary>
    /// Gets all approved user posts with author information, ordered by creation date.
    /// </summary>
    public async Task<List<PostViewModel>> GetPostsAsync()
    {
        try
        {
            var posts = await GetApprovedPostsWithAuthorsAsync();
            
            return posts.Select(p => new PostViewModel
            {
                Id = p.PostId,
                Title = p.Title,
                Author = p.Author,
                CreatedAt = p.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching posts: {ex.Message}");
            return new List<PostViewModel>();
        }
    }
}

/// <summary>
/// View model for displaying community posts with author information.
/// </summary>
public class CommunityPostViewModel
{
    public long PostId { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "Unknown";
    public string Description { get; set; } = "";
    public List<string> MediaLinks { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public string? PrimaryImageUrl { get; set; }

    /// <summary>
    /// Returns a human-readable relative time string (e.g., "2h ago").
    /// </summary>
    public string GetRelativeTime()
    {
        var now = DateTime.UtcNow;
        var diff = now - CreatedAt;

        if (diff.TotalSeconds < 60)
            return "now";
        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays}d ago";

        return CreatedAt.ToString("dd/M/yyyy");
    }
}

/// <summary>
/// View model for displaying reports in incident reporting page.
/// </summary>
public class ReportViewModel
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "Unknown";
    public string Category { get; set; } = "General";
    public DateTime CreatedAt { get; set; }

    public string GetStatusBadge()
    {
        var daysSinceCreated = (DateTime.UtcNow - CreatedAt).TotalDays;
        if (daysSinceCreated < 1)
            return "Pending";
        if (daysSinceCreated < 7)
            return "Responded";
        return "Resolved";
    }
}

/// <summary>
/// View model for displaying appointments in incident reporting page.
/// </summary>
public class AppointmentViewModel
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string Type { get; set; } = "";
    public DateTime ScheduledDate { get; set; }
    public DateTime CreatedAt { get; set; }

    public string GetStatusBadge()
    {
        if (ScheduledDate < DateTime.UtcNow)
            return "Completed";
        if ((ScheduledDate - DateTime.UtcNow).TotalDays < 3)
            return "Upcoming";
        return "Approved";
    }
}

/// <summary>
/// View model for displaying posts in incident reporting page.
/// </summary>
public class PostViewModel
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "Unknown";
    public DateTime CreatedAt { get; set; }

    public string GetStatusBadge()
    {
        var hoursSinceCreated = (DateTime.UtcNow - CreatedAt).TotalHours;
        if (hoursSinceCreated < 24)
            return "Urgent";
        return "Active";
    }
}
