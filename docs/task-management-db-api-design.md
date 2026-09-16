# Task Management System --- Database Design & API Specification

## 1. Project Overview

### 1.1 Project Name

**Team Task Management System**

### 1.2 Objective

Build a role-based task management system that allows organizations to:

-   Manage teams and team members.
-   Create, assign, and manage tasks.
-   Track task progress.
-   Collaborate through comments.
-   Notify users about important task events.
-   Provide role-specific dashboards and task filtering.

The system has three roles:

-   **Admin**
-   **Manager**
-   **User**

------------------------------------------------------------------------

# 2. Technology Stack

  Layer               Technology
  ------------------- ------------------------------------------------------
  Backend             ASP.NET Core Web API / .NET
  Authentication      ASP.NET Core Identity + JWT
  Authorization       Role-Based Authorization + Resource/Ownership Checks
  ORM                 Entity Framework Core
  Database            PostgreSQL
  API Style           REST API
  API Documentation   Swagger / OpenAPI
  Frontend            React
  HTTP Client         Axios
  Validation          FluentValidation
  Logging             Serilog
  Testing             xUnit + Integration Tests
  Containerization    Docker + Docker Compose
  CI/CD               GitHub Actions
  Deployment          Render / Railway / Vercel or equivalent

------------------------------------------------------------------------

# 3. Architecture

The backend will use a layered/clean architecture approach with
feature-based organization.

``` text
TaskManagement.sln

src/
├── TaskManagement.API
│   ├── Controllers/
│   ├── Middleware/
│   ├── Extensions/
│   └── Program.cs
│
├── TaskManagement.Application
│   ├── Features/
│   │   ├── Auth/
│   │   ├── Users/
│   │   ├── Teams/
│   │   ├── Tasks/
│   │   ├── Comments/
│   │   ├── Notifications/
│   │   └── Dashboard/
│   ├── DTOs/
│   ├── Interfaces/
│   ├── Validators/
│   └── Common/
│
├── TaskManagement.Domain
│   ├── Entities/
│   ├── Enums/
│   └── Common/
│
└── TaskManagement.Infrastructure
    ├── Persistence/
    ├── Identity/
    ├── Repositories/
    ├── Services/
    └── Configurations/

tests/
├── TaskManagement.UnitTests/
└── TaskManagement.IntegrationTests/
```

### Layer Responsibilities

  -----------------------------------------------------------------------
  Layer                               Responsibility
  ----------------------------------- -----------------------------------
  API                                 HTTP endpoints, authentication
                                      pipeline, middleware,
                                      request/response handling

  Application                         Business use cases, DTOs,
                                      validation, interfaces,
                                      authorization rules

  Domain                              Entities, enums, domain rules, core
                                      business concepts

  Infrastructure                      EF Core, PostgreSQL, Identity,
                                      repositories, external services

  Unit Tests                          Test business logic independently

  Integration Tests                   Test API endpoints and
                                      database-related workflows
  -----------------------------------------------------------------------

------------------------------------------------------------------------

# 4. Roles and Permissions

## 4.1 Admin

Admin has system-wide access.

-   Manage users.
-   Change user roles.
-   Create and manage teams.
-   Assign/reassign team managers.
-   View all teams.
-   View all tasks.
-   Create, assign, update, and delete tasks.
-   View and manage comments according to moderation rules.

## 4.2 Manager

Manager operates within their own team.

-   View their team.
-   Add/remove users from their own team.
-   Create tasks for their team.
-   Assign tasks to members of their own team.
-   Update tasks belonging to their team.
-   Reassign tasks within their team.
-   View team task information.
-   Manage task collaboration within their scope.

## 4.3 User

User can work only with tasks assigned to them.

-   View own profile.
-   View assigned tasks.
-   Update status of assigned tasks.
-   Add comments to assigned tasks.
-   View comments for accessible tasks.
-   View own notifications.

------------------------------------------------------------------------

# 5. Database Design

## 5.1 Users

If ASP.NET Core Identity is used, the application user will extend
`IdentityUser<Guid>`.

