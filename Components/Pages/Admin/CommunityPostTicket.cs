namespace CBOS.Components.Pages.Admin;

// Provides the admin view model for community post tickets.
public class CommunityPostTicket
{
    public long TicketId { get; set; }
    public long PostId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string? PrimaryImageUrl { get; set; }
    public string Author { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
}
