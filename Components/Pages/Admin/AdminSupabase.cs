using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using CBOS.Components.Shared;
namespace CBOS.Components.Pages.Admin;

public struct AdminLoginResult
{
    public Model.Admin? Admin;
    public bool isValid = false;

    public AdminLoginResult()
    {
        Admin = null;
    }
}
public class AdminSupabase : ISupabase
{
    public AdminSupabase(Supabase.Client supabase) : base(supabase) { }

    public async Task<AdminLoginResult> IsAdminValid(string id, string password)
    {
        AdminLoginResult result = new AdminLoginResult();

        try
        {
            var admin = await _supabase.From<Model.Admin>()
                .Where(admin => admin.verificationNumber == id)
                .Single();

            if (admin == null || string.IsNullOrEmpty(admin.password)) return result;

            byte[] hashBytes = Convert.FromBase64String(admin.password);

            byte[] salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);

            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
            byte[] hash = pbkdf2.GetBytes(20);

            for (int i = 0; i < 20; i++)
                if (hashBytes[i + 16] != hash[i])
                    return result;

            result.Admin = admin;
            result.isValid = true;
            
            return result;
        }
        catch
        {
            return result;   
        }
    }
}