namespace LLOIS.Repositories;

using Microsoft.EntityFrameworkCore;
using LLOIS.Data;
using LLOIS.Models;

public class UserRepository(IDbContextFactory<AppDbContext> dbFactory) : IUserRepository
{
    public User? GetByUsername(string username)
    {
        using var db = dbFactory.CreateDbContext();
        return db.Users.FirstOrDefault(u => u.Username == username && u.IsActive);
    }

    public IEnumerable<User> GetAll()
    {
        using var db = dbFactory.CreateDbContext();
        return db.Users.OrderBy(u => u.Username).ToList();
    }

    public void Add(User user)
    {
        using var db = dbFactory.CreateDbContext();
        db.Users.Add(user);
        db.SaveChanges();
    }

    public void Update(User user)
    {
        using var db = dbFactory.CreateDbContext();
        db.Users.Update(user);
        db.SaveChanges();
    }
}