using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CBOS.Components.Model;

[Table("report")]
public class Report : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("userid")]
    public long UserId { get; set; }

    [Column("title")]
    public string Title { get; set; } = "";

    [Column("content")]
    public string Content { get; set; } = "";

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    [Column("approved_by")]
    public long? ApprovedBy { get; set; }

    [Column("media_links")]
    public string[]? MediaLinks { get; set; }

    [Column("category")]
    public string Category { get; set; } = "";
}