### ApplicationUser

  Column      Type           Notes
  ----------- -------------- -----------------------------
  Id          UUID           Primary key
  Name        varchar(100)   Required
  Email       varchar(150)   Unique; managed by Identity
  TeamId      UUID           FK → Teams.Id, nullable
  CreatedAt   timestamp      Audit field
  UpdatedAt   timestamp      Audit field

Authentication-specific fields such as password hash and normalized
email should be managed by ASP.NET Core Identity rather than duplicated
in the domain model.

Roles:

``` text
Admin
Manager
User
```

Identity will manage role relationships through its standard role
tables.

------------------------------------------------------------------------

## 5.2 Teams

  Column      Type           Notes
  ----------- -------------- --------------------------
  Id          UUID           Primary key
  Name        varchar(100)   Required
  ManagerId   UUID           FK → ApplicationUsers.Id
  CreatedAt   timestamp      Audit field
  UpdatedAt   timestamp      Audit field

### Business Rule

`ManagerId` must reference a user whose role is `Manager`.

------------------------------------------------------------------------

## 5.3 Tasks

  Column         Type           Notes
  -------------- -------------- --------------------------
  Id             UUID           Primary key
  Title          varchar(150)   Required
  Description    text           Optional
  Status         enum/string    To Do, In Progress, Done
  Priority       enum/string    Low, Medium, High
  Deadline       date           Required
  TeamId         UUID           FK → Teams.Id
  AssignedToId   UUID           FK → ApplicationUsers.Id
  AssignedById   UUID           FK → ApplicationUsers.Id
  CreatedAt      timestamp      Audit field
  UpdatedAt      timestamp      Audit field

### Task Statuses

``` text
To Do
In Progress
Done
```

### Task Priorities

``` text
Low
Medium
High
```

### Business Rules

1.  `AssignedToId` must reference an existing user.
2.  The assignee must belong to the task's team.
3.  A Manager can assign tasks only within their own team.
4.  Admin can manage tasks across all teams.
5.  User can update status only when they are the assignee.
6.  Deadline cannot be invalid according to the application's validation
    rules.

------------------------------------------------------------------------

## 5.4 Comments

  Column      Type        Notes
  ----------- ----------- --------------------------
  Id          UUID        Primary key
  TaskId      UUID        FK → Tasks.Id
  UserId      UUID        FK → ApplicationUsers.Id
  Content     text        Required
  CreatedAt   timestamp   Audit field
  UpdatedAt   timestamp   Audit field

The comment author must have access to the associated task.

------------------------------------------------------------------------

## 5.5 Notifications

  Column      Type           Notes
  ----------- -------------- --------------------------
  Id          UUID           Primary key
  UserId      UUID           FK → ApplicationUsers.Id
  TaskId      UUID           FK → Tasks.Id, nullable
  Type        enum/string    Assignment, StatusUpdate
  Message     varchar(255)   Notification message
  IsRead      boolean        Default false
  CreatedAt   timestamp      Audit field

### Notification Events

#### Task Assignment

When a task is assigned/reassigned:

``` text
Task
  ↓
Notification
  ↓
Assignee
```

The assignee receives an `Assignment` notification.

#### Task Status Update

When a task status changes:

``` text
Task
  ↓
Notification
  ↓
Task Assigner / Relevant Manager
```

The configured recipient receives a `StatusUpdate` notification.

If email integration is not implemented, the database notification acts
as the required mock notification.

------------------------------------------------------------------------

## 5.6 Refresh Tokens

Refresh tokens are required because the API exposes refresh/logout
operations.

  Column              Type        Notes
  ------------------- ----------- ---------------------------------------
  Id                  UUID        Primary key
  UserId              UUID        FK → ApplicationUsers.Id
  TokenHash           varchar     Store a protected/hash representation
  ExpiresAt           timestamp   Expiration time
  CreatedAt           timestamp   Creation time
  RevokedAt           timestamp   Nullable
  ReplacedByTokenId   UUID        Nullable

### Refresh Token Rules

-   Access tokens should be short-lived.
-   Refresh tokens should have a longer lifetime.
-   Revoked or expired refresh tokens cannot be used.
-   Logout should revoke the relevant refresh token/session.
-   Token rotation can be used when refreshing.

