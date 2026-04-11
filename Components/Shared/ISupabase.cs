using DotNetEnv;

namespace CBOS.Components.Shared;

public abstract class ISupabase
{
    protected readonly Supabase.Client _supabase;
    public ISupabase(Supabase.Client supabase) { }
}