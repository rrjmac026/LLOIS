namespace LLOIS.Services;

using LLOIS.Models;

public interface IResolutionService
{
    IEnumerable<Resolution> Search(string query);
    Resolution? GetDetails(int id);
    void Add(Resolution resolution);
    void Update(Resolution resolution);
    void Delete(int id);
}