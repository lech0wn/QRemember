# QRemember

Scan a QR code, upload your photos, remember the moment together.

QRemember is a shared event photo album — organizers create an event, generate a QR code, and guests scan it to upload and view photos in one centralized gallery.

## Tech Stack

- ASP.NET Core 10 (Razor Pages)
- Entity Framework Core 10 + Npgsql
- Supabase (PostgreSQL)
- Cloudinary (photo storage & CDN)
- QRCoder (QR code generation)
- ASP.NET Core Identity (organizer auth)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) — verify with `dotnet --version`
- Git

## Getting Started

**1. Clone the repo**

```bash
git clone https://github.com/<your-org>/qremember.git
cd qremember/src/QRemember.Web
```

**2. Set your local secrets**

Secrets aren't stored in the repo — ask the project owner for the actual values.

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."
dotnet user-secrets set "ConnectionStrings:MigrationConnection" "..."
dotnet user-secrets set "Cloudinary:CloudName" "qremember"
dotnet user-secrets set "Cloudinary:ApiKey" "..."
dotnet user-secrets set "Cloudinary:ApiSecret" "..."
```

**3. Run the app**

```bash
dotnet restore
dotnet run
```
