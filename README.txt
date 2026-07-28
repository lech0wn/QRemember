QRemember
=========

Scan a QR code, upload your photos, remember the moment together.

QRemember is a shared event photo album. Organizers create an event and
get a QR code; guests scan it to upload and browse photos in one shared
gallery, with no guest account required.

Tech stack: ASP.NET Core 10 (Razor Pages), Entity Framework Core 10 +
Npgsql, PostgreSQL (Supabase), Cloudinary (photo storage), QRCoder
(QR generation), ASP.NET Core Identity (organizer login).

For a full technical write-up (project structure, package list,
connection-string details, etc.) see TECHNICAL_DOCUMENTATION.md.


Group Members
--------------
(TBD)


How to Restore Packages and Run the Project
---------------------------------------------
Prerequisites: .NET 10 SDK (`dotnet --version` to check), Git.

1. Clone the repo and go to the web project:

       git clone https://github.com/<your-org>/qremember.git
       cd qremember/src/QRemember.Web

2. Set local secrets (ask the project owner for real values):

       dotnet user-secrets init
       dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."
       dotnet user-secrets set "ConnectionStrings:MigrationConnection" "..."
       dotnet user-secrets set "Cloudinary:CloudName" "..."
       dotnet user-secrets set "Cloudinary:ApiKey" "..."
       dotnet user-secrets set "Cloudinary:ApiSecret" "..."

   No database access? You can run against an in-memory database instead:

       dotnet user-secrets set "UseInMemoryDb" "true"

   (Development environment only. This seeds one sample event,
   EventCode "TEST123", so guest pages have something to look up.)

3. Restore and run:

       dotnet restore
       dotnet run

   The app serves over HTTP/HTTPS per Properties/launchSettings.json.


How to Create/Update the Database Using Migrations
-----------------------------------------------------
Requires the EF Core CLI tool (one-time install):

    dotnet tool install --global dotnet-ef

Run these from src/QRemember.Web:

    # Create a new migration after changing an entity/DbContext
    dotnet ef migrations add <MigrationName>

    # Apply pending migrations to the database
    dotnet ef database update

    # Roll back to a specific prior migration
    dotnet ef database update <PreviousMigrationName>

    # Remove the most recently added (not yet applied) migration
    dotnet ef migrations remove

Migrations use ConnectionStrings:MigrationConnection (a direct, non-pooled
connection) rather than the app's pooled DefaultConnection - see
AppDbContextFactory.cs.


How to Run Unit Tests
------------------------
From the repo root:

    dotnet restore tests/QRemember.Tests/QRemember.Tests.csproj
    dotnet test tests/QRemember.Tests/QRemember.Tests.csproj

Tests run against an isolated EF Core InMemory database per test
(see TestHelpers/InMemoryDbContextFactory.cs), so no real database
connection is needed to run the suite.


Default/Test Login Account
-----------------------------
Email:    annissabal@gmail.com
Password: Admin123!

There is no seeded admin account in the database - register this account
yourself via the app's Register page (/Shared/Onboarding/Register) before
using it to log in. The password above satisfies the current password
policy (8+ characters, at least one uppercase letter, one digit, and one
symbol).


Known Issues / Incomplete Features
--------------------------------------
- CloudinaryImageService.UploadAsync(string imageData) is unimplemented
  and throws NotImplementedException. The only Cloudinary method actually
  used by the app is UploadEventPhotoAsync.
- No production seed/admin bootstrap - the only seed data path is dev-only
  and only runs against the in-memory database provider, not a real
  Postgres database.
- LAN IP resolution for guest QR links (CreateEventModel.GetLocalNetworkIp)
  is a best-effort local-network trick for development only; it's skipped
  outside Development or when the request host isn't loopback, and isn't
  guaranteed to work across all network setups (multiple NICs, VPNs, etc.).
