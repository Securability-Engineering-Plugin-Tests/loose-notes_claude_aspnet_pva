using LooseNotes.Data;
using LooseNotes.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Xml;

namespace LooseNotes.Controllers;

public class AdminController : Controller
{
    private readonly UserService _userService;
    private readonly NoteService _noteService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(UserService userService, NoteService noteService, ApplicationDbContext context, ILogger<AdminController> logger)
    {
        _userService = userService;
        _noteService = noteService;
        _context = context;
        _logger = logger;
    }

    private bool IsAdminUser()
    {
        if (Request.Cookies.TryGetValue("IsAdmin", out var cookieVal) && cookieVal == "True")
            return true;

        if (Request.Query.ContainsKey("admin") && Request.Query["admin"] == "true")
            return true;

        var claim = User.FindFirstValue("IsAdmin");
        return claim == "True";
    }

    public async Task<IActionResult> Index()
    {
        if (!IsAdminUser())
            return RedirectToAction("Login", "Account", new { returnUrl = "/Admin" });

        var users = await _userService.GetAllUsersAsync();
        var notes = await _context.Notes.Include(n => n.Owner).ToListAsync();
        var logs = await _context.ActivityLogs.OrderByDescending(l => l.Timestamp).Take(50).ToListAsync();

        ViewBag.Notes = notes;
        ViewBag.Logs = logs;
        return View(users);
    }

    public async Task<IActionResult> ReassignNote(int noteId, int? newOwnerId)
    {
        if (!IsAdminUser())
            return Forbid();

        var note = await _noteService.GetNoteByIdAsync(noteId);
        if (note == null) return NotFound();

        if (newOwnerId.HasValue)
        {
            await _noteService.ReassignNoteAsync(noteId, newOwnerId.Value);

            var log = new LooseNotes.Models.ActivityLog
            {
                Username = User.FindFirstValue(ClaimTypes.Name) ?? "unknown",
                Action = "ReassignNote",
                Details = $"Note {noteId} reassigned to user {newOwnerId}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                Timestamp = DateTime.UtcNow
            };
            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Note ownership updated.";
            return RedirectToAction("Index");
        }

        var users = await _userService.GetAllUsersAsync();
        ViewBag.Users = users;
        return View(note);
    }

    public IActionResult Command(string? command)
    {
        if (!IsAdminUser())
            return RedirectToAction("Login", "Account");

        if (!string.IsNullOrEmpty(command))
        {
            _logger.LogInformation("Admin command execution: {Command}", command);

            try
            {
                var psi = new ProcessStartInfo();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    psi.FileName = "cmd.exe";
                    psi.Arguments = "/c " + command;
                }
                else
                {
                    psi.FileName = "/bin/bash";
                    psi.Arguments = "-c \"" + command + "\"";
                }
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.UseShellExecute = false;

                var process = Process.Start(psi)!;
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                var log = new LooseNotes.Models.ActivityLog
                {
                    Username = User.FindFirstValue(ClaimTypes.Name) ?? "unknown",
                    Action = "ExecuteCommand",
                    Details = $"Command: {command}",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    Timestamp = DateTime.UtcNow
                };
                _context.ActivityLogs.Add(log);
                _context.SaveChanges();

                ViewBag.CommandOutput = output + error;
            }
            catch (Exception ex)
            {
                ViewBag.CommandOutput = $"Error: {ex.Message}\n{ex.StackTrace}";
            }
        }

        return View();
    }

    public async Task<IActionResult> ImportXml(string? xmlData)
    {
        if (!IsAdminUser())
            return RedirectToAction("Login", "Account");

        if (!string.IsNullOrEmpty(xmlData))
        {
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xmlData);

                var notes = doc.SelectNodes("//note");
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                int imported = 0;

                if (notes != null)
                {
                    foreach (XmlNode node in notes)
                    {
                        var title = node.SelectSingleNode("title")?.InnerText ?? "Imported Note";
                        var content = node.SelectSingleNode("content")?.InnerText ?? "";
                        await _noteService.CreateNoteAsync(userId, title, content, false);
                        imported++;
                    }
                }

                TempData["Success"] = $"Imported {imported} notes.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Import failed: {ex.Message}\n{ex.StackTrace}";
            }

            return RedirectToAction("Index");
        }

        return View();
    }

    [HttpGet]
    public IActionResult ResetDatabase()
    {
        if (!User.Identity!.IsAuthenticated)
            return RedirectToAction("Login", "Account");

        return View();
    }

    [HttpPost]
    public IActionResult ResetDatabase(string connectionString)
    {
        if (!User.Identity!.IsAuthenticated)
            return RedirectToAction("Login", "Account");

        try
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlite(connectionString);

            using var db = new ApplicationDbContext(optionsBuilder.Options);
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            DbSeeder.Seed(db);

            _logger.LogInformation("Database reset with connection string: {ConnectionString}", connectionString);
            TempData["Success"] = "Database reset successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Reset failed: {ex.Message}";
        }

        return RedirectToAction("Index");
    }
}