------------------------------------------------------------------------

# 6. Entity Relationships

``` text
ApplicationUser
    │
    ├─────────────── N ──────────────── Teams (as Manager)
    │
    ├─────────────── N ──────────────── Tasks (AssignedTo)
    │
    ├─────────────── N ──────────────── Tasks (AssignedBy)
    │
    ├─────────────── N ──────────────── Comments
    │
    ├─────────────── N ──────────────── Notifications
    │
    └─────────────── N ──────────────── RefreshTokens

Team
    │
    ├─────────────── N ──────────────── Users
    │
    └─────────────── N ──────────────── Tasks

Task
    │
    └─────────────── N ──────────────── Comments
```

### Team Membership Model

For the assessment, a user belongs to one team through
`ApplicationUser.TeamId`.

If the system later requires users to belong to multiple teams,
introduce:

``` text
TeamMembers
-----------
TeamId
UserId
CreatedAt
```

------------------------------------------------------------------------

# 7. Authentication Design

## 7.1 Registration

``` http
POST /api/auth/register
```

Public endpoint.

New public registrations receive the default `User` role.

A client must not be allowed to register itself as `Admin` or `Manager`.

## 7.2 Login

``` http
POST /api/auth/login
```

Returns:

-   Access token
-   Refresh token
-   User information
-   Role information

## 7.3 Refresh

``` http
POST /api/auth/refresh
```

Validates the refresh token and issues a new access token.

## 7.4 Logout

``` http
POST /api/auth/logout
```

Revokes the active refresh token/session.

## 7.5 Token Expiration

Expired access tokens should result in:

``` http
401 Unauthorized
```

The frontend can use the refresh flow when appropriate. If the refresh
token is expired/revoked, the user must authenticate again.

------------------------------------------------------------------------

# 8. Authorization Strategy

Authorization uses two levels:

### Level 1 --- Role Authorization

Example:

``` csharp
[Authorize(Roles = "Admin")]
```

or equivalent policy-based authorization.

### Level 2 --- Resource/Ownership Authorization

Role checks alone are not enough.

Examples:

``` text
Manager → own team only
User → own assigned tasks only
Comment author → own comment
```

------------------------------------------------------------------------

# 9. Authorization Matrix

  Operation                          Admin       Manager            User
  ------------------------------- -------- ------------- ---------------
  Register                          Public        Public          Public
  Login                             Public        Public          Public
  Own profile                          Yes           Yes             Yes
  List all users                       Yes   Team-scoped              No
  View specific user                   Yes        Scoped              No
  Change role                          Yes            No              No
  Assign user to team                  Yes      Own team              No
  Create team                          Yes            No              No
  List teams                           Yes   Yes, scoped              No
  View team details                    Yes      Own team              No
  Update team                          Yes            No              No
  Add team member                      Yes      Own team              No
  Remove team member                   Yes      Own team              No
  Create task                          Yes      Own team              No
  List tasks                           All      Own team   Assigned only
  View task                            All      Own team   Assigned only
  Update task                          Yes      Own team              No
  Update task status                   Yes        Scoped   Assigned only
  Assign/reassign task                 Yes      Own team              No
  Delete task                          Yes      Own team              No
  Add comment                       Scoped        Scoped   Assigned task
  View comments                     Scoped        Scoped   Assigned task
  Delete own comment                   Yes           Yes             Yes
  Delete another user's comment        Yes            No              No
  View notifications                   Own           Own             Own
  Mark notification read               Own           Own             Own

------------------------------------------------------------------------

# 10. API Routes

## 10.1 Authentication

  ----------------------------------------------------------------------------------
  Method            Route                  Access                  Description
  ----------------- ---------------------- ----------------------- -----------------
  POST              `/api/auth/register`   Public                  Register a new
                                                                   user

  POST              `/api/auth/login`      Public                  Login and return
                                                                   JWT

  POST              `/api/auth/refresh`    Authenticated/Refresh   Refresh access
                                           Token                   token

  POST              `/api/auth/logout`     Authenticated           Revoke
                                                                   session/refresh
                                                                   token
  ----------------------------------------------------------------------------------

