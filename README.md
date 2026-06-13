# LLOIS — Local Legislation Ordinance Information System

A web-based document management and legal tracking system for ordinances passed by a Sanggunian (city/municipal legislative council). LLOIS centralizes the encoding, tracking, searching, and reporting of local ordinances — including their full amendment history, legal status, and inter-ordinance relationships.

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
- [Queue Worker Setup](#queue-worker-setup)
- [Folder Structure](#folder-structure)
- [Ordinance Lifecycle](#ordinance-lifecycle)
- [Ordinance Statuses](#ordinance-statuses)
- [Ordinance Types](#ordinance-types)
- [Relationships Between Ordinances](#relationships-between-ordinances)
- [Roadmap](#roadmap)
- [License](#license)

---

## Overview

LLOIS is designed to serve local government units (LGUs) in the Philippines that need a structured, searchable, and legally traceable repository of enacted ordinances. It addresses the common problem of ordinances being stored as scattered physical documents or unorganized digital files — with no easy way to track which laws have been amended, superseded, or repealed.

The system is built on **Laravel** with **Blade** templating and a **Docker**-based infrastructure, designed for deployment in government or infrastructure-sector environments.

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
| Backend Framework | Laravel (PHP) |
| Templating | Blade |
| Database | MySQL |
| File Storage | Local disk / Laravel Storage |
| Queue System | Laravel Queue (database driver) |
| Queue Worker | Supervisor (via Docker) |
| Infrastructure | Docker + Docker Compose |
| Mail | SMTP via Laravel Mail (queued) |

---

## System Requirements

- PHP >= 8.1
- Composer
- Node.js & NPM (for asset compilation)
- MySQL 8.x
- Docker & Docker Compose (recommended)
- Supervisor (for queue workers, included in Docker setup)

---

## Installation

### 1. Clone the Repository

```bash
git clone https://github.com/your-org/llois.git
cd llois
```

### 2. Install PHP Dependencies

```bash
composer install
```

### 3. Install Node Dependencies & Compile Assets

```bash
npm install
npm run build
```

### 4. Copy Environment File

```bash
cp .env.example .env
php artisan key:generate
```

---

## Environment Configuration

Edit `.env` and configure the following:

```env
APP_NAME=LLOIS
APP_ENV=local
APP_URL=http://localhost

DB_CONNECTION=mysql
DB_HOST=127.0.0.1
DB_PORT=3306
DB_DATABASE=llois
DB_USERNAME=root
DB_PASSWORD=your_password

MAIL_MAILER=smtp
MAIL_HOST=your_smtp_host
MAIL_PORT=587
MAIL_USERNAME=your_email
MAIL_PASSWORD=your_password
MAIL_FROM_ADDRESS=noreply@yourlgu.gov.ph
MAIL_FROM_NAME="LLOIS"

QUEUE_CONNECTION=database

FILESYSTEM_DISK=local
```

> **Note:** Mail sending is handled asynchronously via Laravel's database queue to prevent timeouts on slow SMTP connections.

---

## Database Setup

```bash
php artisan migrate
php artisan db:seed   # optional: seeds sample data
```

For Docker:

```bash
docker compose exec app php artisan migrate
docker compose exec app php artisan db:seed
```

---

## Running the Application

### Without Docker

```bash
php artisan serve
```

Visit: `http://localhost:8000`

### With Docker

```bash
docker compose up -d
```

Visit: `http://localhost` (or the port configured in your `docker-compose.yml`)

---

## Queue Worker Setup

LLOIS uses Laravel's database queue for mail dispatch. In the Docker environment, **Supervisor** is already configured to run the queue worker automatically.

To verify the worker is running:

```bash
docker compose exec app supervisorctl status
```

To manually run the worker (non-Docker / local dev):

```bash
php artisan queue:work --sleep=3 --tries=3
```

To monitor failed jobs:

```bash
php artisan queue:failed
```

---

## Folder Structure

```
llois/
├── app/
│   ├── Http/
│   │   ├── Controllers/        # Ordinance, User, Report controllers
│   │   └── Middleware/         # Role-based access middleware
│   ├── Models/                 # Ordinance, Amendment, User, etc.
│   └── Services/               # Business logic (status resolution, relationship handling)
├── database/
│   ├── migrations/             # All table schemas
│   └── seeders/                # Sample data seeders
├── resources/
│   └── views/
│       ├── ordinances/         # Index, show, create, edit Blade views
│       ├── reports/            # Print-ready report views
│       └── layouts/            # App layout and navigation
├── routes/
│   └── web.php                 # All application routes
├── storage/
│   └── app/ordinances/         # Uploaded PDF files
├── docker/                     # Dockerfile, supervisor config, nginx config
├── docker-compose.yml
└── .env.example
```

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
- [ ] Ordinance relationship linking (in progress)

### Phase 2 — Reporting & Print
- [ ] Reports by year, status, type
- [ ] Print-ready ordinance detail sheet
- [ ] Export to PDF / Word

### Phase 3 — Access Control & Audit
- [ ] User login with role-based access (Admin, Encoder, Legal Officer, Viewer)
- [ ] Audit log (who added/edited what and when)

### Phase 4 — Nice to Have
- [ ] Dashboard with status summary counts
- [ ] Bulk import via Excel/CSV (for historical data migration)
- [ ] Email notifications for status changes

---

## License

This system is developed for use by Local Government Units (LGUs) in the Philippines. All rights reserved by the developing organization. Unauthorized distribution or commercial use is prohibited.

---

*Built with Laravel · Blade · MySQL · Docker*