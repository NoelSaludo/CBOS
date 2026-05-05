using Supabase;
using CBOS.Components.Model;
using static Supabase.Postgrest.Constants;

namespace CBOS.Components.Services;

public class IncidentManagementService
{
    private readonly Client _supabase;
    private readonly ILogger<IncidentManagementService> _logger;

    public event Action? OnIncidentsChanged;

    public IncidentManagementService(Client supabase, ILogger<IncidentManagementService> logger)
    {
        _supabase = supabase;
        _logger = logger;
    }

    /// <summary>
    /// Fetches all incident reports by joining ticket (type='report'), report, and users tables.
    /// </summary>
    public async Task<List<IncidentReportDto>> GetIncidentReportsAsync()
    {
        // 1. Get all report-type tickets, newest first.
        //    Uses .Filter() instead of .Where() because ticket.type is a Postgres enum (ticket_type),
        //    which the supabase-csharp client's lambda expressions don't reliably handle.
        var ticketResponse = await _supabase.From<Ticket>()
            .Filter("type", Operator.Equals, "report")
            .Order("created_at", Ordering.Descending)
            .Get();

        var results = new List<IncidentReportDto>();

        // TODO: This does N+2 queries per ticket. For production scale, consider a Supabase RPC/view or batch-fetch approach.
        foreach (var ticket in ticketResponse.Models)
        {
            // 2. Fetch the corresponding report via source_id
            var report = await _supabase.From<Report>()
                .Filter("id", Operator.Equals, ticket.SourceId.ToString())
                .Single();

            if (report == null)
            {
                _logger.LogWarning("No report found for ticket {TicketId} with source_id={SourceId}.", ticket.Id, ticket.SourceId);
                continue;
            }

            // 3. Fetch the user who submitted the report
            var user = await _supabase.From<UserProfile>()
                .Filter("id", Operator.Equals, report.UserId.ToString())
                .Single();

            // 4. Resolve media URLs — treat values as direct public URLs.
            //    If they are relative storage paths, replace this with
            //    supabase.Storage.From("bucket").GetPublicUrl(path)
            var mediaUrls = report.MediaLinks ?? Array.Empty<string>();

            // 5. Normalize category: DB stores lowercase (e.g. "complaint"),
            //    UI expects uppercase (e.g. "COMPLAINT").
            //    Also handle 'others' → 'OTHER' to match existing UI filter value.
            var normalizedCategory = report.Category.ToUpper() switch
            {
                "OTHERS" => "OTHER",
                var c => c
            };

            var complainant = user != null
                ? $"{user.FirstName} {user.LastName}".Trim()
                : "Unknown User";

            results.Add(new IncidentReportDto
            {
                ReportId = report.Id,
                TicketId = ticket.Id,
                FormattedId = $"#INC-{report.Id}",
                Complainant = complainant,
                Title = report.Title,
                Description = report.Content,
                Category = normalizedCategory,
                Date = report.CreatedAt,
                Status = ticket.Status.ToUpper(),
                MediaLinks = mediaUrls
            });
        }

        return results;
    }

    /// <summary>
    /// Updates the status of a ticket record and its corresponding report.
    /// </summary>
    public async Task UpdateTicketStatusAsync(long ticketId, string newStatus, long? adminId = null)
    {
        if (string.IsNullOrEmpty(newStatus)) return;
        
        // Format status to Title Case (e.g. "RESOLVED" -> "Resolved")
        string formattedStatus = char.ToUpper(newStatus[0]) + newStatus.Substring(1).ToLower();

        var ticket = await _supabase.From<Ticket>()
            .Filter("id", Operator.Equals, ticketId.ToString())
            .Single();

        if (ticket != null)
        {
            _logger.LogInformation("Updating ticket {TicketId} status to {Status} by admin {AdminId}", ticket.Id, formattedStatus, adminId);
            ticket.Status = formattedStatus;
            ticket.ApprovedBy = adminId;
            await _supabase.From<Ticket>().Update(ticket);

            // Update the corresponding report
            _logger.LogInformation("Ticket Type is '{Type}'. Checking if we should update report.", ticket.Type);
            if (string.Equals(ticket.Type, "report", StringComparison.OrdinalIgnoreCase))
            {
                var report = await _supabase.From<Report>()
                    .Filter("id", Operator.Equals, ticket.SourceId.ToString())
                    .Single();

                if (report != null)
                {
                    _logger.LogInformation("Found corresponding report {ReportId}. Updating approved_at and approved_by.", report.Id);
                    report.ApprovedAt = DateTime.UtcNow;
                    report.ApprovedBy = adminId;
                    var response = await _supabase.From<Report>().Update(report);
                    _logger.LogInformation("Report {ReportId} update response models count: {Count}", report.Id, response.Models.Count);
                }
                else
                {
                    _logger.LogWarning("Failed to find report with id {SourceId} to update.", ticket.SourceId);
                }
            }

            OnIncidentsChanged?.Invoke();
        }
    }
}

/// <summary>
/// Data transfer object for passing incident report data from service to UI layer.
/// Combines data from the report, ticket, and users tables.
/// </summary>
public class IncidentReportDto
{
    public long ReportId { get; set; }
    public long TicketId { get; set; }
    public string FormattedId { get; set; } = "";
    public string Complainant { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public DateTime Date { get; set; }
    public string Status { get; set; } = "";
    public string[] MediaLinks { get; set; } = Array.Empty<string>();
}
