using LooseNotes.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LooseNotes.Controllers;

public class HomeController : Controller
{
    private readonly NoteService _noteService;

    public HomeController(NoteService noteService)
    {
        _noteService = noteService;
    }

    public async Task<IActionResult> Index()
    {
        var notes = await _noteService.GetPublicNotesAsync();
        return View(notes);
    }

    public async Task<IActionResult> TopRated()
    {
        var notes = await _noteService.GetTopRatedNotesAsync(10);
        return View(notes);
    }

    public IActionResult Error()
    {
        return View();
    }
}
