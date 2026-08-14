namespace LLOIS.Repositories;

using Microsoft.EntityFrameworkCore;
using LLOIS.Data;
using LLOIS.Models;

public class MinutesRepository(IDbContextFactory<AppDbContext> dbFactory) : IMinutesRepository
{
    public IEnumerable<Minutes> GetAll()
    {
        using var db = dbFactory.CreateDbContext();
        return db.Minutes.OrderByDescending(m => m.Date).ToList();
    }

    public Minutes? GetByPrimaryId(int id)
    {
        using var db = dbFactory.CreateDbContext();
        return db.Minutes.FirstOrDefault(m => m.Id == id);
    }

    public void Add(Minutes minutes)
    {
        using var db = dbFactory.CreateDbContext();
        db.Minutes.Add(minutes);
        db.SaveChanges();
    }

    public void Update(Minutes minutes)
    {
        using var db = dbFactory.CreateDbContext();
        db.Minutes.Update(minutes);
        db.SaveChanges();
    }

    public void Delete(Minutes minutes)
    {
        using var db = dbFactory.CreateDbContext();
        db.Minutes.Remove(minutes);
        db.SaveChanges();
    }
}