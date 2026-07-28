# QRemember — Technical Documentation

QRemember is a shared event photo album. Organizers create an event and get a QR code; guests scan it to upload and browse photos in one gallery, without creating an account. This document covers the technical setup, structure, and operational commands for the codebase. For a quick-start summary see [README.md](README.md).

## 1. Technology Stack

| Layer | Technology |
|---|---|
| Runtime / SDK | .NET 10 |
| Web framework | ASP.NET Core Razor Pages (not MVC — no controllers, page-based routing under `Pages/`) |
| ORM | Entity Framework Core 10 |
| Database provider | `Npgsql.EntityFrameworkCore.PostgreSQL` (production/dev-against-real-DB), `Microsoft.EntityFrameworkCore.InMemory` (optional local dev without a DB) |
| Database | PostgreSQL, hosted on Supabase |
| Auth | ASP.NET Core Identity (`Microsoft.AspNetCore.Identity.EntityFrameworkCore`) — cookie auth for organizers only; guest pages are anonymous |
| Photo storage | Cloudinary (`CloudinaryDotNet`) |
| QR generation | `QRCoder` |
| Outbound email | Custom `SmtpEmailSender` implementing `IEmailSender` (used for password-reset codes) |
| Testing framework | xUnit 2.9.3 + Moq 4.20.72, run via `Microsoft.NET.Test.Sdk` |

There is no `.sln` file in the repo — the two projects (`src/QRemember.Web`, `tests/QRemember.Tests`) are built/run directly via their `.csproj` files.

## 2. Project Structure

```
QRemember/
├── README.md
├── TECHNICAL_DOCUMENTATION.md
├── src/QRemember.Web/                  # The application
│   ├── Program.cs                      # Composition root: DI registrations, middleware pipeline, dev-only seed data
│   ├── appsettings.json                # Non-secret config (logging, AppName)
│   ├── appsettings.Development.json    # Dev-only overrides (DetailedErrors, logging)
│   ├── Data/
│   │   ├── AppDbContext.cs             # EF Core DbContext: IdentityDbContext<ApplicationUser> + Events + Photos, fluent relationships
│   │   └── AppDbContextFactory.cs      # Design-time factory used by `dotnet ef` (connects via MigrationConnection, not the pooled DefaultConnection)
│   ├── Migrations/                     # EF Core migration history (see §4)
│   ├── Models/
│   │   ├── ApplicationUser.cs          # Identity user + DisplayName, CreatedAt, Events
│   │   ├── Event.cs                    # Organizer's event: code, dates, ExpiresAt (derived), AutoApprovePhotos
│   │   ├── Photo.cs                    # Uploaded photo: Status derived from IsApproved/IsHidden
│   │   └── ViewModels/                 # Small DTOs consumed by Razor views (not persisted)
│   ├── Pages/
│   │   ├── Landing.cshtml(.cs)         # Public landing page; also exposes the QR-decode-to-redirect endpoint
│   │   ├── Guest/                      # Anonymous, guest-facing pages
│   │   │   ├── GuestUpload.cshtml(.cs)       # Guest photo upload + QR scan/lookup handlers
│   │   │   └── GuestEventGallery.cshtml(.cs) # Public gallery view for an event
│   │   └── Shared/
│   │       ├── Onboarding/             # Login, Register, ForgotPassword, ResetPassword, Logout
│   │       ├── Events/                 # Organizer-only: CreateEvent, EventReady, EventDetail, MyEvents
│   │       │                           #   (this folder is locked down via AuthorizeFolder in Program.cs)
│   │       ├── Gallery/                # PhotoUpload — organizer-side upload flow
│   │       ├── _Layout.cshtml, _BrandHeader.cshtml, _EventsNavBar.cshtml, _QrScanner.cshtml  # Shared layout/partials
│   │       └── UploadForm.cshtml
│   │   └── Components/                 # Small reusable partials (_HeroSection, _PhotoGrid, _EmptyGallery)
│   ├── Services/
│   │   ├── QrCodeService.cs            # QR PNG generation (bytes + data URI) via QRCoder
│   │   ├── EventLookupService.cs       # Looks up an active event by code
│   │   ├── CloudinaryImageService.cs   # Uploads event photos to Cloudinary
│   │   └── SmtpEmailSender.cs          # IEmailSender implementation for password-reset emails
│   └── wwwroot/                        # Static assets: css/js/images + vendored Bootstrap, jQuery, jQuery Validation
└── tests/QRemember.Tests/              # xUnit test project
    ├── TestHelpers/                    # Moq factories for UserManager/SignInManager, in-memory DbContext factory, PageModel/PageContext binding helpers
    ├── Models/                         # Event, Photo tests
    ├── Services/                       # QrCodeService, EventLookupService tests
    ├── Pages/                          # Page-model handler tests, mirroring the Pages/ folder above
    └── Validation/                     # Runs Validator.TryValidateObject against page-model properties to prove
                                         #   [Required]/[EmailAddress]/[MinLength]/[Compare]/[StringLength]/[MaxLength]
                                         #   actually catch bad input, independent of handler-level ModelState checks
```

