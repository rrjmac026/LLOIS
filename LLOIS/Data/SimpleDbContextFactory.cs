using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS.Data;

using Microsoft.EntityFrameworkCore;

public class SimpleDbContextFactory : IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=llois.db")
            .Options;

        return new AppDbContext(options);
    }
}
