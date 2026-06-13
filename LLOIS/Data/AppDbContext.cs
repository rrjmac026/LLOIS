using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS.Data;

using Microsoft.EntityFrameworkCore;
using LLOIS.Models;

public class AppDbContext : DbContext
{
    public DbSet<Ordinance> Ordinances => Set<Ordinance>();
    public DbSet<OrdinanceVersion> OrdinanceVersions => Set<OrdinanceVersion>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnConfiguring(DbContextOptionsBuilder options) =>
        options.UseSqlite("Data Source=llois.db");
}
