using Supabase;

namespace CBOS.Components.Shared;

public sealed class AdminSupabaseClient
{
    public AdminSupabaseClient(Client client)
    {
        Client = client;
    }

    public Client Client { get; }
}

