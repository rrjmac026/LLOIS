namespace LLOIS.Repositories;

using LLOIS.Models;

public interface IUserRepository
{
    User? GetByUsername(string username);
    IEnumerable<User> GetAll();
    void Add(User user);
    void Update(User user);
}
