namespace LLOIS.Services;

using LLOIS.Models;

public interface IMinutesService
{
    IEnumerable<Minutes> GetAll();
    Minutes? GetDetails(int id);
    void Add(Minutes minutes);
    void Update(Minutes minutes);
    void Delete(int id);
}