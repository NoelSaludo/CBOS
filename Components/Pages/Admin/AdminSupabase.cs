namespace CBOS.Components.Pages.Admin;


public struct AdminLoginResult
{
    public Model.Admin? Admin;
    public bool isValid;

    public AdminLoginResult()
    {
        Admin = null;
        isValid = false;
    }
}
public class AdminSupabase {
    
    private readonly ILogger<AdminSupabase> logger;
    private readonly Supabase.Client supabase;
    public AdminSupabase(Supabase.Client supabase,ILogger<AdminSupabase> logger) {
        this.supabase = supabase;
        this.logger = logger;
     }

    public async Task<AdminLoginResult> IsAdminValid(string verificationNumber, string password)
    {
        AdminLoginResult result= new AdminLoginResult();
        
        var admin = await supabase.From<Model.Admin>()
            .Where(a => a.VerificationNumber == verificationNumber).Single();

        if (admin != null) {
            if (admin.Password == password)
            {
                result.Admin = admin;
                result.isValid = true;
                logger.LogInformation("Admin with verification number {VerificationNumber} successfully authenticated.", verificationNumber);
            }
            else
            {
                logger.LogWarning("Failed authentication attempt for verification number {VerificationNumber}: Incorrect password.", verificationNumber);
            }
        }
        else
        {
            logger.LogWarning("Failed authentication attempt: No admin found with verification number {VerificationNumber}.", verificationNumber);         
        }

        return result;
    }
}