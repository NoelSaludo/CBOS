using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CBOS.Components.Model;

[Table("barangay")]
public class BarangayRecord : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = "";

    [Column("address")]
    public string Address { get; set; } = "";
}
