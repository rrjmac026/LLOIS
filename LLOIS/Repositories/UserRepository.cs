namespace LLOIS.Repositories;

using LLOIS.Data;
using LLOIS.Models;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public User? GetByUsername(string username) =>
        db.Users.FirstOrDefault(u => u.Username == username && u.IsActive);

    public IEnumerable<User> GetAll() =>
        db.Users.OrderBy(u => u.Username).ToList();

    public void Add(User user)
    {
        db.Users.Add(user);
        db.SaveChanges();
    }

    public void Update(User user)
    {
        db.Users.Update(user);
        db.SaveChanges();
    }
}
