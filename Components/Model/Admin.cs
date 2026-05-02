using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CBOS.Components.Model;

[Table("admin")]
public class Admin : BaseModel
{
    [PrimaryKey("id")]
    public long Id { get; set; }
    
    [Column("first_name")]
    public string? FirstName { get; set; }
    
    [Column("middle_name")]
    public string? MiddleName  { get; set; }
    
    [Column("last_name")]
    public string? LastName { get; set; }
    
    [Column("verification_number")]
    public string? VerificationNumber { get; set; }
    
    [Column("password")]
    public string? Password { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("barangay_id")]
    public long BarangayId { get; set; }

    public string GetName()
    {
        return FirstName + " " + MiddleName + " " + LastName;
    }
    
}