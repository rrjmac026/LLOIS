namespace LLOIS.Repositories;

using LLOIS.Models;

public interface IMinutesRepository
{
    IEnumerable<Minutes> GetAll();
    Minutes? GetByPrimaryId(int id);
    void Add(Minutes minutes);
    void Update(Minutes minutes);
    void Delete(Minutes minutes);
}