# Loose Notes

A multi-user note-taking web application built with ASP.NET Core 8 MVC and Entity Framework Core (SQLite).

## Features

- User registration, login, and profile management
- Password reset via token
- Create, edit, delete, and view notes
- Public and private note visibility
- File attachments on notes
- Note sharing via generated share links
- Star ratings and comments on notes
- Search notes by keyword
- Top-rated notes view
- Admin dashboard with user management, note reassignment, command execution, and XML import

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Setup & Run

1. **Restore dependencies and run:**

   ```bash
   cd /path/to/rawdog
   dotnet run
   ```

   The application will create a SQLite database (`loosenotes.db`) automatically on first run and seed it with sample data.

2. **Access the application:**

   Open your browser to `https://localhost:5001` or `http://localhost:5000`
   (the exact port will be shown in the terminal output).

## Default Accounts

| Username | Password   | Role  |
|----------|------------|-------|
| admin    | admin123   | Admin |
| alice    | password   | User  |
| bob      | password   | User  |

## Project Structure

```
LooseNotes/
├── Controllers/
│   ├── HomeController.cs       # Public home page, top-rated notes
│   ├── AccountController.cs    # Registration, login, profile, password reset
│   ├── NotesController.cs      # CRUD for notes, sharing, file downloads
│   ├── AdminController.cs      # Admin dashboard, command exec, XML import
│   └── RatingsController.cs    # Note rating submission
├── Data/
│   ├── ApplicationDbContext.cs # EF Core DbContext
│   └── DbSeeder.cs             # Initial seed data
├── Models/
│   ├── User.cs
│   ├── Note.cs
│   ├── Rating.cs
│   ├── Attachment.cs
│   ├── ShareToken.cs
│   ├── PasswordResetToken.cs
│   ├── ActivityLog.cs
│   └── ViewModels/             # Form view models
├── Services/
│   ├── UserService.cs          # User auth and management
│   ├── NoteService.cs          # Note CRUD, search, sharing, ratings
│   └── FileService.cs          # File upload and download
├── Views/                      # Razor views
│   ├── Shared/_Layout.cshtml
│   ├── Home/
│   ├── Account/
│   ├── Notes/
│   └── Admin/
├── wwwroot/                    # Static files
├── appsettings.json
└── Program.cs
```

## Notes

- The SQLite database file (`loosenotes.db`) and uploaded files (`uploads/`) are created in the project root at runtime.
- This application is intended for **educational and demonstration purposes only** and should not be deployed in a production environment.