## 3. Package Dependencies & Installation

Packages are restored automatically by `dotnet restore` from the `.csproj` files below — you generally won't add these by hand, but the install commands are listed for reference.

**`src/QRemember.Web/QRemember.Web.csproj`**

```bash
cd src/QRemember.Web
dotnet add package CloudinaryDotNet --version 1.29.2
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 10.0.9
dotnet add package Microsoft.AspNetCore.Identity.UI --version 10.0.9
dotnet add package Microsoft.EntityFrameworkCore.Design --version 10.0.9
dotnet add package Microsoft.EntityFrameworkCore.InMemory --version 10.0.10
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.2
dotnet add package QRCoder --version 1.8.0
```

**`tests/QRemember.Tests/QRemember.Tests.csproj`**

```bash
cd tests/QRemember.Tests
dotnet add package coverlet.collector --version 6.0.4
dotnet add package Microsoft.NET.Test.Sdk --version 17.14.1
dotnet add package Moq --version 4.20.72
dotnet add package xunit --version 2.9.3
dotnet add package xunit.runner.visualstudio --version 3.1.4
```

The test project references the web project directly (`ProjectReference` to `QRemember.Web.csproj`), so page models and services can be tested in-process.

**EF Core CLI tool** (needed for migration commands in §4, if not already installed):

```bash
dotnet tool install --global dotnet-ef
```

## 4. Database Setup

The app targets PostgreSQL via Npgsql, normally a Supabase-hosted instance. Connection strings and other secrets are **not** committed to the repo — they're supplied via `dotnet user-secrets` locally, or environment variables in deployed environments.

### 4.1 Connection strings (placeholders)

Two connection strings are used:

- `ConnectionStrings:DefaultConnection` — used by the running app; typically Supabase's pooled ("transaction pooler") connection.
- `ConnectionStrings:MigrationConnection` — used only by `dotnet ef` design-time commands (see `AppDbContextFactory.cs`); must be a **direct** (non-pooled) connection, since pgbouncer transaction pooling doesn't reliably support EF's migration commands.

