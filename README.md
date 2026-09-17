# Team Task Management System

A production-ready, role-based Team Task Management System built with **.NET 10 Web API**, **Entity Framework Core 10**, **PostgreSQL 17**, and a modern **React 18 + Vite** frontend.

---

## 1. Project Overview

The Team Task Management System provides an enterprise-grade platform for organizations to coordinate work, manage team structures, assign tasks, track progress, engage in task-level discussions, and receive automated event notifications.

The application strictly enforces **Role-Based Access Control (RBAC)** and **Resource-Based / Scope-Based Authorization** across three distinct organizational roles:
- **Admin**: Full administrative oversight across users, roles, teams, tasks, and system-wide metrics.
- **Manager**: Scoped team management, task creation, task assignment within their own team, and team-specific performance tracking.
- **User (Member)**: Individual task execution, status updates, task comments, personal notifications, and individual dashboard metrics.

---

## 2. Key Features

- **Authentication & Security**:
  - Secure registration and login powered by ASP.NET Core Identity (PBKDF2 with HMAC-SHA256 password hashing).
  - Short-lived JWT access tokens (15-minute expiration) with cryptographic HMAC-SHA256 signing.
  - Cryptographically secure 64-byte Refresh Tokens stored as SHA-256 hashes with automatic token rotation and revocation on logout.
  - Strong defense against Insecure Direct Object Reference (IDOR), vertical/horizontal privilege escalation, and over-posting.
- **User Management**:
  - Profile retrieval (`/api/users/me`), user directory with role and team filtering, Admin-controlled role modifications, and team assignments.
- **Team Management**:
  - Organization team creation, case-insensitive unique name validation, manager assignment, and dynamic team membership management.
