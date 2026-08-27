namespace LLOIS.Repositories;

using LLOIS.Models;

public interface IResolutionRepository
{
    IEnumerable<Resolution> GetAll();
    Resolution? GetByPrimaryId(int id);
    IEnumerable<Resolution> Search(string query);
    void Add(Resolution resolution);
    void Update(Resolution resolution);
    void Delete(Resolution resolution);
}