------------------------------------------------------------------------

## 10.2 Users

  ------------------------------------------------------------------------------
  Method            Route                    Access            Description
  ----------------- ------------------------ ----------------- -----------------
  GET               `/api/users/me`          Authenticated     Get own profile

  GET               `/api/users`             Admin             List all users

  GET               `/api/users/{id}`        Admin, Manager    Get a specific
                                                               user within scope

  PATCH             `/api/users/{id}/role`   Admin             Change user's
                                                               role

  PATCH             `/api/users/{id}/team`   Admin             Assign user to a
                                                               team
  ------------------------------------------------------------------------------

------------------------------------------------------------------------

## 10.3 Teams

  ------------------------------------------------------------------------------------------
  Method            Route                                Access            Description
  ----------------- ------------------------------------ ----------------- -----------------
  POST              `/api/teams`                         Admin             Create a team

  GET               `/api/teams`                         Admin, Manager    List teams within
                                                                           scope

  GET               `/api/teams/{id}`                    Admin, Manager    Team details and
                                                                           members

  PATCH             `/api/teams/{id}`                    Admin             Update
                                                                           team/reassign
                                                                           manager

  POST              `/api/teams/{id}/members`            Admin, Manager    Add a user to the
                                                                           team

  DELETE            `/api/teams/{id}/members/{userId}`   Admin, Manager    Remove a user
                                                                           from the team
  ------------------------------------------------------------------------------------------

------------------------------------------------------------------------

## 10.4 Tasks

  --------------------------------------------------------------------------------
  Method            Route                      Access            Description
  ----------------- -------------------------- ----------------- -----------------
  POST              `/api/tasks`               Admin, Manager    Create and assign
                                                                 a task

  GET               `/api/tasks`               Authenticated     List tasks
                                                                 according to role
                                                                 scope

  GET               `/api/tasks/{id}`          Authenticated     Get task details
                                                                 according to
                                                                 scope

  PATCH             `/api/tasks/{id}`          Admin, Manager    Update task
                                                                 fields

  PATCH             `/api/tasks/{id}/status`   Assignee, Admin,  Update task
                                               Manager           status

  PATCH             `/api/tasks/{id}/assign`   Admin, Manager    Assign/reassign
                                                                 task

  DELETE            `/api/tasks/{id}`          Admin, Manager    Delete task
  --------------------------------------------------------------------------------

### Task Filters

``` http
GET /api/tasks?status=In%20Progress
GET /api/tasks?priority=High
GET /api/tasks?deadline=2026-09-30
```

Optional production-oriented query parameters:

``` text
page
pageSize
sortBy
sortDirection
```

Example:

``` http
GET /api/tasks?status=In%20Progress&priority=High&page=1&pageSize=20
```

------------------------------------------------------------------------

## 10.5 Comments

  Method   Route                            Access          Description
  -------- -------------------------------- --------------- --------------------
  POST     `/api/tasks/{taskId}/comments`   Scoped          Add a comment
  GET      `/api/tasks/{taskId}/comments`   Scoped          List task comments
  DELETE   `/api/comments/{id}`             Author, Admin   Delete a comment

------------------------------------------------------------------------

## 10.6 Notifications

  --------------------------------------------------------------------------------------
  Method            Route                            Access            Description
  ----------------- -------------------------------- ----------------- -----------------
  GET               `/api/notifications`             Authenticated     List own
                                                                       notifications

  PATCH             `/api/notifications/{id}/read`   Authenticated     Mark notification
                                                                       as read

  PATCH             `/api/notifications/read-all`    Authenticated     Mark all
                                                                       notifications as
                                                                       read
  --------------------------------------------------------------------------------------

Users must only be able to modify their own notifications.

------------------------------------------------------------------------

## 10.7 Dashboard

  --------------------------------------------------------------------------------
  Method            Route                      Access            Description
  ----------------- -------------------------- ----------------- -----------------
  GET               `/api/dashboard/summary`   Authenticated     Task counts by
                                                                 status

  GET               `/api/dashboard/tasks`     Authenticated     Filtered
                                                                 dashboard task
                                                                 list
  --------------------------------------------------------------------------------

