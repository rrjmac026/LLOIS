namespace LLOIS.Data;

using BCrypt.Net;
using LLOIS.Models;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {

        if (db.Users.Any()) return;

        db.Users.AddRange(
            new User { Username = "superadmin", PasswordHash = BCrypt.HashPassword("superadmin123"), Role = UserRole.SuperAdmin },
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
                    Title = "Single-Use Plastics Regulation Act",
                    Subject = "An Ordinance Regulating the Use of Single-Use Plastics",
                    Type = TypeOfLaw.Ordinance,
                    Status = OrdinanceStatus.Amended,
                    Sponsor = "Hon. Juan dela Cruz",
                    Committee = "Committee on Environment",
                    DatePassed = new DateOnly(2020, 3, 10),
                    DateApproved = new DateOnly(2020, 3, 15),
                    DatePublished = new DateOnly(2020, 3, 20),
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
                    Title = "Local Youth Development Council Act",
                    Subject = "An Ordinance Establishing the Local Youth Development Council",
                    Type = TypeOfLaw.Ordinance,
                    Status = OrdinanceStatus.InEffect,
                    Sponsor = "Hon. Maria Santos",
                    Committee = "Committee on Youth Affairs",
                    DatePassed = new DateOnly(2019, 5, 28),
                    DateApproved = new DateOnly(2019, 6, 1),
                    DatePublished = new DateOnly(2019, 6, 5),
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
                },
                new Ordinance
                {
                    OrdinanceNumber = "ORD-2015-010",
                    SeriesNumber = "Series of 2015, No. 10",
                    Title = "Market Vendor Fee Collection Ordinance",
                    Subject = "An Ordinance on Market Vendor Fee Collection",
                    Type = TypeOfLaw.Ordinance,
                    Status = OrdinanceStatus.Repealed,
                    Sponsor = "Hon. Pedro Reyes",
                    Committee = "Committee on Trade",
                    DatePassed = new DateOnly(2015, 1, 15),
                    DateApproved = new DateOnly(2015, 1, 20),
                    DatePublished = new DateOnly(2015, 1, 25),
                    Versions =
                    [
                        new OrdinanceVersion
                        {
                            VersionNumber = 1,
                            Title = "Market Vendor Fee Collection Ordinance",
                            Content = "Original fee schedule for public market vendors.",
                            DateEnacted = new DateOnly(2015, 1, 20),
                            EnactedBy = "Ordinance No. 2015-010",
                            AmendmentNotes = "Repealed and superseded by ORD-2023-002."
                        }
                    ]
                }
            );
        }

        db.SaveChanges();
    }
}