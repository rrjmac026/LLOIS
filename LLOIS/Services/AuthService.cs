namespace LLOIS.Services;

using BCrypt.Net;
using LLOIS.Data;
using LLOIS.Models;
using LLOIS.Repositories;

public class AuthService(IUserRepository userRepo, AppDbContext db) : IAuthService
{
    public User? Login(string username, string password)
    {
        var user = userRepo.GetByUsername(username);
        if (user is null) return null;
        return BCrypt.Verify(password, user.PasswordHash) ? user : null;
    }

    public void LogAction(User user, string action, string details)
    {
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
        var user = db.Users.Find(userId)
            ?? throw new InvalidOperationException("User not found.");
        user.PasswordHash = BCrypt.HashPassword(newPassword);
        userRepo.Update(user);
    }

    public void SetActiveStatus(int userId, bool isActive)
    {
        var user = db.Users.Find(userId)
            ?? throw new InvalidOperationException("User not found.");
        user.IsActive = isActive;
        userRepo.Update(user);
    }

    public IEnumerable<AuditLog> GetRecentLogs(int count = 200) =>
        db.AuditLogs.OrderByDescending(l => l.Timestamp).Take(count).ToList();
}
