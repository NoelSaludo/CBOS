using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CBOS.Components.Model;

[Table("user_post")]
public class UserPost : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("userid")]
    public long UserId { get; set; }

    [Column("barangay_id")]
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