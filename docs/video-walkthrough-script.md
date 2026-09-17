# Video Walkthrough Script & Presentation Checklist

This document provides the structured script and demonstration agenda for recording the **5–8 minute video walkthrough** of the Team Task Management System.

> [!NOTE]
> **Status**: Script & checklist prepared. Video recording is a manual action performed by the presenter prior to final submission.

---

## 1. Timing Breakdown & Presentation Agenda

| Timestamp | Section | Key Talking Points & Live Actions |
| :--- | :--- | :--- |
| **0:00 – 0:30** | **Introduction** | - Welcome & high-level overview of the Team Task Management System.<br>- Problem solved: Multi-tenant, role-based task coordination and team oversight. |
| **0:30 – 1:15** | **Architecture & Tech Stack** | - **Backend**: .NET 10 Web API, Clean Architecture, EF Core, PostgreSQL 17.<br>- **Frontend**: React 18, Vite, responsive UI system.<br>- **Security**: ASP.NET Core Identity, JWT Bearer, SHA-256 Refresh Token rotation. |
| **1:15 – 2:15** | **Authentication & Role Access** | - Show Login & Registration screens.<br>- Explain 3 core roles: **Admin**, **Manager**, **User**.<br>- Demonstrate logging in as `admin@taskmanagement.com` to show full system access.<br>- Switch to `manager1@taskmanagement.com` to demonstrate scoped team view.<br>- Switch to `dev1@taskmanagement.com` to demonstrate individual assigned task view. |
| **2:15 – 3:30** | **Teams & Task Management** | - Demonstrate Team Management (create team, view members, assign members).<br>- Demonstrate Task Creation (title, description, status `ToDo`, priority `High`, deadline).<br>- Demonstrate Task Assignment validation (assignee must belong to task team).<br>- Demonstrate Task Status transitions (`ToDo` $\rightarrow$ `InProgress` $\rightarrow$ `Done`). |
| **3:30 – 4:15** | **Comments & Notifications** | - Open a task details page.<br>- Post a comment as an assignee or manager.<br>- Demonstrate in-app event notifications generated automatically upon task assignment and status updates.<br>- Mark notification as read and test "Mark all as read". |
| **4:15 – 5:00** | **Dashboard & Filtering** | - Show Role-scoped Dashboard (Admin system metrics vs Manager team metrics vs User personal metrics).<br>- Demonstrate real-time filters: status, priority, and date range.<br>- Demonstrate responsive layout adaptation (desktop widescreen vs tablet/mobile sidebar drawer). |
| **5:00 – 5:45** | **API Documentation (Swagger)** | - Navigate to `http://localhost:5000/swagger`.<br>- Show OpenAPI specification with JWT Bearer authorization.<br>- Highlight ProblemDetails RFC 7807 error responses and DTO validation. |
| **5:45 – 6:30** | **Docker Containerization** | - Show `docker-compose.yml` and container health (`docker compose ps`).<br>- Show 3 healthy services: `taskmanagement_postgres`, `taskmanagement_backend`, `taskmanagement_frontend`.<br>- Demonstrate persistent data volume across restarts. |
| **6:30 – 7:15** | **Testing & CI/CD Pipeline** | - Run `dotnet test backend/TaskManagement.sln` showing all **322 automated unit/integration tests passing**.<br>- Show GitHub Actions CI pipeline (`.github/workflows/ci.yml`) passing all checks (Backend CI, Frontend CI, Docker Build Validation). |
| **7:15 – 8:00** | **Conclusion & Wrap-Up** | - Recap Clean Architecture modularity, security posture, and compliance with project specification.<br>- Thank the reviewer. |

---

## 2. Presenter Demonstration Checklist

### Pre-Recording Setup
- [ ] Docker containers running: `docker compose up -d`
- [ ] Browser tabs prepared:
  - Tab 1: `http://localhost:3000` (React Frontend)
  - Tab 2: `http://localhost:5000/swagger` (Swagger API Docs)
  - Tab 3: GitHub repository / GitHub Actions CI tab
- [ ] Terminal window ready with command: `dotnet test backend/TaskManagement.sln`

### Demo Accounts
- **Admin**: `admin@taskmanagement.com` / `Password123!`
- **Manager**: `manager1@taskmanagement.com` / `Password123!`
- **User (Dev)**: `dev1@taskmanagement.com` / `Password123!`
- **User (QA)**: `qa1@taskmanagement.com` / `Password123!`
