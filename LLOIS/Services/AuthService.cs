namespace LLOIS.Services;

using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using LLOIS.Data;
using LLOIS.Models;
using LLOIS.Repositories;

public class AuthService(IUserRepository userRepo, IDbContextFactory<AppDbContext> dbFactory) : IAuthService
{
    public User? Login(string username, string password)
    {
        var user = userRepo.GetByUsername(username);
        if (user is null) return null;
        return BCrypt.Verify(password, user.PasswordHash) ? user : null;
    }

    public void LogAction(User user, string action, string details)
    {
        using var db = dbFactory.CreateDbContext();
        db.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            Username = user.Username,
            Action = action,
            Details = details
        });
        db.SaveChanges();
    }

    public IEnumerable<User> GetAllUsers() => userRepo.GetAll();

    public void CreateUser(string username, string password, UserRole role)
    {
        using var db = dbFactory.CreateDbContext();
        if (db.Users.Any(u => u.Username == username))
            throw new InvalidOperationException($"Username '{username}' already exists.");

        userRepo.Add(new User
        {
            Username = username,
            PasswordHash = BCrypt.HashPassword(password),
            Role = role,
            IsActive = true
        });
    }

    public void ResetPassword(int userId, string newPassword)
    {
        using var db = dbFactory.CreateDbContext();
        var user = db.Users.Find(userId)
            ?? throw new InvalidOperationException("User not found.");
        user.PasswordHash = BCrypt.HashPassword(newPassword);
        db.SaveChanges();
    }

    public void SetActiveStatus(int userId, bool isActive)
    {
        using var db = dbFactory.CreateDbContext();
        var user = db.Users.Find(userId)
            ?? throw new InvalidOperationException("User not found.");
        user.IsActive = isActive;
        db.SaveChanges();
    }

    public IEnumerable<AuditLog> GetRecentLogs(int count = 200)
    {
        using var db = dbFactory.CreateDbContext();
        return db.AuditLogs.OrderByDescending(l => l.Timestamp).Take(count).ToList();
    }
}