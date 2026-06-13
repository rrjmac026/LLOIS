using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS.Repositories;

using LLOIS.Models;

public static class SeedData
{
    public static List<Ordinance> Get() =>
    [
        new Ordinance
        {
            Id = "ORD-2020-001",
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
            Id = "ORD-2019-004",
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
        },
        new Ordinance
        {
            Id = "ORD-2015-010",
            SeriesNumber = "Series of 2015, No. 10",
            Subject = "An Ordinance on Market Vendor Fee Collection",
            Status = OrdinanceStatus.Repealed,
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
    ];
}
