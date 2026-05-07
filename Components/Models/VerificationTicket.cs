using System;
using System.Text.Json.Serialization;

namespace CBOS.Models
{
    public class VerificationTicket
    {
        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long Id { get; set; }

        [JsonPropertyName("userid")]
        public long UserId { get; set; }

        [JsonPropertyName("user_email")]
        public string UserEmail { get; set; } = string.Empty;

        [JsonPropertyName("user_full_name")]
        public string UserFullName { get; set; } = string.Empty;

        [JsonPropertyName("valid_id_url")]
        public string? ValidIdUrl { get; set; }

        [JsonPropertyName("birth_certificate_url")]
        public string? BirthCertificateUrl { get; set; }

        [JsonPropertyName("created_date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("is_approved")]
        public bool? IsApproved { get; set; } = null;

        [JsonPropertyName("approved_date")]
        public DateTime? ApprovedDate { get; set; }

        [JsonPropertyName("approved_by")]
        public long? ApprovedBy { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Pending";

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }
    }
}
