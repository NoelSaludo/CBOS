using CBOS.Components.Model;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using static Supabase.Postgrest.Constants;

namespace CBOS.Components.Shared;

public enum AdminSupabaseCode
{
    Success,
    InvalidCredentials,
    AdminNotFound
}

public struct AdminLoginResult
{
    public Model.Admin? Admin;
    public AdminSupabaseCode Code;
    public string sessionToken;

    public AdminLoginResult()
    {
        Admin = null;
        sessionToken = string.Empty;
        Code = AdminSupabaseCode.AdminNotFound;
    }
}
// handles all supabase related operations for admin login and authentication
public class AdminSupabase : ISupabase {
    
    private readonly ILogger<AdminSupabase> logger;
    public AdminSupabase(Supabase.Client supabase,ILogger<AdminSupabase> logger) : base(supabase) {
        this.logger = logger;
     }

    public async Task SignUp(Model.Admin admin)
    {
        await supabase.From<Model.Admin>().Insert(admin);
    }
    public async Task<bool> CheckSessionKey(string sessionKey)
    {
        var session = await supabase.From<AdminSession>()
            .Where(s => s.SessonKey == sessionKey && s.expirationAt > DateTime.UtcNow).Single();

        return session != null;
    }

    /// <summary>
    /// Retrieves the admin user associated with a given session key.
    /// The session key format is a 24-character base64 string followed by the verification number.
    /// </summary>
    public async Task<Model.Admin?> GetAdminBySessionKey(string sessionKey)
    {
        if (string.IsNullOrEmpty(sessionKey) || sessionKey.Length <= 24)
            return null;

        try
        {
            string verificationNumber = sessionKey.Substring(24);
            var admin = await supabase.From<Model.Admin>()
                .Filter("verification_number", Operator.Equals, verificationNumber)
                .Single();
                
            return admin;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve admin for session key.");
            return null;
        }
    }

    public async Task DeleteSession(string sessionKey)
    {
        await supabase.From<AdminSession>()
            .Where(s => s.SessonKey == sessionKey)
            .Delete();
    }
    public async Task<AdminLoginResult> Login(string verificationNumber, string password)
    {
        AdminLoginResult result= new AdminLoginResult();
        
        var admin = await supabase.From<Model.Admin>()
            .Where(a => a.VerificationNumber == verificationNumber).Single();

        if (admin != null) {
            if (admin.Password == password)
            {
                string sessionToken = GenerateSessionToken(admin.VerificationNumber);
                result.Admin = admin;
                result.sessionToken = sessionToken;
                result.Code = AdminSupabaseCode.Success;

                await supabase.From<AdminSession>().Insert(new AdminSession
                {
                    SessonKey = sessionToken, 
                    createdAt = DateTime.UtcNow,
                    expirationAt = DateTime.UtcNow.AddHours(1)
                });

                return result;
            }
            result.Code = AdminSupabaseCode.InvalidCredentials;
            return result;
        }

        return result;
    }

    private string GenerateSessionToken(string verificationNumber)
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + verificationNumber;
    }

    public async Task LogoutAsync()
    {
        // Clear the session from the database
        // This would typically find the current session and delete it
        logger.LogInformation("User logged out");
    }
}

[Table("admin_session")]
class AdminSession : BaseModel
{
    [Column("session_key")]
    public string? SessonKey {get; set;}

    [Column("created_at")]
    public DateTime createdAt {get; set;}
    
    [Column("expiration_at")]
    public DateTime expirationAt {get; set;}
}
