using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CBOS.Components.Model;

[Table("admin")]
public class Admin : BaseModel
{
    [PrimaryKey("id")]
    public int id { get; set; }
    
    [Column("firstname")]
    public string? firstName { get; set; }
    
    [Column("middlename")]
    public string? middleName  { get; set; }
    
    [Column("lastname")]
    public string? lastName { get; set; }
    
    [Column("verification_number")]
    public string? verificationNumber { get; set; }
    
    [Column("hashedpassword")]
    public string? password { get; set; }
    
    [Column("created_at")]
    public DateTime? createdAt { get; set; }

    public string GetName()
    {
        return firstName + " " + middleName + " " + lastName;
    }
    
}