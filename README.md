# LLOIS — Local Legislation Ordinance Information System

A desktop application for document management and legal tracking of ordinances passed by a Sanggunian (city/municipal legislative council). LLOIS centralizes the encoding, tracking, searching, and reporting of local ordinances — including their full amendment history, legal status, and inter-ordinance relationships.

---

## Table of Contents

- [Overview](#overview)
- [User Roles](#user-roles)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [System Requirements](#system-requirements)
- [Installation](#installation)
- [Environment Configuration](#environment-configuration)
- [Database Setup](#database-setup)
- [Running the Application](#running-the-application)
- [Background Services](#background-services)
- [Ordinance Lifecycle](#ordinance-lifecycle)
- [Ordinance Statuses](#ordinance-statuses)
- [Ordinance Types](#ordinance-types)
- [Relationships Between Ordinances](#relationships-between-ordinances)
- [Roadmap](#roadmap)
- [License](#license)

---

## Overview

LLOIS is designed to serve local government units (LGUs) in the Philippines that need a structured, searchable, and legally traceable repository of enacted ordinances. It addresses the common problem of ordinances being stored as scattered physical documents or unorganized digital files — with no easy way to track which laws have been amended, superseded, or repealed.

The system is built with **C#** and **WPF** (.NET) with a **PostgreSQL** database backend, designed for deployment in government or infrastructure-sector environments.

---

## User Roles

| Role | Description |
|---|---|
| **Admin** | Full system access. Manages users, system data, and configurations. |
| **Legislative Staff (Encoder)** | Encodes and manages ordinance records, uploads documents, and maintains metadata. |
| **Legal Officer** | Tracks legal status, flags conflicts, manages amendment and repeal relationships. |
| **Public / Researcher (Viewer)** | Read-only access to search and view published ordinances. |

---

## Features

### Ordinance Management
- Add, edit, and archive ordinances
- Record complete ordinance metadata:
  - Ordinance number and series
  - Title and subject matter
  - Sponsor / Author (Councilor)
  - Date passed by the Sanggunian
  - Date approved by the Mayor
  - Date published / effectivity date
- Attach the original signed ordinance as a PDF document
- Classify ordinances by type (see [Ordinance Types](#ordinance-types))

### Version & Amendment Tracking
- Link amendments directly back to the original ordinance
- Display full amendment history in chronological order
- Record what specifically changed per amendment
- Handle **superseding** — one ordinance fully replacing another
- Handle **repeal** — with reason, date, and reference to the repealing ordinance

### Status Management
- Statuses: `In Effect`, `Amended`, `Superseded`, `Repealed`, `Under Review`
- Automatic status suggestions based on inter-ordinance relationships
  - e.g. if Ordinance B repeals Ordinance A, the system flags Ordinance A's status

### Search & Filter
- Full-text keyword search across title, subject, and body
- Search by ordinance number or series
- Filter by:
  - Status (In Effect, Repealed, etc.)
  - Type (Regulatory, Revenue, etc.)
  - Date range (date passed, date approved)
  - Committee
  - Sponsor / Author
- Sort by date, series number, or relevance

### Relationships Between Ordinances
- Explicitly define relationships:
  - `This ordinance amends ORD-2020-001`
  - `This ordinance repeals ORD-2015-010`
  - `This ordinance is related to ORD-2019-004`
- Visual lifecycle chain showing the full history of a law from enactment through amendments and eventual repeal or supersession

### Reports
- List of all ordinances by year / series
- List of all repealed ordinances
- List of all amended ordinances and their current in-effect version
- Print-ready ordinance detail sheet (per ordinance)

### Audit Log *(planned)*
- Tracks who added or edited each record and when
- Viewable by Admin

### Dashboard *(planned)*
- Summary counts by status
- Recent activity feed
- Quick links to pending or under-review ordinances

---

## Tech Stack

| Layer | Technology |
|---|---|
| Desktop Framework | WPF (.NET 10.0 Windows) |
| Backend Language | C# |
| Database | PostgreSQL |
| ORM | Entity Framework Core |
| File Storage | Local disk / cloud storage |
| PDF Handling | iTextSharp / PdfSharp |
| Architecture | MVVM with Repository & Service patterns |

---

## System Requirements

- Windowss 10 or later
- .NET 10.0 Runtime (for users) or .NET SDK 10.0 (for developers)
- PostgreSQL 12+ (for database)
- Visual Studio 2022 or Visual Studio Code with C# extension (for development)

---

## Installation

### 1. Clone the Repository

```bash
git clone https://github.com/your-org/llois.git
cd llois
```

### 2. Install .NET Dependencies

Open the solution file `LLOIS.slnx` in Visual Studio 2022 or restore dependencies via CLI:

```bash
dotnet restore
```

### 3. Set Up Database

Ensure PostgreSQL is running and configure the connection string in the application settings.

---

## Environment Configuration

Edit `appsettings.json` (or `appsettings.Development.json` for local development) to configure:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=llois;Username=postgres;Password=your_password"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "StoragePath": "./storage",
  "DatabaseProvider": "PostgreSQL"
}
```

> **Note:** Update the connection string to match your PostgreSQL server configuration.

---

## Database Setup

Apply Entity Framework Core migrations to initialize the database:

```bash
dotnet ef database update
```

To seed sample data:

```bash
dotnet ef database update --context AppDbContext
```

---

## Running the Application

### From Source

```bash
dotnet run
```

The WPF desktop application will launch.

### From Published Build

Publish for standalone deployment:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

Then run `LLOIS.exe` from the `publish` directory.

---

## Background Services

LLOIS uses background services for async operations such as updates and exports. These are handled by the application's internal task scheduler and do not require external queue workers.

To monitor logs and application state, use the Dashboard view within the LLOIS application.

---



## Ordinance Lifecycle

Below is the typical lifecycle of an ordinance in LLOIS:

```
Drafted / Proposed
       ↓
  Passed by Sanggunian  ←→ [Recorded in LLOIS with full metadata]
       ↓
  Approved by Mayor     ←→ [Date approved logged]
       ↓
  Published / In Effect ←→ [Status: In Effect]
       ↓
  ┌────────────────────────────────────────┐
  │  Possible future events:              │
  │                                        │
  │  → Amended         [Status: Amended]  │
  │  → Superseded      [Status: Superseded]│
  │  → Repealed        [Status: Repealed] │
  └────────────────────────────────────────┘
```

Each transition is recorded with a reference to the related ordinance (the one that caused the amendment, supersession, or repeal).

---

## Ordinance Statuses

| Status | Description |
|---|---|
| **In Effect** | Currently active and enforceable |
| **Amended** | Modified by a subsequent ordinance; original still partially in effect |
| **Superseded** | Entirely replaced by a newer ordinance |
| **Repealed** | Abolished; no longer in effect |
| **Under Review** | Flagged for legal review or currently being deliberated |

---

## Ordinance Types

| Type | Description |
|---|---|
| **Regulatory** | Governs conduct, prescribes rules and standards |
| **Revenue** | Taxation, fees, charges, and financial matters |
| **Administrative** | Internal government operations and procedures |
| **Penal** | Defines prohibited acts and corresponding penalties |
| **Appropriations** | Budget and allocation of government funds |
| **General** | Miscellaneous ordinances not fitting other categories |

---

## Relationships Between Ordinances

LLOIS supports the following formal relationships between ordinance records:

| Relationship Type | Description |
|---|---|
| `amends` | The current ordinance modifies specific provisions of another |
| `repeals` | The current ordinance abolishes another ordinance entirely |
| `supersedes` | The current ordinance replaces another in full |
| `related_to` | A loose relationship for cross-referencing without legal effect |

Each relationship is bidirectional in display — viewing either ordinance will show the connection and link to the other.

---

## Roadmap

### Phase 1 — Core (Current)
- [x] Search & view ordinances
- [x] Add / Edit ordinance form
- [x] PDF attachment support
- [x] Ordinance status management
- [x] Ordinance relationship linking
- [x] User authentication and role-based access
- [x] Audit logging

### Phase 2 — Reporting & Print
- [x] Reports by year, status, type
- [x] Print-ready ordinance detail sheet
- [ ] Export to Excel / CSV

### Phase 3 — Enhanced Features
- [ ] Bulk import via Excel/CSV (for historical data migration)
- [ ] Advanced search filters and saved searches
- [ ] Email notifications for status changes
- [ ] Automated backup and restore functions

### Phase 4 — Nice to Have
- [ ] Multi-language support
- [ ] Customizable report templates
- [ ] Integration with government document management systems

---

## License

This system is developed for use by Local Government Units (LGUs) in the Philippines. All rights reserved by the developing organization. Unauthorized distribution or commercial use is prohibited.

---

*Built with C# · WPF · .NET · PostgreSQL · Entity Framework Core*