using DotNetEnv;

namespace CBOS.Components.Shared;

public abstract class ISupabase
{
    private string url;
    private string key;
    private Supabase.SupabaseOptions options;
    protected Supabase.Client supabase;

    public ISupabase()
    {
        Env.Load();
        
        url = Environment.GetEnvironmentVariable("SUPABASE_URL") ??
              throw new InvalidOperationException("SUPABASE URL NOT SET");
        key = Environment.GetEnvironmentVariable("SUPABASE_KEY") ??
              throw new InvalidOperationException("SUPABASE KEY NOT SET");
        
        options = new Supabase.SupabaseOptions { AutoConnectRealtime = true };
        
        supabase = new Supabase.Client(url, key, options);
    }
}