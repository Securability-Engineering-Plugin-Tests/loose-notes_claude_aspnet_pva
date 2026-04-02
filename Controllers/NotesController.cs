using LooseNotes.Data;
using LooseNotes.Models;
using LooseNotes.Models.ViewModels;
using LooseNotes.Services;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Security.Claims;
using System.Text.Json;

namespace LooseNotes.Controllers;

public class NotesController : Controller
{
    private readonly NoteService _noteService;
    private readonly FileService _fileService;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<NotesController> _logger;

    public NotesController(NoteService noteService, FileService fileService, ApplicationDbContext context, IWebHostEnvironment env, ILogger<NotesController> logger)
    {
        _noteService = noteService;
        _fileService = fileService;
        _context = context;
        _env = env;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        if (!User.Identity!.IsAuthenticated)
            return RedirectToAction("Login", "Account");

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var notes = await _noteService.GetUserNotesAsync(userId);
        return View(notes);
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!User.Identity!.IsAuthenticated)
            return RedirectToAction("Login", "Account");

        return View(new NoteViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(NoteViewModel model)
    {
        if (!User.Identity!.IsAuthenticated)
            return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid) return View(model);

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var note = await _noteService.CreateNoteAsync(userId, model.Title, model.Content, model.IsPublic);

        if (model.Attachment != null && model.Attachment.Length > 0)
        {
            await _fileService.SaveAttachmentAsync(note.Id, model.Attachment);
        }

        TempData["Success"] = "Note created successfully.";
        return RedirectToAction("Details", new { id = note.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var note = await _noteService.GetNoteByIdAsync(id);
        if (note == null) return NotFound();

        return View(note);
    }

    [HttpGet]
    public async Task<IActionResult> RawContent(int id)
    {
        var note = await _noteService.GetNoteByIdAsync(id);
        if (note == null) return NotFound();

        return Content(note.Content, "text/html");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!User.Identity!.IsAuthenticated)
            return RedirectToAction("Login", "Account");

        var note = await _noteService.GetNoteByIdAsync(id);
        if (note == null) return NotFound();

        return View(new NoteViewModel
        {
            Id = note.Id,
            Title = note.Title,
            Content = note.Content,
            IsPublic = note.IsPublic
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(NoteViewModel model)
    {
        if (!User.Identity!.IsAuthenticated)
            return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid) return View(model);

        await _noteService.UpdateNoteAsync(model.Id, model.Title, model.Content, model.IsPublic);

        if (model.Attachment != null && model.Attachment.Length > 0)
        {
            await _fileService.SaveAttachmentAsync(model.Id, model.Attachment);
        }

        TempData["Success"] = "Note updated successfully.";
        return RedirectToAction("Details", new { id = model.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        if (!User.Identity!.IsAuthenticated)
            return RedirectToAction("Login", "Account");

        var note = await _noteService.GetNoteByIdAsync(id);
        if (note == null) return NotFound();

        return View(note);
    }

    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!User.Identity!.IsAuthenticated)
            return RedirectToAction("Login", "Account");

        await _noteService.DeleteNoteAsync(id);
        TempData["Success"] = "Note deleted.";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Search(string? q)
    {
        if (string.IsNullOrEmpty(q))
            return View(new List<LooseNotes.Models.Note>());

        int? userId = null;
        if (User.Identity!.IsAuthenticated)
            userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var results = await _noteService.SearchNotesAsync(q, userId);
        ViewBag.Query = q;
        return View(results);
    }

    [HttpPost]
    public async Task<IActionResult> GenerateShareLink(int noteId)
    {
        if (!User.Identity!.IsAuthenticated)
            return RedirectToAction("Login", "Account");

        var token = await _noteService.GenerateShareTokenAsync(noteId);
        TempData["ShareLink"] = Url.Action("Shared", "Notes", new { token }, Request.Scheme);
        return RedirectToAction("Details", new { id = noteId });
    }

    [HttpGet]
    public async Task<IActionResult> Shared(string token)
    {
        var note = await _noteService.GetNoteByShareTokenAsync(token);
        if (note == null)
        {
            TempData["Error"] = "Invalid or expired share link.";
            return RedirectToAction("Index", "Home");
        }
        return View(note);
    }

    [HttpGet]
    public IActionResult Download(string filename)
    {
        var attachmentsDir = _fileService.GetAttachmentsBasePath();
        var fullPath = Path.Combine(attachmentsDir, filename);

        _logger.LogInformation("File download request: {FilePath}", fullPath);

        if (!System.IO.File.Exists(fullPath))
        {
            ViewBag.Filename = filename;
            return View("DownloadError");
        }

        var data = System.IO.File.ReadAllBytes(fullPath);
        return File(data, "application/octet-stream", filename);
    }

    [HttpGet]
    public async Task<IActionResult> Export()
    {
        if (!User.Identity!.IsAuthenticated)
            return RedirectToAction("Login", "Account");

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var notes = await _noteService.GetUserNotesAsync(userId);
        return View(notes);
    }

    [HttpPost]
    public async Task<IActionResult> Export(List<int> selectedNoteIds)
    {
        if (!User.Identity!.IsAuthenticated)
            return RedirectToAction("Login", "Account");

        if (selectedNoteIds == null || !selectedNoteIds.Any())
        {
            TempData["Error"] = "Please select at least one note to export.";
            return RedirectToAction("Export");
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var allNotes = await _noteService.GetUserNotesAsync(userId);
        var selectedIds = selectedNoteIds.ToHashSet();

        var notesToExport = new List<Note>();
        foreach (var note in allNotes.Where(n => selectedIds.Contains(n.Id)))
        {
            var full = await _noteService.GetNoteByIdAsync(note.Id);
            if (full != null) notesToExport.Add(full);
        }

        var attachmentsBaseDir = _fileService.GetAttachmentsBasePath();

        using var memStream = new MemoryStream();
        using (var archive = new ZipArchive(memStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var manifest = new
            {
                exportedAt = DateTime.UtcNow,
                notes = notesToExport.Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    content = n.Content,
                    isPublic = n.IsPublic,
                    createdAt = n.CreatedAt,
                    attachments = n.Attachments.Select(a => new
                    {
                        filename = a.FileName,
                        originalName = a.FileName,
                        contentType = a.ContentType
                    }).ToList()
                }).ToList()
            };

            var jsonEntry = archive.CreateEntry("notes.json", CompressionLevel.Optimal);
            await using (var sw = new StreamWriter(jsonEntry.Open()))
                await sw.WriteAsync(JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

            foreach (var note in notesToExport)
            {
                foreach (var attachment in note.Attachments)
                {
                    var filePath = Path.Combine(attachmentsBaseDir, attachment.FileName);
                    if (!System.IO.File.Exists(filePath)) continue;
                    var entryName = $"attachments/{attachment.FileName}";
                    var fileEntry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                    await using var entryStream = fileEntry.Open();
                    await using var fileStream = System.IO.File.OpenRead(filePath);
                    await fileStream.CopyToAsync(entryStream);
                }
            }
        }

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        return File(memStream.ToArray(), "application/zip", $"export_{timestamp}.zip");
    }

    [HttpGet]
    public IActionResult Import()
    {
        if (!User.Identity!.IsAuthenticated)
            return RedirectToAction("Login", "Account");

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Import(IFormFile zipFile)
    {
        if (!User.Identity!.IsAuthenticated)
            return RedirectToAction("Login", "Account");

        if (zipFile == null || zipFile.Length == 0)
        {
            TempData["Error"] = "Please select a ZIP file to import.";
            return View();
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        int importedCount = 0;

        await using var zipStream = zipFile.OpenReadStream();
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        var jsonEntry = archive.GetEntry("notes.json");
        if (jsonEntry == null)
        {
            TempData["Error"] = "Invalid archive: notes.json not found.";
            return View();
        }

        string json;
        using (var reader = new StreamReader(jsonEntry.Open()))
            json = await reader.ReadToEndAsync();

        using var doc = JsonDocument.Parse(json);
        var notesArray = doc.RootElement.GetProperty("notes");

        foreach (var noteEl in notesArray.EnumerateArray())
        {
            var title    = noteEl.GetProperty("title").GetString()   ?? "(untitled)";
            var content  = noteEl.GetProperty("content").GetString() ?? "";
            var isPublic = noteEl.GetProperty("isPublic").GetBoolean();

            var createdNote = await _noteService.CreateNoteAsync(userId, title, content, isPublic);

            if (!noteEl.TryGetProperty("attachments", out var attachmentsEl)) { importedCount++; continue; }

            foreach (var attachEl in attachmentsEl.EnumerateArray())
            {
                var fileName    = attachEl.GetProperty("filename").GetString() ?? "attachment";
                var contentType = attachEl.TryGetProperty("contentType", out var ct) ? ct.GetString() ?? "application/octet-stream" : "application/octet-stream";

                var archiveEntry = archive.GetEntry($"attachments/{fileName}");
                if (archiveEntry == null) continue;

                var savePath = Path.Combine(_env.WebRootPath, archiveEntry.FullName);

                var saveDir = Path.GetDirectoryName(savePath);
                if (saveDir != null && !Directory.Exists(saveDir))
                    Directory.CreateDirectory(saveDir);

                await using (var fs = new FileStream(savePath, FileMode.Create))
                await using (var es = archiveEntry.Open())
                    await es.CopyToAsync(fs);

                _context.Attachments.Add(new Attachment
                {
                    NoteId      = createdNote.Id,
                    FileName    = fileName,
                    StoredPath  = savePath,
                    ContentType = contentType,
                    Size        = archiveEntry.Length,
                    UploadedAt  = DateTime.UtcNow
                });
            }

            importedCount++;
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Successfully imported {importedCount} note(s).";
        return RedirectToAction("Index");
    }
}