### Dashboard Scope

``` text
Admin   → system-wide task information
Manager → own team's task information
User    → own assigned task information
```

### Summary Example

``` json
{
  "total": 12,
  "todo": 5,
  "inProgress": 4,
  "done": 3
}
```

------------------------------------------------------------------------

# 11. Request/Response Examples

## 11.1 Login Request

``` json
{
  "email": "user@example.com",
  "password": "Password123!"
}
```

## 11.2 Create Task Request

``` json
{
  "title": "Implement authentication",
  "description": "Implement JWT authentication and authorization.",
  "priority": "High",
  "deadline": "2026-09-30",
  "teamId": "team-guid",
  "assignedToId": "user-guid"
}
```

## 11.3 Update Status

``` json
{
  "status": "In Progress"
}
```

## 11.4 Add Comment

``` json
{
  "content": "Authentication implementation is in progress."
}
```

------------------------------------------------------------------------

# 12. Validation Rules

Validation should be implemented at the application/API boundary.

## Registration

-   Name is required.
-   Email is required.
-   Email must have a valid format.
-   Email must be unique.
-   Password is required.
-   Password must satisfy the configured password policy.
-   Public registration cannot select privileged roles.

## Team

-   Team name is required.
-   Team name must satisfy the configured length constraints.
-   Manager must exist.
-   Manager must have the `Manager` role.

## Task

-   Title is required.
-   Title must not exceed the configured maximum length.
-   Status must be one of the supported statuses.
-   Priority must be one of `Low`, `Medium`, or `High`.
-   Assignee must exist.
-   Team must exist.
-   Assignee must belong to the selected team.

## Comment

-   Content is required.
-   Content must not be empty/whitespace.
-   Content must satisfy the configured length limit.
-   User must have access to the task.

------------------------------------------------------------------------

# 13. Error Handling

Use a consistent API error format based on ASP.NET Core
`ProblemDetails`.

Recommended status codes:

  Status   Meaning
  -------- ------------------------------------------------------------
  400      Validation / malformed request
  401      Missing, invalid, or expired authentication
  403      Authenticated but not authorized
  404      Resource not found
  409      Business conflict, such as duplicate email/team constraint
  500      Unexpected server error

Example:

``` json
{
  "title": "Validation failed",
  "status": 400,
  "errors": {
    "email": [
      "Email is required."
    ]
  }
}
```

A global exception-handling middleware should prevent internal exception
details from being exposed to clients.

------------------------------------------------------------------------

# 14. Notification Flow

## Assignment Flow

``` text
Admin/Manager
     ↓
Assign Task
     ↓
Validate permission
     ↓
Save Task
     ↓
Create Assignment Notification
     ↓
Notify Assignee
```

## Status Update Flow

``` text
Assignee/Admin/Manager
          ↓
     Update Status
          ↓
   Validate permission
          ↓
       Save Task
          ↓
Create StatusUpdate Notification
          ↓
Notify configured recipient
```

------------------------------------------------------------------------

# 15. Pagination

Task and user listing endpoints should support pagination.

Example:

``` http
GET /api/tasks?page=1&pageSize=20
```

Example response:

``` json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 100,
  "totalPages": 5
}
```

This prevents large datasets from being returned in a single request.

------------------------------------------------------------------------

# 16. EF Core Design

Use:

``` text
Entity Framework Core
        ↓
Npgsql Provider
        ↓
PostgreSQL
```

Recommended practices:

-   Use migrations.
-   Configure relationships explicitly.
-   Configure indexes and unique constraints.
-   Use `AsNoTracking()` for read-only queries where appropriate.
-   Avoid unnecessary lazy loading.
-   Keep database access inside Infrastructure.
-   Use async EF Core operations.
-   Use projections for read-heavy endpoints.
-   Apply pagination at the database level.

Important indexes should include:

``` text
Users.Email
Users.TeamId
Teams.ManagerId
Tasks.TeamId
Tasks.AssignedToId
Tasks.AssignedById
Tasks.Status
Tasks.Priority
Tasks.Deadline
Comments.TaskId
Notifications.UserId
Notifications.IsRead
RefreshTokens.UserId
```

