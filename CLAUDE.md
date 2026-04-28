# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Common Development Tasks

*   **Build**: To build the project, use the .NET CLI:
    ```bash
    dotnet build
    ```

*   **Run**: To run the application locally:
    ```bash
    dotnet run
    ```

*   **Run Tests**: There are no explicit test files found in the current directory listing. If tests are added, they can typically be run using:
    ```bash
    dotnet test
    ```
    To run a specific test (assuming test projects are set up):
    ```bash
    dotnet test --filter "FullyQualifiedName~YourNamespace.YourTestClass.YourTestMethod"
    ```

*   **Clean**: To clean the build output:
    ```bash
    dotnet clean
    ```

## High-Level Code Architecture and Structure

This project is a **Blazor Server application** built with .NET 10.0.

*   **Entry Point**: `Program.cs` handles the application's startup, service registration, and HTTP request pipeline configuration.
    *   It loads environment variables using `DotNetEnv`.
    *   It initializes and registers a `Supabase.Client` as a singleton for database and authentication interactions.
    *   Authentication and Authorization are configured using ASP.NET Core's built-in mechanisms.

*   **Components**: The `Components` directory houses the Blazor components.
    *   `Components/Layout`: Contains shared layout components like `MainLayout.razor` and `AdminLoginLayout.razor`.
    *   `Components/Pages`: Contains individual page components, including `Home.razor`, `Counter.razor`, and the `Admin` subdirectory for admin-specific pages.
    *   `Components/Pages/Admin/AdminLogin.razor`: This is the administrator login page, which uses `AdminSupabase` for authentication and `ProtectedSessionStorage` to store session keys.
    *   `Components/Shared/AdminSupabase.cs`: This service class encapsulates all Supabase-related operations for administrator login and session management. It handles `SignUp`, `Login`, and `CheckSessionKey`.

*   **Supabase Integration**: The application extensively uses Supabase for its backend.
    *   Environment variables `SUPABASE_URL` and `SUPABASE_KEY` are required for configuration.
    *   The `AdminSupabase` service interacts with Supabase tables for `Admin` user data and `AdminSession` for managing login sessions.

## Important Considerations

*   **Security Vulnerability**: In `Components/Shared/AdminSupabase.cs:55`, the `Login` method directly compares `admin.Password == password`. This is a critical security vulnerability as passwords should never be stored or compared in plaintext. It is highly recommended to implement password hashing and salting for secure storage and verification.