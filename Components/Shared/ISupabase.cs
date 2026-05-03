using Supabase;

namespace CBOS.Components.Shared;
public abstract class ISupabase
{
    protected Supabase.Client supabase { get; set; }
    public ISupabase(Supabase.Client supabase)
    {
        this.supabase = supabase;
     }
}