------------------------------------------------------------------------

# 17. Security Requirements

The API should implement:

-   JWT authentication.
-   ASP.NET Core Identity password hashing.
-   Role-based authorization.
-   Resource/ownership authorization.
-   Access-token expiration.
-   Refresh-token expiration and revocation.
-   HTTPS in deployed environments.
-   Input validation.
-   Safe error responses.
-   No passwords or raw tokens in logs.
-   Configuration/secrets through environment variables or secure
    deployment configuration.
-   CORS restricted to the frontend application in production.

------------------------------------------------------------------------

# 18. Testing Strategy

Testing contributes directly to the assessment evaluation.

## Unit Tests

Test:

-   Task creation rules.
-   Task assignment rules.
-   Manager team-scope rules.
-   User ownership rules.
-   Status transition/business rules.
-   Validation.
-   Notification creation.
-   Dashboard calculations.

## Integration Tests

Test complete API workflows:

``` text
Register
   ↓
Login
   ↓
Create Team
   ↓
Assign Manager
   ↓
Add Team Member
   ↓
Create Task
   ↓
Assign Task
   ↓
Update Status
   ↓
Create Notification
   ↓
Add Comment
   ↓
Read Notification
```

Also test negative authorization cases:

``` text
User attempts to update another user's task → 403
Manager accesses another team's task → 403
User attempts to create a task → 403
User attempts to change role → 403
```

------------------------------------------------------------------------

# 19. Swagger / OpenAPI

Swagger should document:

-   Authentication endpoints.
-   Request/response DTOs.
-   Validation errors.
-   HTTP status codes.
-   Authorization requirements.
-   JWT Bearer authentication.

The Swagger UI should allow authenticated API testing using the Bearer
token.

------------------------------------------------------------------------

# 20. Docker

The full application should support a multi-container setup.

``` text
┌───────────────────┐
│ React Frontend    │
└─────────┬─────────┘
          │
          ▼
┌───────────────────┐
│ ASP.NET Core API  │
└─────────┬─────────┘
          │
          ▼
┌───────────────────┐
│ PostgreSQL        │
└───────────────────┘
```

Docker Compose should provide:

``` text
frontend
backend
postgres
```

Environment-specific configuration should be supplied through
environment variables.

------------------------------------------------------------------------

# 21. CI/CD

GitHub Actions should run at minimum:

``` text
Checkout
   ↓
Restore
   ↓
Build
   ↓
Unit Tests
   ↓
Integration Tests
   ↓
Docker Build
```

Deployment can be connected after the CI pipeline succeeds.

------------------------------------------------------------------------

# 22. Frontend Requirements

React frontend should provide:

## Authentication

-   Login page.
-   Registration page.
-   Token/session handling.
-   Logout.
-   Protected routes.

## Admin UI

-   User management.
-   Team management.
-   Task management.
-   Dashboard.

## Manager UI

-   Team members.
-   Team tasks.
-   Create/assign tasks.
-   Task status overview.

## User UI

-   Assigned tasks.
-   Task details.
-   Status update.
-   Comments.
-   Notifications.

## Dashboard

Display:

-   Total tasks.
-   To Do.
-   In Progress.
-   Done.
-   Priority filtering.
-   Deadline filtering.
-   Status filtering.

The UI should be responsive and provide useful loading, validation,
empty-state, and API-error feedback.

------------------------------------------------------------------------

# 23. Deliverables Checklist

-   [ ] GitHub repository.
-   [ ] Backend source code.
-   [ ] Frontend source code.
-   [ ] Database migrations.
-   [ ] README.md.
-   [ ] Setup instructions.
-   [ ] Environment variable documentation.
-   [ ] Sample credentials.
-   [ ] Swagger/OpenAPI documentation.
-   [ ] Postman collection if used.
-   [ ] Unit tests.
-   [ ] Integration tests.
-   [ ] Dockerfile(s).
-   [ ] Docker Compose.
-   [ ] GitHub Actions CI/CD.
-   [ ] Optional deployed application.
-   [ ] 5--8 minute video walkthrough.

