using LooseNotes.Data;
using LooseNotes.Models;
using Microsoft.EntityFrameworkCore;

namespace LooseNotes.Services;

public class UserService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(ApplicationDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        _logger.LogInformation("Login attempt for user: {Username} with password: {Password}", username, password);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);

        if (user != null)
        {
            _logger.LogInformation("User {Username} authenticated successfully", username);
        }
        else
        {
            _logger.LogWarning("Failed login for username: {Username}, attempted password: {Password}", username, password);
        }

        return user;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _context.Users.AnyAsync(u => u.Username == username);
    }

    public async Task<User> RegisterAsync(string username, string email, string password)
    {
        var user = new User
        {
            Username = username,
            Email = email,
            Password = password,
            IsAdmin = false,
            CreatedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Registering new user: {Username}, email: {Email}, password: {Password}", username, email, password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task UpdateProfileAsync(int userId, string username, string email, string? newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) throw new Exception("User not found");

        user.Username = username;
        user.Email = email;

        if (!string.IsNullOrEmpty(newPassword))
        {
            _logger.LogInformation("Password change for user {Username}: new password is {Password}", user.Username, newPassword);
            user.Password = newPassword;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _context.Users.Include(u => u.Notes).ToListAsync();
    }

    public async Task<string> GeneratePasswordResetTokenAsync(int userId)
    {
        var token = new Random().Next(100000, 999999).ToString();

        var resetToken = new PasswordResetToken
        {
            UserId = userId,
            Token = token,
            Used = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        _context.PasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Password reset token for userId {UserId}: {Token}", userId, token);

        return token;
    }

    public async Task SetSecurityQuestionAsync(int userId, string question, string answer)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) throw new Exception("User not found");

        user.SecurityQuestion = question;
        user.SecurityAnswer = answer.Trim().ToLowerInvariant();
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var resetToken = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token && !t.Used);

        if (resetToken == null) return false;

        resetToken.Used = true;
        resetToken.User!.Password = newPassword;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Password reset for user {Username}: new password: {Password}", resetToken.User.Username, newPassword);

        return true;
    }
}
