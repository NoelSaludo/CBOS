using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CBOS.Components.Model;

[Table("appointment")]
public class Appointment : BaseModel
{
    [PrimaryKey("id", false)]    // ← was [Column("id")], this stops id being sent on insert
    public long Id { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("appointment_type")]
    public string AppointmentType { get; set; } = "";

    [Column("scheduled_date")]
    public DateTime ScheduledDate { get; set; }

    [Column("description")]
    public string? Description { get; set; }
    
}