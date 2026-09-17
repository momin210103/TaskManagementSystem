# Team Task Management System

A production-ready, role-based Team Task Management System built with **.NET 10 Web API**, **Entity Framework Core**, **PostgreSQL**, and **React 18 + Vite** styled with a responsive design system.

---

## Architecture & Technology Stack

- **Backend**: .NET 10 (ASP.NET Core Web API), Clean Architecture (API $\rightarrow$ Application $\rightarrow$ Domain $\leftarrow$ Infrastructure).
- **Security**: ASP.NET Core Identity, JWT Bearer authentication, cryptographically secure refresh token rotation & hashing.
- **Database**: PostgreSQL 17 with Entity Framework Core migrations and Fluent API configurations.
- **Frontend**: React 18, React Router v6, Axios, Lucide React, responsive CSS architecture.
- **Reverse Proxy / Static Serving**: Nginx Alpine with SPA fallback routing and `/api/` reverse proxy.
- **Containerization**: Docker multi-stage builds & Docker Compose orchestration.

---

## Quick Start with Docker Compose

### Prerequisites

- Docker Engine $\ge 24.0$ / Docker Desktop
- Docker Compose $\ge 2.20$

### 1. Configure Environment Variables

Copy `.env.example` to `.env`:

```bash
cp .env.example .env
```

### 2. Build & Start the Entire Stack

```bash
docker compose up --build -d
```

### 3. Check Container Health and Status

```bash
docker compose ps
```

### 4. View Application Logs

```bash
# View all logs
docker compose logs -f

# View specific service logs
docker compose logs -f backend
docker compose logs -f frontend
docker compose logs -f postgres
```

### 5. Stop the Application

```bash
docker compose down
```

---

## Service URLs

| Service                   | URL                                                  | Description                              |
| :------------------------ | :--------------------------------------------------- | :--------------------------------------- |
| **Frontend Application**  | `http://localhost:3000`                              | React Web Application (served via Nginx) |
| **Backend Web API**       | `http://localhost:5000` (or `http://localhost:5001`) | ASP.NET Core .NET 10 API                 |
| **Swagger / OpenAPI**     | `http://localhost:5000/swagger`                      | Interactive API documentation            |
| **Backend Health Check**  | `http://localhost:5000/health`                       | API liveness/readiness probe             |
| **Frontend Health Check** | `http://localhost:3000/nginx-health`                 | Nginx reverse proxy health check         |
| **PostgreSQL**            | `localhost:5433` (or `5432`)                         | Database server (`TaskManagementDb`)     |

---

## Pre-Seeded Demonstration Accounts

| Role           | Email                         | Password       | Scope / Permissions                          |
| :------------- | :---------------------------- | :------------- | :------------------------------------------- |
| **Admin**      | `admin@taskmanagement.com`    | `Password123!` | Full system access, users, teams, and tasks  |
| **Manager**    | `manager1@taskmanagement.com` | `Password123!` | Engineering Team Manager                     |
| **Manager**    | `manager2@taskmanagement.com` | `Password123!` | QA & Testing Team Manager                    |
| **User (Dev)** | `dev1@taskmanagement.com`     | `Password123!` | Assigned tasks, comments, status transitions |
| **User (Dev)** | `dev2@taskmanagement.com`     | `Password123!` | Assigned tasks, comments, status transitions |
| **User (QA)**  | `qa1@taskmanagement.com`      | `Password123!` | Assigned tasks, comments, status transitions |

---

## Local Development Workflow (Without Docker)

### Backend

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project src/TaskManagement.API
```

### Running Backend Unit & Integration Tests

```bash
cd backend
dotnet test
```

### Frontend

```bash
cd frontend
npm install
npm run dev
```

---

## Database Migrations & Persistence

- When starting with Docker Compose, EF Core migrations are automatically applied on startup if the database is newly created.
- PostgreSQL data is persisted across container lifecycle events using the named Docker volume `taskmanagement_postgres_data`.
- To completely reset database state:
  ```bash
  docker compose down -v
  ```

---

## Continuous Integration (CI/CD)

The repository includes an automated GitHub Actions CI pipeline configured in [`.github/workflows/ci.yml`](.github/workflows/ci.yml).

### Pipeline Workflow

```text
Git Push / Pull Request
        ↓
GitHub Actions CI Pipeline
 ├── Backend CI (Ubuntu, .NET 10 SDK)
 │    ├── dotnet restore (with NuGet caching)
 │    ├── dotnet build --configuration Release --no-restore
 │    └── dotnet test --configuration Release --no-build (322 tests)
 ├── Frontend CI (Ubuntu, Node.js 22.x)
 │    ├── npm ci (with npm caching)
 │    └── npm run build (Vite production bundle verification)
 └── Docker CI (Buildx & Compose Validation)
      ├── docker compose config validation
      ├── Dockerfile backend multi-stage image build
      └── Dockerfile frontend multi-stage image build
        ↓
CI Passed (All Checks Green)
```

### CI Pipeline Features
- **Triggers**: Executed automatically on every `push` to `main`, `pull_request` targeting `main`, and manual `workflow_dispatch`.
- **Concurrency Control**: Automatically cancels outdated in-progress runs on subsequent commits (`cancel-in-progress: true`).
- **Security & Least Privilege**: Explicit read-only repository permissions (`permissions: contents: read`) with zero exposed secrets.

