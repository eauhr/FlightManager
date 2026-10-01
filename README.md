# FlightManager

Compare flight options, shape the results around your priorities, and continue to the provider to book. FlightManager is a portfolio travel-search application built with ASP.NET Core MVC and .NET 9.

[![.NET](https://img.shields.io/badge/.NET-9-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core/)
[![Entity Framework Core](https://img.shields.io/badge/Entity%20Framework-Core-6B3FA0)](https://learn.microsoft.com/ef/core/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-Database-CC2927?logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![SerpApi](https://img.shields.io/badge/Flights-SerpApi-1684D4)](https://serpapi.com/)
[![License](https://img.shields.io/badge/License-Portfolio%20Project-lightgrey)](#license)

## What it does

- Search live flight offers and airport suggestions through SerpApi's Google Flights engine.
- Rank itineraries by **Cheapest**, **Best Value**, or **Premium** while keeping the current trip details.
- Continue to the booking provider with the selected flight's booking handoff.
- Use the traveler’s local date and timezone when setting up a search.
- Show aviation activity data from OpenSky Network.
- Manage flights, passengers, and reservations with ASP.NET Core Identity-backed application pages.

## Built with

- **ASP.NET Core MVC / Razor Pages** on **.NET 9**
- **Entity Framework Core 9** and **SQL Server**
- **ASP.NET Core Identity** for sign-in and roles
- **JavaScript, Bootstrap, and custom CSS** for the interface
- **SerpApi** for flight search and airport lookup
- **OpenSky Network** for aviation activity data

## Run locally

### Requirements

- .NET 9 SDK
- SQL Server or SQL Server LocalDB
- A SerpApi API key for live flight search

### Configure secrets

The project has a User Secrets ID, so local credentials can stay outside the repository. From the solution root, run:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=FlightManager;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True" --project FlightManager/FlightManager.csproj
dotnet user-secrets set "SerpApi:ApiKey" "YOUR_SERPAPI_KEY" --project FlightManager/FlightManager.csproj
dotnet user-secrets set "AdminSeed:Password" "CHOOSE_A_STRONG_PASSWORD" --project FlightManager/FlightManager.csproj
```

The admin seed account is created only when `AdminSeed:Password` is configured. If the database already has an admin account, change its password in the application before deploying it.

### Create the database and start the app

```powershell
dotnet tool install --global dotnet-ef --version 9.0.12
dotnet ef database update --project FlightManager/FlightManager.csproj --startup-project FlightManager/FlightManager.csproj
dotnet run --project FlightManager/FlightManager.csproj
```

Open the local URL printed by `dotnet run`. Without a SerpApi key, the application can start, but live flight searches and airport suggestions are unavailable.

## Configuration reference

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | SQL Server database connection |
| `SerpApi:ApiKey` | Live flight search, booking resolution, and airport suggestions |
| `AdminSeed:Password` | Password used only when creating the initial admin account |

For deployment, provide these through the hosting platform’s secret manager or environment variables. Do not commit API keys, database passwords, or admin credentials.

## Project layout

```text
FlightManager/
├── Areas/Identity/       Sign-in and account pages
├── Controllers/          MVC endpoints
├── Data/                 Entity Framework contexts and data access
├── Migrations/           Database migrations
├── Models/                Application and flight-search models
├── Services/              Flight, aviation, and integration services
├── Views/                 Razor views
└── wwwroot/               CSS, JavaScript, images, and static libraries
```
