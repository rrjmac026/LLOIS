namespace LLOIS.Data;

using Microsoft.EntityFrameworkCore;
using DotNetEnv;

public class SimpleDbContextFactory : IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext()
    {
        Env.Load();

        var connectionString =
            $"Host={Environment.GetEnvironmentVariable("SUPABASE_HOST")};" +
            $"Port={Environment.GetEnvironmentVariable("SUPABASE_PORT")};" +
            $"Database={Environment.GetEnvironmentVariable("SUPABASE_DB")};" +
            $"Username={Environment.GetEnvironmentVariable("SUPABASE_USER")};" +
            $"Password={Environment.GetEnvironmentVariable("SUPABASE_PASSWORD")};" +
            "SSL Mode=Require;Trust Server Certificate=true";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}