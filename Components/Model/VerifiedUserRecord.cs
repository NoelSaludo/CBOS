using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CBOS.Components.Model;

[Table("verified_users")]
public class VerifiedUserRecord : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("userid")]
    public long UserId { get; set; }
}
