using CBOS.Components.Model;
using CBOS.Components.Shared;
using CBOS.Components.Pages.Admin;
using Supabase;

namespace CBOS.Components.Services;

public class AppointmentTicketSupabase : ISupabase
{
    public AppointmentTicketSupabase(Client supabase) : base(supabase)
    {
    }

    public async Task<List<Ticket>> GetAppointmentTickets()
    {
        var response = await supabase.From<Ticket>()
            .Where(t => t.Type == "appointment")
            .Get();
        Console.WriteLine($"Fetched {response.Models.Count} appointment tickets from Supabase.");
        return response.Models;
    }

    public async Task<List<Appointment>> GetAppointments(HashSet<long> appointmentIds)
    {
        if (appointmentIds == null || appointmentIds.Count == 0)
            return new List<Appointment>();

        var response = await supabase.From<Appointment>().Get();
        var filtered = response.Models.Where(a => appointmentIds.Contains(a.Id)).ToList();
        Console.WriteLine($"Fetched {filtered.Count} appointments from Supabase for ticket source IDs.");
        return filtered;
    }

    public async Task<List<CBOS.Components.Pages.Admin.AppointmentTicketModel>> GetAppointmentBoardAsync()
    {
        var ticketsResp = await supabase.From<Ticket>().Where(t => t.Type == "appointment").Get();
        var tickets = ticketsResp.Models;

        var appointmentIds = tickets.Select(t => t.SourceId).ToHashSet();
        var appointments = new List<Appointment>();
        if (appointmentIds.Count > 0)
        {
            var apResp = await supabase.From<Appointment>().Get();
            appointments = apResp.Models.Where(a => appointmentIds.Contains(a.Id)).ToList();
        }

        var userIds = tickets.Select(t => t.SubmittedBy).ToHashSet();
        var users = new List<UserProfile>();
        if (userIds.Count > 0)
        {
            var uResp = await supabase.From<UserProfile>().Get();
            users = uResp.Models.Where(u => userIds.Contains(u.Id)).ToList();
        }

        var appointmentMap = appointments.ToDictionary(a => a.Id);
        var userMap = users.ToDictionary(u => u.Id);

        var result = new List<AppointmentTicketModel>();

        foreach (var t in tickets)
        {
            if (!appointmentMap.TryGetValue(t.SourceId, out var ap))
                continue;

            var model = new AppointmentTicketModel
            {
                TicketId = t.Id,
                AppointmentId = ap.Id,
                AppointmentType = ap.AppointmentType,
                ScheduledDate = ap.ScheduledDate,
                Description = ap.Description ?? string.Empty,
                Status = t.Status ?? "Pending",
                FullName = userMap.TryGetValue(t.SubmittedBy, out var user)
                    ? string.Join(" ", new[] { user.FirstName, user.MiddleName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)))
                    : "Unknown"
            };

            result.Add(model);
        }

        Console.WriteLine($"Constructed {result.Count} appointment ticket models for admin board.");

        return result;
    }



    public async Task UpdateAppointmentTicketStatusAsync(AppointmentTicketModel ticket, string status)
    {
        if (ticket == null)
            throw new ArgumentNullException(nameof(ticket));

        // Keep the ticket and appointment statuses aligned.
        await supabase.From<Ticket>()
            .Where(t => t.Id == ticket.TicketId)
            .Set(t => t.Status, status)
            .Update();
    }

    
}