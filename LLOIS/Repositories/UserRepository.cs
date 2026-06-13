using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS.Repositories;

using LLOIS.Data;
using LLOIS.Models;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public User? GetByUsername(string username) =>
        db.Users.FirstOrDefault(u => u.Username == username && u.IsActive);
}