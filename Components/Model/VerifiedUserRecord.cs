using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CBOS.Components.Model;

[Table("verified_users")]
public class VerifiedUserRecord : BaseModel
{
    [PrimaryKey("userid", false)]
    [Column("userid")]
    public long UserId { get; set; }

    [Column("user_email")]
    public string? UserEmail { get; set; }

    [Column("user_full_name")]
    public string? UserFullName { get; set; }

    [Column("verified_at")]
    public DateTime? VerifiedAt { get; set; }

    [Column("verified_by")]
    public long? VerifiedBy { get; set; }
}
