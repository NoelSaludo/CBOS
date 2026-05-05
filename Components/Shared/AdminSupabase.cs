using CBOS.Components.Model;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using static Supabase.Postgrest.Constants;

namespace CBOS.Components.Shared;

public enum AdminSupabaseCode
{
    Success,
    InvalidCredentials,
    AdminNotFound
}

public struct AdminLoginResult
{
    public Model.Admin? Admin;
    public AdminSupabaseCode Code;
    public string sessionToken;

    public AdminLoginResult()
    {
        Admin = null;
        sessionToken = string.Empty;
        Code = AdminSupabaseCode.AdminNotFound;
    }
}
// handles all supabase related operations for admin login and authentication
public class AdminSupabase : ISupabase {
    
    private readonly ILogger<AdminSupabase> logger;
    public AdminSupabase(Supabase.Client supabase,ILogger<AdminSupabase> logger) : base(supabase) {
        this.logger = logger;
     }

    public async Task SignUp(Model.Admin admin)
    {
        await supabase.From<Model.Admin>().Insert(admin);
    }
    public async Task<bool> CheckSessionKey(string sessionKey)
    {
        var session = await supabase.From<AdminSession>()
            .Where(s => s.SessonKey == sessionKey && s.expirationAt > DateTime.UtcNow).Single();

        return session != null;
    }

    /// <summary>
    /// Retrieves the admin user associated with a given session key.
    /// The session key format is a 24-character base64 string followed by the verification number.
    /// </summary>
    public async Task<Model.Admin?> GetAdminBySessionKey(string sessionKey)
    {
        if (string.IsNullOrEmpty(sessionKey) || sessionKey.Length <= 24)
            return null;

        try
        {
            string verificationNumber = sessionKey.Substring(24);
            var admin = await supabase.From<Model.Admin>()
                .Filter("verification_number", Operator.Equals, verificationNumber)
                .Single();
                
            return admin;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve admin for session key.");
            return null;
        }
    }

    public async Task DeleteSession(string sessionKey)
    {
        await supabase.From<AdminSession>()
            .Where(s => s.SessonKey == sessionKey)
            .Delete();
    }
    public async Task<AdminLoginResult> Login(string verificationNumber, string password)
    {
        AdminLoginResult result= new AdminLoginResult();
        
        var admin = await supabase.From<Model.Admin>()
            .Where(a => a.VerificationNumber == verificationNumber).Single();

        if (admin != null) {
            if (admin.Password == password)
            {
                string sessionToken = GenerateSessionToken(admin.VerificationNumber);
                result.Admin = admin;
                result.sessionToken = sessionToken;
                result.Code = AdminSupabaseCode.Success;

                await supabase.From<AdminSession>().Insert(new AdminSession
                {
                    SessonKey = sessionToken, 
                    createdAt = DateTime.UtcNow,
                    expirationAt = DateTime.UtcNow.AddHours(1)
                });

                return result;
            }
            result.Code = AdminSupabaseCode.InvalidCredentials;
            return result;
        }

        return result;
    }

    private string GenerateSessionToken(string verificationNumber)
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + verificationNumber;
    }

    public async Task LogoutAsync()
    {
        // Clear the session from the database
        // This would typically find the current session and delete it
        logger.LogInformation("User logged out");
    }

    // ── Incident Report Operations ──────────────────────────────────────────

    /// <summary>
    /// Fetches all incident reports by joining ticket (type='report'), report, and users tables.
    /// </summary>
    public async Task<List<IncidentReportDto>> GetIncidentReportsAsync()
    {
        // 1. Get all report-type tickets, newest first.
        //    Uses .Filter() instead of .Where() because ticket.type is a Postgres enum (ticket_type),
        //    which the supabase-csharp client's lambda expressions don't reliably handle.
        var ticketResponse = await supabase.From<Ticket>()
            .Filter("type", Operator.Equals, "report")
            .Order("created_at", Ordering.Descending)
            .Get();

        logger.LogInformation("Fetched {Count} report-type tickets from database.", ticketResponse.Models.Count);

        var results = new List<IncidentReportDto>();

        // ── DEBUG: Check if the report table is accessible at all ──
        var allReports = await supabase.From<Report>().Get();
        logger.LogInformation("DEBUG — Total reports accessible via API: {Count}", allReports.Models.Count);
        if (allReports.Models.Count == 0)
        {
            logger.LogWarning("DEBUG — The report table returned 0 rows. This likely means RLS is enabled without a SELECT policy for the anon/service key. Check Supabase → report → RLS policies.");
        }
        else
        {
            foreach (var r in allReports.Models)
                logger.LogInformation("DEBUG — Report row: id={Id}, title='{Title}', category='{Category}'", r.Id, r.Title, r.Category);
        }
        // ── END DEBUG ──

        // TODO: This does N+2 queries per ticket. For production scale, consider a Supabase RPC/view or batch-fetch approach.
        foreach (var ticket in ticketResponse.Models)
        {
            logger.LogInformation("Processing ticket {TicketId}, source_id={SourceId}, status={Status}",
                ticket.Id, ticket.SourceId, ticket.Status);

            // 2. Fetch the corresponding report via source_id
            var report = await supabase.From<Report>()
                .Filter("id", Operator.Equals, ticket.SourceId.ToString())
                .Single();

            if (report == null)
            {
                logger.LogWarning("No report found for ticket {TicketId} with source_id={SourceId}.", ticket.Id, ticket.SourceId);
                continue;
            }

            // 3. Fetch the user who submitted the report
            var user = await supabase.From<UserProfile>()
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

            logger.LogInformation("Mapped report {ReportId}: title='{Title}', category={Category}, complainant={Complainant}",
                report.Id, report.Title, normalizedCategory, complainant);

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

        logger.LogInformation("Returning {Count} incident reports to UI.", results.Count);
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

        var ticket = await supabase.From<Ticket>()
            .Filter("id", Operator.Equals, ticketId.ToString())
            .Single();

        if (ticket != null)
        {
            logger.LogInformation("Updating ticket {TicketId} status to {Status} by admin {AdminId}", ticket.Id, formattedStatus, adminId);
            ticket.Status = formattedStatus;
            ticket.ApprovedBy = adminId;
            await supabase.From<Ticket>().Update(ticket);

            // Update the corresponding report
            logger.LogInformation("Ticket Type is '{Type}'. Checking if we should update report.", ticket.Type);
            if (string.Equals(ticket.Type, "report", StringComparison.OrdinalIgnoreCase))
            {
                var report = await supabase.From<Report>()
                    .Filter("id", Operator.Equals, ticket.SourceId.ToString())
                    .Single();

                if (report != null)
                {
                    logger.LogInformation("Found corresponding report {ReportId}. Updating approved_at and approved_by.", report.Id);
                    report.ApprovedAt = DateTime.UtcNow;
                    report.ApprovedBy = adminId;
                    var response = await supabase.From<Report>().Update(report);
                    logger.LogInformation("Report {ReportId} update response models count: {Count}", report.Id, response.Models.Count);
                }
                else
                {
                    logger.LogWarning("Failed to find report with id {SourceId} to update.", ticket.SourceId);
                }
            }
        }
    }
}

[Table("admin_session")]
class AdminSession : BaseModel
{
    [Column("session_key")]
    public string? SessonKey {get; set;}

    [Column("created_at")]
    public DateTime createdAt {get; set;}
    
    [Column("expiration_at")]
    public DateTime expirationAt {get; set;}
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