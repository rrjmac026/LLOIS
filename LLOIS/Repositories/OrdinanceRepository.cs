using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS.Repositories;

using LLOIS.Models;

public class OrdinanceRepository : IOrdinanceRepository
{
    private readonly List<Ordinance> _store = SeedData.Get();

    public IEnumerable<Ordinance> GetAll() => _store;

    public Ordinance? GetById(string id) =>
        _store.FirstOrDefault(o => o.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<Ordinance> Search(string query) =>
        _store.Where(o =>
            o.Id.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            o.Subject.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            o.SeriesNumber.Contains(query, StringComparison.OrdinalIgnoreCase));

    public void Add(Ordinance ordinance) => _store.Add(ordinance);

    public void Update(Ordinance ordinance)
    {
        var index = _store.FindIndex(o => o.Id == ordinance.Id);
        if (index >= 0) _store[index] = ordinance;
    }
}
