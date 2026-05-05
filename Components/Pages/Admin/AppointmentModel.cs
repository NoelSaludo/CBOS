using System;

namespace CBOS.Components.Pages.Admin;

public class AppointmentTicketModel
{
    public long TicketId { get; set; }
    public long AppointmentId { get; set; }
    public string FullName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string AppointmentType { get; set; } = "";
    public DateTime ScheduledDate { get; set; }
    public string Description { get; set; } = "";
    // Status comes from the Ticket and is authoritative for this view
    public string Status { get; set; } = "Pending";
}