Set them from `src/QRemember.Web`:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=<host>;Port=6543;Database=<db>;Username=<user>;Password=<password>;SSL Mode=Require"
dotnet user-secrets set "ConnectionStrings:MigrationConnection" "Host=<host>;Port=5432;Database=<db>;Username=<user>;Password=<password>;SSL Mode=Require"
```

Cloudinary credentials, required for photo uploads to work, are set the same way:

```bash
dotnet user-secrets set "Cloudinary:CloudName" "<cloud-name>"
dotnet user-secrets set "Cloudinary:ApiKey" "<api-key>"
dotnet user-secrets set "Cloudinary:ApiSecret" "<api-secret>"
```

SMTP settings (used by `SmtpEmailSender` for password-reset codes) are read at runtime from configuration keys `Smtp:Host`, `Smtp:Port`, `Smtp:Username`, `Smtp:Password`, `Smtp:FromEmail`, `Smtp:FromName`, `Smtp:EnableSsl` — set these the same way if you need password reset emails to actually send locally.

### 4.2 Working without a real database (local dev only)

If you don't have Supabase access, `Program.cs` supports an in-memory database toggle:

```bash
dotnet user-secrets set "UseInMemoryDb" "true"
```

This only takes effect in the `Development` environment. It also triggers a one-time seed (see §4.4) so guest pages have something to look up.

### 4.3 Migration commands

Run from `src/QRemember.Web` (or pass `--project`/`--startup-project` from the repo root):

```bash
# Create a new migration after changing an entity/DbContext
dotnet ef migrations add <MigrationName>

# Apply pending migrations to the database (uses MigrationConnection via AppDbContextFactory)
dotnet ef database update

# Roll back to a specific prior migration
dotnet ef database update <PreviousMigrationName>

# Remove the most recently added (not-yet-applied) migration
dotnet ef migrations remove
```

Existing migrations (in `Migrations/`), in order: `InitialCreate` → `AddCreatedAtToApplicationUser` → `AddDescriptionToEvent` → `AddCaptionToPhoto` → `AddAutoApproveToEvent`.

### 4.4 Seed / test data

There's no production seed script. The only seeding is dev-only, in `Program.cs`: when running in `Development` with `UseInMemoryDb=true`, a single sample event is inserted if the (in-memory) `Events` table is empty:

- `EventCode = "TEST123"`, `Name = "Dasigsilab Sports Fest"`, `IsActive = true`

Use that code against `/Guest/GuestUpload?code=TEST123` or the landing page to exercise guest flows without a real database or an organizer account. This seed does **not** run against a real PostgreSQL database — if you're pointed at Supabase, create an event through the normal organizer flow (register → log in → Create Event) instead.

For automated tests, `tests/QRemember.Tests/TestHelpers/InMemoryDbContextFactory.cs` gives every test its own isolated EF Core InMemory database (a fresh `Guid`-named instance per call), so no shared seed data is needed there.

## 5. Run Instructions

From the repo root:

```bash
# Restore dependencies for both projects
dotnet restore src/QRemember.Web/QRemember.Web.csproj
dotnet restore tests/QRemember.Tests/QRemember.Tests.csproj

# Build
dotnet build src/QRemember.Web/QRemember.Web.csproj
dotnet build tests/QRemember.Tests/QRemember.Tests.csproj

# Run the test suite
dotnet test tests/QRemember.Tests/QRemember.Tests.csproj

# Run the app (from src/QRemember.Web, or pass --project from the root)
cd src/QRemember.Web
dotnet run
```

By default this serves over HTTPS/HTTP per `Properties/launchSettings.json`. In `Development`, HTTPS redirection is intentionally skipped (see the comment in `Program.cs`) so a guest's phone can hit the app over plain HTTP on the LAN without tripping over the untrusted local dev cert.

## 6. Known Limitations / Incomplete Features

- **`CloudinaryImageService.UploadAsync(string imageData)` is unimplemented** — it throws `NotImplementedException`. It's part of the `ICloudinaryImageService` interface but the only method actually used in the app is `UploadEventPhotoAsync`.
- **No production seed/admin bootstrap** — the only seed data path is dev-only and only runs against the in-memory provider, not against a real Postgres database.
- **LAN IP resolution for guest QR links** (`CreateEventModel.GetLocalNetworkIp`) is a best-effort UDP-socket trick for local development only; it's skipped entirely outside `Development` or when the request host isn't loopback, and has no equivalent guarantee across all network configurations (e.g., multiple NICs, VPNs).
