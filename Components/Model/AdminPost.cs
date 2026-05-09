using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CBOS.Components.Model;

[Table("admin_post")]
public class AdminPost : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("admin_id")]
    public long AdminId { get; set; }

    [Column("barangayid")]
    public long BarangayId { get; set; }

    [Column("title")]
    public string Title { get; set; } = "";

    [Column("author")]
    public string Author { get; set; } = "";

    [Column("description")]
    public string Description { get; set; } = "";

    [Column("media_link")]
    public List<string> MediaLink { get; set; } = new List<string>();
}
