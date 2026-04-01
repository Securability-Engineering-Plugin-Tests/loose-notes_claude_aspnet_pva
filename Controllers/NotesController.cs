using LooseNotes.Models.ViewModels;
using LooseNotes.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LooseNotes.Controllers;

public class NotesController : Controller
{
    private readonly NoteService _noteService;
    private readonly FileService _fileService;
    private readonly ILogger<NotesController> _logger;

    public NotesController(NoteService noteService, FileService fileService, ILogger<NotesController> logger)
    {
        _noteService = noteService;
        _fileService = fileService;
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

    [HttpPost, ActionName("Delete")]
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
    public IActionResult Download(string filePath, string fileName)
    {
        var (data, contentType, name) = _fileService.DownloadFile(filePath, fileName);
        return File(data, contentType, name);
    }
}