- **Task Management**:
  - Task creation, full description support, due date deadlines, priority levels (`Low`, `Medium`, `High`), and lifecycle states (`ToDo`, `InProgress`, `Done`).
  - Task assignment with strict team boundary validation (assignees must belong to the task's assigned team).
  - Server-enforced `AssignedById` derived directly from authenticated JWT claims.
- **Comments**:
  - Collaborative task discussion threads with author attribution, timestamps, and access control (view/comment permissions tied to task access).
- **In-App Notifications**:
  - Automated event-driven notifications generated upon task assignment, reassignment, and status transitions.
  - User-scoped notification center with unread filtering, individual read toggle, and "Mark all as read".
- **Real-Time Dashboard**:
  - High-performance, database-aggregated summary metrics (Total, To Do, In Progress, Done, High Priority, Overdue, Due Today, Upcoming).
  - Role-scoped task listings with real-time status, priority, and date range filters.
- **Frontend UI / UX**:
  - Modern, responsive SaaS interface adapting across mobile (320px+), tablet, and desktop (widescreen).
  - Dedicated desktop sidebar (`lg:` breakpoint) and slide-over mobile drawer navigation.
  - Contextual modals, skeleton loaders, confirmation dialogs, and RFC 7807 `ProblemDetails` error formatting.

---

## 3. Technology Stack

### Backend
- **Framework**: .NET 10 (ASP.NET Core Web API)
- **Language**: C# 13
- **ORM**: Entity Framework Core 10
- **Database Provider**: Npgsql PostgreSQL Provider
- **Database Engine**: PostgreSQL 17 Alpine
- **Identity & Security**: ASP.NET Core Identity, `Microsoft.AspNetCore.Authentication.JwtBearer`
- **Validation**: FluentValidation & Model State validation
- **Documentation**: Swagger / OpenAPI (`Swashbuckle.AspNetCore` & `Microsoft.AspNetCore.OpenApi`)

### Frontend
- **Library**: React 18
- **Build Tool**: Vite 6
- **Routing**: React Router DOM v6
- **HTTP Client**: Axios with automated request/response interceptors & token refresh queue
- **Icons**: Lucide React
- **Styling**: Tailwind CSS & custom design system

### Testing
- **Framework**: xUnit
- **Database Isolation**: EF Core In-Memory Provider for isolated unit & authorization test suites

### DevOps & Containerization
- **Web Server / Reverse Proxy**: Nginx Alpine (SPA routing fallback + `/api/` reverse proxy)
- **Containers**: Multi-stage Dockerfiles (`backend/Dockerfile`, `frontend/Dockerfile`)
- **Orchestration**: Docker Compose
- **CI/CD**: GitHub Actions automated pipeline (`.github/workflows/ci.yml`)

---

## 4. Architecture

The backend strictly follows **Clean Architecture** principles to ensure complete separation of concerns and maintainability:

```text
TaskManagement.API (Presentation / HTTP Layer)
       │
       ▼
TaskManagement.Application (Business Logic & Use Cases)
       │
       ▼
TaskManagement.Domain (Entities & Core Business Rules)
       ▲
       │
TaskManagement.Infrastructure (Persistence, EF Core, Identity, JWT)
```

### Layer Responsibilities

| Project | Responsibilities |
| :--- | :--- |
| **`TaskManagement.Domain`** | Enterprise business models (`ApplicationUser`, `Team`, `TaskItem`, `Comment`, `Notification`, `RefreshToken`), Domain Enums (`TaskStatus`, `TaskPriority`, `NotificationType`). Zero external dependencies. |
| **`TaskManagement.Application`** | Application interfaces (`ITaskService`, `ITeamService`, `IUserService`, etc.), Request/Response DTOs, Business validation rules, Custom exception hierarchy. |
| **`TaskManagement.Infrastructure`** | `ApplicationDbContext`, EF Core Fluent API configurations, Npgsql migrations, ASP.NET Core Identity integration, JWT token generator, SHA-256 refresh token management, Repositories with database-level aggregation. |
| **`TaskManagement.API`** | Thin ASP.NET Core Controllers, HTTP middleware pipeline, JWT Bearer configuration, CORS policies, Exception/ProblemDetails handling, Swagger/OpenAPI endpoints. |

---

## 5. Project Structure

```text
TaskManagementSystem/
├── .github/
│   └── workflows/
│       └── ci.yml                      # GitHub Actions CI pipeline
├── backend/
│   ├── TaskManagement.sln              # Visual Studio / .NET Solution
│   ├── Directory.Build.props           # Common build configuration
│   ├── Directory.Packages.props        # Centralized package management
│   ├── .editorconfig                   # C# formatting & linting rules
│   ├── Dockerfile                      # Multi-stage .NET 10 API build
│   ├── src/
│   │   ├── TaskManagement.API/         # Web API controllers & startup
│   │   ├── TaskManagement.Application/ # Services, DTOs, interfaces
│   │   ├── TaskManagement.Domain/      # Domain entities & enums
│   │   └── TaskManagement.Infrastructure/ # EF Core, DB migrations, Identity
│   └── tests/
│       └── TaskManagement.UnitTests/   # xUnit unit & authorization tests
├── frontend/
│   ├── Dockerfile                      # Multi-stage Node 22 + Nginx build
│   ├── nginx.conf                      # Nginx SPA fallback & proxy configuration
│   ├── package.json                    # Frontend dependencies & scripts
│   ├── vite.config.js                  # Vite bundler configuration
│   └── src/
│       ├── api/                        # Axios client & token interceptors
│       ├── components/                 # Reusable UI components & modals
│       ├── context/                    # AuthContext & state providers
│       ├── layouts/                    # AppLayout, Navbar, Sidebar
│       ├── pages/                      # Dashboard, Tasks, Teams, Users, Auth
│       ├── routes/                     # Protected & role-based routing
│       └── services/                   # Frontend API service abstractions
├── docs/
│   ├── task-management-db-api-design.md # Original design & specification
│   ├── database-er-diagram.md          # Database ER diagram & schema
│   ├── video-walkthrough-script.md     # 5-8 min video walkthrough guide
│   └── submission-checklist.md         # Final assessment checklist
├── docker-compose.yml                  # Multi-container orchestration
├── .env.example                        # Template environment variables
├── .gitignore                          # Git ignore rules
└── README.md                           # Master project documentation
```

---

## 6. Prerequisites

### For Running with Docker Compose (Recommended)
- **Docker Engine**: $\ge 24.0$ (or Docker Desktop)
- **Docker Compose**: $\ge 2.20$

### For Local Development (Without Docker)
- **.NET SDK**: `10.0.x`
- **Node.js**: `v20.x` or `v22.x`
- **npm**: $\ge 10.0$
- **PostgreSQL**: `17.x` installed and running locally

---

## 7. Environment Variables

The project uses configuration-driven environment variables. A template is provided in [`.env.example`](.env.example):

```env
# PostgreSQL Configuration
POSTGRES_DB=TaskManagementDb
POSTGRES_USER=postgres
POSTGRES_PASSWORD=YourSecurePasswordHere
POSTGRES_PORT=5433

# JWT Configuration
JWT_SECRET=YourSuperSecretKeyWithMinimum32CharactersLength2026!
JWT_ISSUER=TaskManagementAPI
JWT_AUDIENCE=TaskManagementClients
JWT_ACCESS_EXP_MINUTES=15
JWT_REFRESH_EXP_DAYS=7

# ASP.NET Core Environment
ASPNETCORE_ENVIRONMENT=Development

# Frontend API URL Configuration
VITE_API_BASE_URL=/api
```

---

## 8. Quick Start with Docker Compose

### 1. Initialize Configuration
```bash
cp .env.example .env
```

### 2. Build and Launch Containers
```bash
docker compose up --build -d
```

### 3. Verify Container Health
```bash
docker compose ps
```
*Expected Output:*
```text
NAME                      IMAGE                           STATUS
taskmanagement_postgres   postgres:17-alpine              Up (healthy)
taskmanagement_backend    taskmanagementsystem-backend    Up (healthy)
taskmanagement_frontend   taskmanagementsystem-frontend   Up (healthy)
```

### 4. Service Endpoints
- **React Frontend**: [http://localhost:3000](http://localhost:3000)
- **Backend API & Swagger**: [http://localhost:5000/swagger](http://localhost:5000/swagger)
- **Backend Health Check**: [http://localhost:5000/health](http://localhost:5000/health)
- **PostgreSQL Server**: `localhost:5433` (Database: `TaskManagementDb`)

### 5. Stop Containers
```bash
# Stop containers (preserves database volume)
docker compose down

# Stop containers and reset database volume
docker compose down -v
```

---

## 9. Local Development Setup (Without Docker)

### 1. Database Setup
Ensure PostgreSQL is running locally and create a database named `TaskManagementDb`.

### 2. Backend API Setup
```bash
cd backend

# Restore NuGet dependencies
dotnet restore

# Build solution in Debug/Release
dotnet build

# Apply EF Core migrations to PostgreSQL
dotnet ef database update --project src/TaskManagement.Infrastructure --startup-project src/TaskManagement.API

# Run the API server (launches on http://localhost:5000)
dotnet run --project src/TaskManagement.API
```

### 3. Frontend Application Setup
```bash
cd frontend

# Install Node dependencies
npm ci

# Launch Vite development server (launches on http://localhost:5173)
npm run dev
```

---

## 10. Pre-Seeded Demonstration Accounts

When the application starts, the `DatabaseSeeder` automatically seeds default roles, teams, users, and tasks for immediate evaluation:

| Role | Email | Password | Assigned Team / Scope |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@taskmanagement.com` | `Password123!` | System-wide full administrative access |
| **Manager** | `manager1@taskmanagement.com` | `Password123!` | Manager of **Engineering Team** |
| **Manager** | `manager2@taskmanagement.com` | `Password123!` | Manager of **QA & Testing Team** |
| **User (Dev)** | `dev1@taskmanagement.com` | `Password123!` | Member of **Engineering Team** |
| **User (Dev)** | `dev2@taskmanagement.com` | `Password123!` | Member of **Engineering Team** |
| **User (QA)** | `qa1@taskmanagement.com` | `Password123!` | Member of **QA & Testing Team** |

---

## 11. Role & Authorization Matrix

| Feature / Action | Admin | Manager | User (Member) |
| :--- | :---: | :---: | :---: |
| **Register & Login** | ✓ | ✓ | ✓ |
| **View Current User Profile (`/me`)** | ✓ | ✓ | ✓ |
| **View User Directory** | ✓ (All users) | ✓ (Own team members) | ✗ (Forbidden) |
| **Update User Roles / Teams** | ✓ | ✗ (Forbidden) | ✗ (Forbidden) |
| **Create Teams** | ✓ | ✗ (Forbidden) | ✗ (Forbidden) |
| **View Teams** | ✓ (All teams) | ✓ (Own team) | ✓ (Own team) |
| **Update Team Details** | ✓ | ✗ (Forbidden) | ✗ (Forbidden) |
| **Add / Remove Team Members** | ✓ (Any team) | ✓ (Own team only) | ✗ (Forbidden) |
| **Create Tasks** | ✓ (Any team) | ✓ (Own team only) | ✗ (Forbidden) |
| **View Tasks** | ✓ (All tasks) | ✓ (Own team tasks) | ✓ (Assigned tasks only) |
| **Update General Task Details** | ✓ | ✓ (Own team tasks) | ✗ (Forbidden) |
| **Update Task Status** | ✓ | ✓ (Own team tasks) | ✓ (Assigned tasks only) |
| **Assign / Reassign Tasks** | ✓ | ✓ (Within own team) | ✗ (Forbidden) |
| **Delete Tasks** | ✓ | ✓ (Own team tasks) | ✗ (Forbidden) |
| **Create Comments** | ✓ | ✓ (On own team tasks) | ✓ (On assigned tasks) |
| **Delete Comments** | ✓ | ✓ (On own team tasks) | ✓ (Own comments only) |
| **View & Manage Notifications** | ✓ (Own) | ✓ (Own) | ✓ (Own) |
| **View Dashboard** | ✓ (System metrics) | ✓ (Team metrics) | ✓ (Personal metrics) |

---

## 12. API Endpoint Reference

### Authentication (`/api/auth`)
- `POST /api/auth/register` — Register a new user account (default role: `User`).
- `POST /api/auth/login` — Authenticate credentials; returns JWT access token and refresh token.
- `POST /api/auth/refresh` — Exchange valid refresh token for a new access token and rotated refresh token.
- `POST /api/auth/logout` — Revoke the active refresh token session.

### Users (`/api/users`)
- `GET /api/users/me` — Retrieve the authenticated user's profile.
- `GET /api/users` — Paginated user directory with role and team filters (*Admin, Manager*).
- `GET /api/users/{id}` — Retrieve user by ID (*Admin, Manager for own team, or Self*).
- `PATCH /api/users/{id}/role` — Update a user's role (*Admin only*).
- `PATCH /api/users/{id}/team` — Assign or change a user's team (*Admin only*).

### Teams (`/api/teams`)
- `POST /api/teams` — Create a new organization team (*Admin only*).
- `GET /api/teams` — List teams (*Admin: all teams, Manager/User: own team*).
- `GET /api/teams/{id}` — Retrieve team details and member list.
- `PUT /api/teams/{id}` / `PATCH /api/teams/{id}` — Update team name or manager (*Admin only*).
- `POST /api/teams/{id}/members` — Add a member to the team (*Admin, Manager of team*).
- `DELETE /api/teams/{id}/members/{userId}` — Remove a member from the team (*Admin, Manager of team*).

### Tasks (`/api/tasks`)
- `POST /api/tasks` — Create a new task (*Admin, Manager of team*).
- `GET /api/tasks` — List paginated tasks with filters (*Status, Priority, Deadline, Team, Assignee*).
- `GET /api/tasks/{id}` — Retrieve detailed task information.
- `PUT /api/tasks/{id}` — Update task title, description, priority, deadline (*Admin, Manager of team*).
- `PATCH /api/tasks/{id}/status` — Transition task status (`ToDo`, `InProgress`, `Done`).
- `PATCH /api/tasks/{id}/assign` — Assign task to a team member (*Admin, Manager of team*).
- `DELETE /api/tasks/{id}` — Remove a task (*Admin, Manager of team*).

### Comments (`/api/tasks/{taskId}/comments`)
- `POST /api/tasks/{taskId}/comments` — Add a comment to a task.
- `GET /api/tasks/{taskId}/comments` — Retrieve all comments for a task.
- `DELETE /api/tasks/{taskId}/comments/{commentId}` — Delete a comment (*Admin, Manager of task team, or Author*).

### Notifications (`/api/notifications`)
- `GET /api/notifications` — List paginated notifications for the authenticated user.
- `PATCH /api/notifications/{id}/read` — Mark a single notification as read.
- `PATCH /api/notifications/read-all` — Mark all notifications as read for current user.

### Dashboard (`/api/dashboard`)
- `GET /api/dashboard/summary` — Aggregate task summary metrics scoped to user role.
- `GET /api/dashboard/tasks` — Paginated and filterable task listing for dashboard views.

---

## 13. Database Schema & ER Diagram

Detailed database documentation and the complete Mermaid Entity-Relationship diagram are available in [docs/database-er-diagram.md](docs/database-er-diagram.md).

### Core Entities
- **`AspNetUsers`**: Custom user identity containing `Name`, `Email`, `TeamId`, and timestamps.
- **`Teams`**: Organization team containing `Name`, `ManagerId`, and navigation to members/tasks.
- **`Tasks` (`TaskItem`)**: Core work item with `Title`, `Description`, `Status`, `Priority`, `Deadline`, `TeamId`, `AssignedToId`, `AssignedById`.
- **`Comments`**: Discussion entry linked to `TaskId` and `UserId`.
- **`Notifications`**: Event notification record with `UserId`, `TaskId`, `Type`, `Message`, and `IsRead`.
- **`RefreshTokens`**: Cryptographic SHA-256 token hash records with expiration and revocation tracking.

---

## 14. Testing & Quality Assurance

The backend includes a comprehensive automated test suite testing application services, validation rules, role authorization, resource-level ownership, and IDOR prevention:

```bash
# Run all tests in Release configuration
dotnet test backend/TaskManagement.sln --configuration Release
```

### Verified Test Results
- **Total Tests**: **322**
- **Passed**: **322**
- **Failed**: **0**
- **Skipped**: **0**
- **Execution Time**: ~13–15 seconds

---

## 15. Continuous Integration (CI/CD)

An automated GitHub Actions CI pipeline is configured in [`.github/workflows/ci.yml`](.github/workflows/ci.yml):

- **Triggers**: Executed on every `push` to `main`, `pull_request` targeting `main`, and manual dispatch (`workflow_dispatch`).
- **Jobs**:
  1. **Backend CI**: .NET 10 SDK setup, NuGet caching, `dotnet restore`, `dotnet build --configuration Release`, and `dotnet test` (322 tests passing).
  2. **Frontend CI**: Node.js 22.x setup, npm caching, `npm ci`, and `npm run build` (Vite production bundle verification).
  3. **Docker CI**: Docker Buildx setup, Compose syntax validation, and backend/frontend container image compilation (`push: false`).

---

## 16. Assessment Deliverables Reference

- **Database ER Documentation**: [docs/database-er-diagram.md](docs/database-er-diagram.md)
- **Video Walkthrough Script**: [docs/video-walkthrough-script.md](docs/video-walkthrough-script.md)
- **Submission Checklist**: [docs/submission-checklist.md](docs/submission-checklist.md)
- **Live Demo Note**: Per assessment guidelines, live deployment is optional; the complete multi-container system is runnable locally via `docker compose up --build`.
