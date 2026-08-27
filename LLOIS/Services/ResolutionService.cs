namespace LLOIS.Services;

using LLOIS.Models;
using LLOIS.Repositories;

public class ResolutionService(IResolutionRepository repo) : IResolutionService
{
    public IEnumerable<Resolution> Search(string query) => repo.Search(query);

    public Resolution? GetDetails(int id) => repo.GetByPrimaryId(id);

    public void Add(Resolution resolution) => repo.Add(resolution);

    public void Update(Resolution resolution) => repo.Update(resolution);

    public void Delete(int id)
    {
        var resolution = repo.GetByPrimaryId(id)
            ?? throw new InvalidOperationException("Resolution not found.");
        repo.Delete(resolution);
    }
}