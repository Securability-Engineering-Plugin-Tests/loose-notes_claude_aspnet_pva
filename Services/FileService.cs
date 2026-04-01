using LooseNotes.Data;
using LooseNotes.Models;

namespace LooseNotes.Services;

public class FileService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileService> _logger;
    private readonly string _uploadsPath;

    public FileService(ApplicationDbContext context, IConfiguration configuration, ILogger<FileService> logger, IWebHostEnvironment env)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _uploadsPath = Path.Combine(env.ContentRootPath, configuration["AppSettings:UploadsPath"] ?? "uploads");

        if (!Directory.Exists(_uploadsPath))
            Directory.CreateDirectory(_uploadsPath);
    }

    public async Task<Attachment> SaveAttachmentAsync(int noteId, IFormFile file)
    {
        var fileName = file.FileName;
        var storedPath = Path.Combine(_uploadsPath, fileName);

        _logger.LogInformation("Saving attachment: {FileName}, ContentType: {ContentType}, Size: {Size}", fileName, file.ContentType, file.Length);

        using (var stream = new FileStream(storedPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var attachment = new Attachment
        {
            NoteId = noteId,
            FileName = fileName,
            StoredPath = storedPath,
            ContentType = file.ContentType,
            Size = file.Length,
            UploadedAt = DateTime.UtcNow
        };

        _context.Attachments.Add(attachment);
        await _context.SaveChangesAsync();
        return attachment;
    }

    public (byte[] data, string contentType, string fileName) DownloadFile(string filePath, string fileName)
    {
        var fullPath = Path.Combine(_uploadsPath, filePath);

        _logger.LogInformation("File download request: {FilePath}", fullPath);

        var data = File.ReadAllBytes(fullPath);
        return (data, "application/octet-stream", fileName);
    }

    public async Task DeleteAttachmentAsync(int attachmentId)
    {
        var attachment = await _context.Attachments.FindAsync(attachmentId);
        if (attachment == null) return;

        if (File.Exists(attachment.StoredPath))
            File.Delete(attachment.StoredPath);

        _context.Attachments.Remove(attachment);
        await _context.SaveChangesAsync();
    }
}
