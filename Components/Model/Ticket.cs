using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CBOS.Components.Model;

[Table("ticket")]
public class Ticket : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("type")]
    public string Type { get; set; } = "";

    [Column("source_id")]
    public long SourceId { get; set; }

    [Column("submitted_by")]
    public long SubmittedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("approved_by")]
    public long? ApprovedBy { get; set; }
}