namespace CBOS.Components.Pages.Admin
{
    public class RegistrationData
    {
        public string Month { get; set; } = "";
        public int Count { get; set; }
    }

    public class CategoryData
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    public class AppointmentTrendData
    {
        public string Month { get; set; } = "";
        public string Category { get; set; } = "";
        public int Count { get; set; }
    }

    public class UserVerificationTrendData
    {
        public string Month { get; set; } = "";
        public string Status { get; set; } = "";
        public int Count { get; set; }
    }

    public class IncidentTrendData
    {
        public string Month { get; set; } = "";
        public string Category { get; set; } = "";
        public int Count { get; set; }
    }
}
