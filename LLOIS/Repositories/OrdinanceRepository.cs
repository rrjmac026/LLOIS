namespace LLOIS.Repositories;

using Microsoft.EntityFrameworkCore;
using LLOIS.Data;
using LLOIS.Models;

public class OrdinanceRepository(AppDbContext db) : IOrdinanceRepository
{
    public IEnumerable<Ordinance> GetAll() =>
        db.Ordinances.Include(o => o.Versions).ToList();

    public Ordinance? GetById(string ordinanceNumber) =>
        db.Ordinances.Include(o => o.Versions)
            .FirstOrDefault(o => o.OrdinanceNumber == ordinanceNumber);

    public IEnumerable<Ordinance> Search(string query) =>
        db.Ordinances.Include(o => o.Versions)
            .Where(o => o.OrdinanceNumber.Contains(query) ||
                        o.Subject.Contains(query) ||
                        o.SeriesNumber.Contains(query) ||
                        o.Title.Contains(query) ||
                        o.Sponsor.Contains(query) ||
                        o.Committee.Contains(query))
            .ToList();

    public void Add(Ordinance ordinance)
    {
        db.Ordinances.Add(ordinance);
        db.SaveChanges();
    }

    public void Update(Ordinance ordinance)
    {
        db.Ordinances.Update(ordinance);
        db.SaveChanges();
    }

    public void Delete(Ordinance ordinance)
    {
        db.Ordinances.Remove(ordinance);
        db.SaveChanges();
    }
}
