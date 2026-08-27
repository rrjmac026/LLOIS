namespace LLOIS.Repositories;

using Microsoft.EntityFrameworkCore;
using LLOIS.Data;
using LLOIS.Models;

public class ResolutionRepository(IDbContextFactory<AppDbContext> dbFactory) : IResolutionRepository
{
    public IEnumerable<Resolution> GetAll()
    {
        using var db = dbFactory.CreateDbContext();
        return db.Resolutions.Include(r => r.Clauses).ToList();
    }

    public Resolution? GetByPrimaryId(int id)
    {
        using var db = dbFactory.CreateDbContext();
        return db.Resolutions.Include(r => r.Clauses)
            .FirstOrDefault(r => r.Id == id);
    }

    public IEnumerable<Resolution> Search(string query)
    {
        using var db = dbFactory.CreateDbContext();
        if (string.IsNullOrWhiteSpace(query))
            return db.Resolutions.Include(r => r.Clauses).ToList();

        var keywords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return db.Resolutions
            .Include(r => r.Clauses)
            .ToList()
            .Where(r => keywords.All(k =>
                (r.Title?.Contains(k, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.ResolutionNumber?.Contains(k, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Sponsor?.Contains(k, StringComparison.OrdinalIgnoreCase) ?? false)
            ));
    }

    public void Add(Resolution resolution)
    {
        using var db = dbFactory.CreateDbContext();
        db.Resolutions.Add(resolution);
        db.SaveChanges();
    }

    public void Update(Resolution resolution)
    {
        using var db = dbFactory.CreateDbContext();
        db.Resolutions.Update(resolution);
        db.SaveChanges();
    }

    public void Delete(Resolution resolution)
    {
        using var db = dbFactory.CreateDbContext();
        db.Resolutions.Remove(resolution);
        db.SaveChanges();
    }
}