using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS.Repositories;

using LLOIS.Models;

public interface IOrdinanceRepository
{
    IEnumerable<Ordinance> GetAll();
    Ordinance? GetById(string id);
    IEnumerable<Ordinance> Search(string query);
    void Add(Ordinance ordinance);
    void Update(Ordinance ordinance);
}
