using System;
using System.Collections.Generic;
using System.Text;

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
}