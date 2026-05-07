using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CBOS.Components.Model;

[Table("verification_tickets")]
public class VerificationTicketRecord : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("user_email")]
    public string UserEmail { get; set; } = string.Empty;

    [Column("user_full_name")]
    public string UserFullName { get; set; } = string.Empty;

    [Column("valid_id_url")]
    public string? ValidIdUrl { get; set; }

    [Column("birth_certificate_url")]
    public string? BirthCertificateUrl { get; set; }

    [Column("created_date")]
    public DateTime CreatedDate { get; set; }

    [Column("is_approved")]
    public bool? IsApproved { get; set; }

    [Column("approved_date")]
    public DateTime? ApprovedDate { get; set; }

    [Column("approved_by")]
    public long? ApprovedBy { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    [Column("remarks")]
    public string? Remarks { get; set; }
}
