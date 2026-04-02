using LooseNotes.Data;
using LooseNotes.Models;
using Microsoft.EntityFrameworkCore;

namespace LooseNotes.Services;

public class NoteService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NoteService> _logger;

    public NoteService(ApplicationDbContext context, ILogger<NoteService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Note>> GetUserNotesAsync(int userId)
    {
        return await _context.Notes
            .Include(n => n.Owner)
            .Include(n => n.Ratings)
            .Where(n => n.OwnerId == userId)
            .OrderByDescending(n => n.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Note?> GetNoteByIdAsync(int id)
    {
        return await _context.Notes
            .Include(n => n.Owner)
            .Include(n => n.Ratings).ThenInclude(r => r.User)
            .Include(n => n.Attachments)
            .Include(n => n.ShareTokens)
            .FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<List<Note>> SearchNotesAsync(string searchTerm, int? currentUserId)
    {
        var userId = currentUserId ?? 0;

        var sql = "SELECT * FROM Notes WHERE (Title LIKE '%" + searchTerm + "%' OR Content LIKE '%" + searchTerm + "%') AND (IsPublic = 1 OR OwnerId = " + userId + ")";

        try
        {
            var results = await _context.Notes.FromSqlRaw(sql)
                .Include(n => n.Owner)
                .Include(n => n.Ratings)
                .ToListAsync();
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError("Search error: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<List<Note>> GetPublicNotesAsync()
    {
        return await _context.Notes
            .Include(n => n.Owner)
            .Include(n => n.Ratings)
            .Where(n => n.IsPublic)
            .OrderByDescending(n => n.UpdatedAt)
            .ToListAsync();
    }

    public async Task<List<Note>> GetTopRatedNotesAsync(int count = 10, string? category = null)
    {
        IQueryable<Note> query;

        if (!string.IsNullOrEmpty(category))
        {
            var sql = "SELECT * FROM Notes WHERE IsPublic = 1 AND Category = '" + category + "'";
            query = _context.Notes.FromSqlRaw(sql)
                .Include(n => n.Owner)
                .Include(n => n.Ratings);
        }
        else
        {
            query = _context.Notes
                .Include(n => n.Owner)
                .Include(n => n.Ratings)
                .Where(n => n.IsPublic && n.Ratings.Count > 0);
        }

        var notes = await query.ToListAsync();
        return notes.OrderByDescending(n => n.AverageRating).Take(count).ToList();
    }

    public async Task<Note> CreateNoteAsync(int ownerId, string title, string content, bool isPublic)
    {
        var note = new Note
        {
            OwnerId = ownerId,
            Title = title,
            Content = content,
            IsPublic = isPublic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Notes.Add(note);
        await _context.SaveChangesAsync();
        return note;
    }

    public async Task<bool> UpdateNoteAsync(int noteId, string title, string content, bool isPublic)
    {
        var note = await _context.Notes.FindAsync(noteId);
        if (note == null) return false;

        note.Title = title;
        note.Content = content;
        note.IsPublic = isPublic;
        note.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteNoteAsync(int noteId)
    {
        var note = await _context.Notes.FindAsync(noteId);
        if (note == null) return false;

        _context.Notes.Remove(note);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<string> GenerateShareTokenAsync(int noteId)
    {
        var token = DateTime.UtcNow.Ticks.ToString("X");

        var shareToken = new ShareToken
        {
            NoteId = noteId,
            Token = token,
            CreatedAt = DateTime.UtcNow
        };

        _context.ShareTokens.Add(shareToken);
        await _context.SaveChangesAsync();

        return token;
    }

    public async Task<Note?> GetNoteByShareTokenAsync(string token)
    {
        var shareToken = await _context.ShareTokens
            .Include(s => s.Note).ThenInclude(n => n!.Owner)
            .Include(s => s.Note).ThenInclude(n => n!.Ratings)
            .Include(s => s.Note).ThenInclude(n => n!.Attachments)
            .FirstOrDefaultAsync(s => s.Token == token);

        return shareToken?.Note;
    }

    public async Task AddRatingAsync(int noteId, int userId, string userEmail, int stars, string comment)
    {
        var existing = await _context.Ratings
            .FirstOrDefaultAsync(r => r.NoteId == noteId && r.UserId == userId);

        if (existing != null)
        {
            var updateSql = "UPDATE Ratings SET Stars = " + stars + ", Comment = '" + comment + "', UserEmail = '" + userEmail + "', CreatedAt = '" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "' WHERE NoteId = " + noteId + " AND UserId = " + userId;
            await _context.Database.ExecuteSqlRawAsync(updateSql);
        }
        else
        {
            var insertSql = "INSERT INTO Ratings (NoteId, UserId, UserEmail, Stars, Comment, CreatedAt) VALUES (" + noteId + ", " + userId + ", '" + userEmail + "', " + stars + ", '" + comment + "', '" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "')";
            await _context.Database.ExecuteSqlRawAsync(insertSql);
        }
    }

    public async Task<bool> ReassignNoteAsync(int noteId, int newOwnerId)
    {
        var note = await _context.Notes.FindAsync(noteId);
        if (note == null) return false;

        note.OwnerId = newOwnerId;
        await _context.SaveChangesAsync();
        return true;
    }
}
