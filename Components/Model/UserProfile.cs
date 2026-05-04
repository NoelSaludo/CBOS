using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CBOS.Components.Model;

[Table("users")]
public class UserProfile : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("emailaddress")]
    public string EmailAddress { get; set; } = "";

    [Column("firstname")]
    public string FirstName { get; set; } = "";

    [Column("middlename")]
    public string? MiddleName { get; set; }

    [Column("lastname")]
    public string LastName { get; set; } = "";

    [Column("phonenumber")]
    public string? PhoneNumber { get; set; }

    [Column("birthdate")]
    public DateTime? BirthDate { get; set; }

    [Column("age")]
    public long? Age { get; set; }

    [Column("physicaladdress")]
    public string? PhysicalAddress { get; set; }
    
    [Column("barangay_id")]
    public long? BarangayId { get; set; }
    
}