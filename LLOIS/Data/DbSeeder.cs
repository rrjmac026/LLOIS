using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS.Data;

using BCrypt.Net;
using LLOIS.Models;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        db.Database.EnsureCreated();

        if (db.Users.Any()) return;

        db.Users.AddRange(
            new User { Username = "admin", PasswordHash = BCrypt.HashPassword("admin123"), Role = UserRole.Admin },
            new User { Username = "encoder1", PasswordHash = BCrypt.HashPassword("encoder123"), Role = UserRole.Encoder },
            new User { Username = "viewer1", PasswordHash = BCrypt.HashPassword("viewer123"), Role = UserRole.Viewer }
        );

        if (!db.Ordinances.Any())
        {
            db.Ordinances.AddRange(
                new Ordinance
                {
                    OrdinanceNumber = "ORD-2020-001",
                    SeriesNumber = "Series of 2020, No. 1",
                    Subject = "An Ordinance Regulating the Use of Single-Use Plastics",
                    Status = OrdinanceStatus.Amended,
                    Versions =
                    [
                        new OrdinanceVersion
                        {
                            VersionNumber = 1,
                            Title = "Single-Use Plastics Regulation Act",
                            Content = "Original ordinance banning single-use plastics in commercial establishments.",
                            DateEnacted = new DateOnly(2020, 3, 15),
                            EnactedBy = "Ordinance No. 2020-001"
                        },
                        new OrdinanceVersion
                        {
                            VersionNumber = 2,
                            Title = "Single-Use Plastics Regulation Act (Amended)",
                            Content = "Amendment expanding the ban to include all public spaces and events.",
                            DateEnacted = new DateOnly(2022, 7, 10),
                            EnactedBy = "Ordinance No. 2022-014",
                            AmendmentNotes = "Expanded coverage; added penalty provisions."
                        }
                    ]
                },
                new Ordinance
                {
                    OrdinanceNumber = "ORD-2019-004",
                    SeriesNumber = "Series of 2019, No. 4",
                    Subject = "An Ordinance Establishing the Local Youth Development Council",
                    Status = OrdinanceStatus.InEffect,
                    Versions =
                    [
                        new OrdinanceVersion
                        {
                            VersionNumber = 1,
                            Title = "Local Youth Development Council Act",
                            Content = "Establishes the LYDC and defines its composition, powers, and functions.",
                            DateEnacted = new DateOnly(2019, 6, 1),
                            EnactedBy = "Ordinance No. 2019-004"
                        }
                    ]
                }
            );
        }

        db.SaveChanges();
    }
}
