# AGENTS.md — Repo guidance for code-assistant agents

Purpose
- Short, focused instructions to help an AI coding agent be productive in this repository.

Quick commands
- Build: `dotnet build`
- Run (development): `dotnet run`
- Notes: The app expects two environment variables: `SUPABASE_URL` and `SUPABASE_KEY`.

Where to look first
- `Program.cs` — runtime wiring and Supabase client initialization.
- `CBOS.csproj` — package references (Supabase, DotNetEnv).
- `Components/Services/AppointmentSupabaseImpl.cs` — Supabase usage patterns for data access.
- `Components/Services/AuthService.cs` — authentication/Gotrue usage.
- `Components/Shared/AdminSupabase.cs` — admin helpers and service registration.
- `Components/Pages/Admin/AdminLogin.razor` and `Components/Pages/Admin/AdminAppointmentManager.razor` — admin UI entry points.
- `Components/Model/` — data model classes annotated for Supabase/Postgrest.

Important conventions & notes
- Environment variables: `Program.cs` reads `SUPABASE_URL` and `SUPABASE_KEY` and will throw if missing — set them in your environment or in appsettings when running locally.
- Secrets: Do NOT commit service keys or secrets. Use environment variables or a local secrets store.
- Supabase library: This project uses the `Supabase` NuGet family (see `CBOS.csproj`). Follow the patterns in `AppointmentSupabaseImpl.cs` and model attributes in `Components/Model/` when adding new tables or models.
- Registration: The Supabase `Client` is registered as a singleton in `Program.cs`; per-request services (e.g., `AdminSupabase`) are scoped.

What an agent should do and avoid
- Do: Link to relevant files rather than copying large blocks of documentation.
- Do: Run `dotnet build` and `dotnet run` when making runtime-affecting changes.
- Do: Search for existing patterns in `Components/Services` and `Components/Model` before adding new services.
- Avoid: Committing any credentials, or adding long duplicated docs — link instead.

Suggested next customizations
- Create a small skill for Supabase patterns (connect, initialize, model attributes).
- Create an instructions file for frontend vs backend tasks (where UI lives under `Components/Pages` vs services under `Components/Services`).

References (quick links)
- [Program.cs](Program.cs)
- [CBOS.csproj](CBOS.csproj)
- [Components/Services/AppointmentSupabaseImpl.cs](Components/Services/AppointmentSupabaseImpl.cs)
- [Components/Services/AuthService.cs](Components/Services/AuthService.cs)
- [Components/Shared/AdminSupabase.cs](Components/Shared/AdminSupabase.cs)
- [Components/Pages/Admin/AdminLogin.razor](Components/Pages/Admin/AdminLogin.razor)
- [Components/Pages/Admin/AdminAppointmentManager.razor](Components/Pages/Admin/AdminAppointmentManager.razor)
- [Components/Model](Components/Model)