------------------------------------------------------------------------

# 24. README Requirements

The README should contain:

``` text
Project Overview
Features
Technology Stack
Architecture
Prerequisites
Environment Variables
Database Setup
Migration Commands
Backend Setup
Frontend Setup
Docker Setup
API Documentation
Sample Credentials
Testing
Deployment
Project Screenshots
Demo Link
Video Walkthrough
```

Do not commit real secrets, passwords, JWT signing keys, database
credentials, or API keys.

------------------------------------------------------------------------

# 25. Suggested Implementation Order

Implement the project in this order:

``` text
1. Solution + Clean Architecture
        ↓
2. Domain Entities + Enums
        ↓
3. PostgreSQL + EF Core
        ↓
4. ASP.NET Core Identity
        ↓
5. JWT + Refresh Tokens
        ↓
6. Authentication & Authorization
        ↓
7. Users
        ↓
8. Teams
        ↓
9. Tasks
        ↓
10. Comments
        ↓
11. Notifications
        ↓
12. Dashboard
        ↓
13. Validation + ProblemDetails
        ↓
14. Swagger
        ↓
15. Unit Tests
        ↓
16. Integration Tests
        ↓
17. React Frontend
        ↓
18. Docker Compose
        ↓
19. GitHub Actions
        ↓
20. Deployment
        ↓
21. README + Video Walkthrough
```

------------------------------------------------------------------------

# 26. Final Scope

The implementation must satisfy the core requirements:

``` text
Authentication
Authorization
User Management
Team Management
Task Management
Task Assignment
Task Status Tracking
Comments
Notifications
Dashboard
Filtering
Validation
Error Handling
```

Advanced/bonus implementation:

``` text
Docker
Unit Tests
Integration Tests
Swagger
CI/CD
Deployment
Responsive UI
```

The design intentionally keeps team membership simple with one team per
user. If multi-team membership becomes a future requirement, the
`TeamMembers` join-table model can replace `ApplicationUser.TeamId`.

------------------------------------------------------------------------

# 27. Assessment Alignment

  -----------------------------------------------------------------------
  Assessment Area                     Implementation Focus
  ----------------------------------- -----------------------------------
  Backend API Design & Auth           REST API, Identity, JWT, refresh
                                      tokens, validation, ProblemDetails

  Database Design & Relations         PostgreSQL, EF Core, normalized
                                      relationships, indexes

  Frontend UI & UX                    React, Axios, responsive UI,
                                      loading/error states

  Role-Based Access & Logic           Admin/Manager/User + ownership/team
                                      scope

  Code Quality & Modularity           Clean Architecture + feature-based
                                      Application layer

  Testing                             Unit + integration tests

  DevOps                              Docker, Docker Compose, GitHub
                                      Actions, deployment

  Documentation & Presentation        README, Swagger, sample
                                      credentials, walkthrough video
  -----------------------------------------------------------------------

------------------------------------------------------------------------

# 28. Definition of Done

The project is considered complete when:

-   [ ] Admin, Manager, and User authentication works.
-   [ ] JWT access tokens work correctly.
-   [ ] Refresh tokens can be rotated/revoked.
-   [ ] Role-based authorization is enforced.
-   [ ] Resource-level authorization is enforced.
-   [ ] Teams can be managed according to role.
-   [ ] Managers can manage only their own teams.
-   [ ] Tasks can be created and assigned.
-   [ ] Users can update only their permitted task status.
-   [ ] Comments work with proper ownership checks.
-   [ ] Assignment notifications are generated.
-   [ ] Status-update notifications are generated.
-   [ ] Dashboard summaries work by role scope.
-   [ ] Task filtering works.
-   [ ] Validation is implemented.
-   [ ] Consistent API error handling is implemented.
-   [ ] Swagger documentation is available.
-   [ ] Unit tests pass.
-   [ ] Integration tests pass.
-   [ ] Docker Compose starts the application stack.
-   [ ] CI pipeline passes.
-   [ ] README is complete.
-   [ ] Video walkthrough is prepared.
