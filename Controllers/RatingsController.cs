using LooseNotes.Models.ViewModels;
using LooseNotes.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LooseNotes.Controllers;

public class RatingsController : Controller
{
    private readonly NoteService _noteService;

    public RatingsController(NoteService noteService)
    {
        _noteService = noteService;
    }

    [HttpPost]
    public async Task<IActionResult> Rate(RatingViewModel model)
    {
        if (!User.Identity!.IsAuthenticated)
            return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Invalid rating data.";
            return RedirectToAction("Details", "Notes", new { id = model.NoteId });
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userEmail = User.FindFirstValue("Email") ?? string.Empty;
        await _noteService.AddRatingAsync(model.NoteId, userId, userEmail, model.Stars, model.Comment);

        TempData["Success"] = "Rating submitted successfully.";
        return RedirectToAction("Details", "Notes", new { id = model.NoteId });
    }
}
