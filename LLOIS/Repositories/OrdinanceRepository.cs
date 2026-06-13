using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS.Repositories;

using Microsoft.EntityFrameworkCore;
using LLOIS.Data;
using LLOIS.Models;

public class OrdinanceRepository(AppDbContext db) : IOrdinanceRepository
{
    private void LoadOrdinances()
    {
        try
        {
            var results = _service.Search(_searchQuery).ToList();

            if (StatusFilter.SelectedItem is ComboBoxItem { Content: string status } && status != "All"
                && Enum.TryParse<OrdinanceStatus>(status.Replace(" ", ""), out var parsed))
            {
                results = results.Where(o => o.Status == parsed).ToList();
            }

            OrdinanceList.ItemsSource = results;
            ResultCount.Text = $"{results.Count} ordinance(s) found";
            DetailPanel.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}\n\nInner: {ex.InnerException?.Message}\n\nStack: {ex.StackTrace}", 
                "Debug Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    public IEnumerable<Ordinance> GetAll() =>
        db.Ordinances.Include(o => o.Versions).ToList();

    public Ordinance? GetById(string ordinanceNumber) =>
        db.Ordinances.Include(o => o.Versions)
            .FirstOrDefault(o => o.OrdinanceNumber == ordinanceNumber);

    public IEnumerable<Ordinance> Search(string query) =>
        db.Ordinances.Include(o => o.Versions)
            .Where(o => o.OrdinanceNumber.Contains(query) ||
                        o.Subject.Contains(query) ||
                        o.SeriesNumber.Contains(query))
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

    
}
