namespace LLOIS.Services;

using LLOIS.Models;
using LLOIS.Repositories;

public class MinutesService(IMinutesRepository repo) : IMinutesService
{
    public IEnumerable<Minutes> GetAll() => repo.GetAll();

    public Minutes? GetDetails(int id) => repo.GetByPrimaryId(id);

    public void Add(Minutes minutes) => repo.Add(minutes);

    public void Update(Minutes minutes) => repo.Update(minutes);

    public void Delete(int id)
    {
        var minutes = repo.GetByPrimaryId(id)
            ?? throw new InvalidOperationException("Minutes record not found.");
        repo.Delete(minutes);
    }
}