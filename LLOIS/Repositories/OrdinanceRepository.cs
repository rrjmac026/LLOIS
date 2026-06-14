namespace LLOIS.Repositories;

using Microsoft.EntityFrameworkCore;
using LLOIS.Data;
using LLOIS.Models;

public class OrdinanceRepository(IDbContextFactory<AppDbContext> dbFactory) : IOrdinanceRepository
{
    public IEnumerable<Ordinance> GetAll()
    {
        using var db = dbFactory.CreateDbContext();
        return db.Ordinances.Include(o => o.Versions).ToList();
    }

    public Ordinance? GetById(string ordinanceNumber)
    {
        using var db = dbFactory.CreateDbContext();
        return db.Ordinances.Include(o => o.Versions)
            .FirstOrDefault(o => o.OrdinanceNumber == ordinanceNumber);
    }

    public IEnumerable<Ordinance> Search(string query)
    {
        using var db = dbFactory.CreateDbContext();
        return db.Ordinances.Include(o => o.Versions)
            .Where(o => o.OrdinanceNumber.Contains(query) ||
                        o.Subject.Contains(query) ||
                        o.SeriesNumber.Contains(query) ||
                        o.Title.Contains(query) ||
                        o.Sponsor.Contains(query) ||
                        o.Committee.Contains(query))
            .ToList();
    }

    public void Add(Ordinance ordinance)
    {
        using var db = dbFactory.CreateDbContext();
        db.Ordinances.Add(ordinance);
        db.SaveChanges();
    }

    public void Update(Ordinance ordinance)
    {
        using var db = dbFactory.CreateDbContext();
        db.Ordinances.Update(ordinance);
        db.SaveChanges();
    }

    public void Delete(Ordinance ordinance)
    {
        using var db = dbFactory.CreateDbContext();
        db.Ordinances.Remove(ordinance);
        db.SaveChanges();
    }
}