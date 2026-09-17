# AGENTS.md

# Team Task Management System
## AI Coding Agent Instructions

This document defines the architecture, coding standards, security rules,
development workflow, and implementation constraints for the Team Task Management System.

The functional requirements and detailed database/API design are defined in:
`docs/task-management-db-api-design.md`

The AI coding agent MUST read and follow both this file and the project specification
before implementing any feature.

---

# 1. PROJECT OVERVIEW

The project is a role-based Team Task Management System.

The system allows organizations to:
- Manage users
- Manage teams
- Assign tasks
- Track task progress
- Add comments to tasks
- Generate notifications
- View dashboards
- Filter and paginate task data
- Enforce role-based and resource-based authorization

Roles:
- Admin
- Manager
- User

Do not silently change requirements from the project specification.

---

# 2. TECHNOLOGY STACK

## Backend
- .NET 10
- ASP.NET Core Web API
- C#
- Entity Framework Core
- PostgreSQL
- Npgsql
- ASP.NET Core Identity
- JWT Bearer Authentication
- Refresh Tokens
- FluentValidation
- Serilog
- Swagger / OpenAPI

## Testing
- xUnit
- ASP.NET Core Integration Testing
- WebApplicationFactory where appropriate

## Frontend
- React
- Axios
- React Router
- Responsive UI

## DevOps
- Docker
- Docker Compose
- GitHub Actions

---

# 3. ARCHITECTURE

Use Clean Architecture.

The repository is organized into separate backend and frontend areas.

Expected repository structure:

```text
TaskManagementSystem/

├── backend/
│   ├── TaskManagement.sln
│   │
│   ├── src/
│   │   ├── TaskManagement.API/
│   │   ├── TaskManagement.Application/
│   │   ├── TaskManagement.Domain/
│   │   └── TaskManagement.Infrastructure/
│   │
│   ├── tests/
│   │   ├── TaskManagement.UnitTests/
│   │   └── TaskManagement.IntegrationTests/
│   │
│   ├── Directory.Build.props
│   ├── Directory.Packages.props
│   └── .editorconfig
│
├── frontend/
│   └── React application
│
├── docs/
│   └── task-management-db-api-design.md
│
├── AGENTS.md
├── .gitignore
└── README.md

Dependency direction:

```text
API
 ↓
Application
 ↓
Domain

Infrastructure
 ↓
Application + Domain
```

Rules:
- Domain must not depend on Infrastructure.
- Domain must not depend on API.
- Domain must not depend on EF Core.
- Application must not depend on API.
- Application must not depend on Infrastructure implementations.
- Infrastructure implements interfaces defined by Application where appropriate.
- API handles presentation/HTTP concerns.
- Business logic must not be placed inside controllers.
- Controllers must remain thin.

Do not introduce another architecture unless explicitly requested.

---

# 4. PROJECT SPECIFICATION

Before implementing any feature:
1. Read `AGENTS.md`.
2. Read the relevant section of `docs/task-management-db-api-design.md`.
3. Inspect the existing implementation.
4. Understand existing dependencies.
5. Implement only the requested phase.

The specification is the source of truth for:
- Entities
- Relationships
- Roles
- API routes
- Authorization rules
- DTOs
- Notifications
- Dashboard
- Database requirements
- Deliverables

Do not invent major requirements.
Do not silently modify the database design.

---

# 5. DEVELOPMENT STRATEGY

The project MUST be implemented phase by phase.

Do NOT implement the entire project in one operation.

Expected order:

```text
PHASE 0  Project understanding / planning
   ↓
PHASE 1  Domain Layer
   ↓
PHASE 2  EF Core + PostgreSQL
   ↓
PHASE 3  Identity + JWT + Refresh Tokens
   ↓
PHASE 4  User Management
   ↓
PHASE 5  Team Management
   ↓
PHASE 6  Task Management
   ↓
PHASE 7  Comments
   ↓
PHASE 8  Notifications
   ↓
PHASE 9  Dashboard
   ↓
PHASE 10 Backend Audit
   ↓
PHASE 11 React Frontend
   ↓
PHASE 12 Docker
   ↓
PHASE 13 CI/CD
   ↓
PHASE 14 Security + QA Audit
   ↓
PHASE 15 README + Submission Preparation
```

When asked to implement one phase:
- Implement only that phase.
- Do not automatically continue to the next phase.
- Build the project.
- Run relevant tests.
- Fix errors.
- Report the result.
- Stop and wait for the next instruction.

---

# 6. PHASE ISOLATION

If the current request is PHASE 1: Domain Layer, do NOT implement:
- EF Core
- PostgreSQL
- Identity
- JWT
- Controllers
- React
- Docker
- CI/CD

If the current request is PHASE 3: Authentication, do NOT silently implement:
- Teams
- Tasks
- Comments
- Dashboard
- React frontend

Only implement the requested phase.

---

# 7. DOMAIN LAYER

The Domain layer must remain independent from infrastructure.

Core entities:
- ApplicationUser
- Team
- TaskItem
- Comment
- Notification
- RefreshToken

Core enums:
- TaskStatus
- TaskPriority
- NotificationType

Use Guid for entity IDs.

Use a shared BaseEntity where appropriate for:
- Id
- CreatedAt
- UpdatedAt

Use `TaskItem` instead of `Task` to avoid confusion with
`System.Threading.Tasks.Task`.

Domain entities should not contain EF Core-specific dependencies.

Avoid:
- EF Core attributes
- DbContext references
- Infrastructure services
- HTTP concerns
- JWT concerns

inside Domain.

---

# 8. APPLICATIONUSER

Use ASP.NET Core Identity:

```csharp
ApplicationUser : IdentityUser<Guid>
```

ApplicationUser should contain project-specific user information required by the specification.

Roles:
- Admin
- Manager
- User

Do not create custom password hashing when ASP.NET Core Identity provides the required functionality.

---

# 9. DATABASE

Database:
- PostgreSQL

ORM:
- Entity Framework Core

Provider:
- Npgsql

Use Fluent API configurations.

Expected Infrastructure structure:

```text
TaskManagement.Infrastructure/

├── Persistence/
│   ├── ApplicationDbContext.cs
│   ├── Configurations/
│   │   ├── TeamConfiguration.cs
│   │   ├── TaskItemConfiguration.cs
│   │   ├── CommentConfiguration.cs
│   │   ├── NotificationConfiguration.cs
│   │   └── RefreshTokenConfiguration.cs
│   └── Migrations/
```

Configure explicitly where appropriate:
- Primary keys
- Foreign keys
- Required properties
- Optional properties
- Maximum lengths
- Relationships
- Delete behaviors
- Indexes
- Unique constraints

---

# 10. DATABASE RELATIONSHIPS

Follow the project specification.

Expected relationships:

```text
Team
 ├── Users
 └── Tasks

User
 ├── Assigned Tasks
 ├── Assigned Tasks By User
 ├── Comments
 └── Notifications

TaskItem
 └── Comments
```

Use appropriate foreign keys.

Avoid accidental cascade delete behavior that could delete large amounts of related business data.

Delete behavior must be explicitly considered.

---

# 11. DATABASE INDEXING

Use indexes for frequently queried fields where appropriate.

Potential examples:
- User.Email
- TaskItem.TeamId
- TaskItem.AssignedToId
- TaskItem.AssignedById
- TaskItem.Status
- TaskItem.Priority
- TaskItem.Deadline
- Comment.TaskId
- Notification.UserId
- RefreshToken.UserId

Do not create indexes blindly. Indexes should support actual query patterns.

---

# 12. MIGRATIONS

Use EF Core migrations.

Typical commands:

```bash
dotnet ef migrations add InitialCreate   --project src/TaskManagement.Infrastructure   --startup-project src/TaskManagement.API
```

```bash
dotnet ef database update   --project src/TaskManagement.Infrastructure   --startup-project src/TaskManagement.API
```

Do not modify migrations manually unless necessary.
Do not delete existing migrations just to solve an unrelated problem.

---

# 13. AUTHENTICATION

Authentication must use:
- ASP.NET Core Identity
- JWT access tokens
- Refresh tokens

Required operations:

```text
POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
```

Registration:
- Validate input.
- Create user through Identity.
- Hash password through Identity.
- Default public registration role must be `User`.
- Do not allow public users to register themselves as Admin or Manager.

Login:
- Validate credentials.
- Generate access token.
- Generate refresh token.
- Persist refresh token securely.
- Return appropriate authentication response.

---

# 14. JWT RULES

JWT access tokens must contain only necessary claims.

Appropriate claims may include:
- User ID
- Email
- Role

Do not place sensitive information inside JWT claims.

JWT configuration must come from configuration/environment variables.

Never hardcode JWT secrets.
Never commit JWT secrets to Git.
Never log JWT access tokens.

Access tokens should be short-lived.

---

# 15. REFRESH TOKEN RULES

Refresh tokens must:
- Be cryptographically secure.
- Have expiration.
- Be stored securely.
- Be revocable.
- Be associated with the correct user.
- Not be logged.
- Not be stored as plaintext where avoidable.

Prefer storing a secure hash of the refresh token rather than the raw token.

Refresh flow must verify:
```text
Token exists
+
Token belongs to user
+
Token is not expired
+
Token is not revoked
```

Logout should revoke the relevant refresh token/session.

Do not allow reuse of revoked/expired refresh tokens.

---

# 16. AUTHORIZATION

Authorization must combine:
- Role-based authorization
- Resource/ownership-based authorization

Do not rely only on `[Authorize(Roles = "Manager")]` when resource ownership also matters.

A Manager can access manager-protected endpoints, but that does NOT mean the
Manager can access every team. The Manager must also be authorized for the requested resource.

---

# 17. ADMIN ROLE

Admin has broad system-level permissions.

Admin can:
- Manage users
- Manage roles
- Manage teams
- Manage team membership
- Manage tasks
- View system-wide permitted data
- Access administrative dashboard information

Admin operations must still validate input and resource existence.

---

# 18. MANAGER ROLE

Manager can operate within their own team scope.

Manager can:
- View own team
- Manage team membership where allowed
- Create tasks
- Assign tasks to team members
- Update permitted tasks
- View relevant team task information
- View relevant dashboard information

Manager must NOT:
- Manage another Manager's team
- Assign tasks to users outside their team
- Modify arbitrary users outside permitted scope
- Bypass resource authorization

---

# 19. USER ROLE

User can:
- View assigned tasks
- Update permitted task information
- Update task status where allowed
- Add comments where allowed
- View own notifications
- View permitted dashboard information

User must NOT:
- Access arbitrary users
- Access arbitrary tasks
- Assign tasks to other users
- Change their own role
- Escalate privileges
- Access another user's private resources

---

# 20. CURRENT USER ID

Never trust the current user's identity from the request body.

Do not use a client-provided user ID to determine who is performing a security-sensitive action.

Derive identity from authenticated JWT claims.

For task creation:

```text
AssignedById = CurrentUserId
```

Do not allow the client to specify another user's ID as AssignedById.

---

# 21. SECURITY: IDOR

Prevent Insecure Direct Object Reference.

For every endpoint receiving a resource ID, such as:

```text
/api/tasks/{id}
/api/users/{id}
/api/teams/{id}
/api/comments/{id}
```

verify that the current user has permission to access that specific resource.

Never assume a Manager can access every Team.
Always verify ownership/scope.

---

# 22. PRIVILEGE ESCALATION

Prevent:

### Vertical privilege escalation
Example:
```text
User → Admin
```

A normal user must not be able to change their own role.

### Horizontal privilege escalation
Example:
```text
User A → User B's task
```

A user must not access another user's resources simply by changing an ID.

---

# 23. DTO RULES

Never expose EF Core entities directly from controllers.

Use DTOs.

Examples:
- RegisterRequest
- LoginRequest
- RefreshTokenRequest
- UserResponse
- UpdateUserRoleRequest
- CreateTeamRequest
- UpdateTeamRequest
- TeamResponse
- CreateTaskRequest
- UpdateTaskRequest
- TaskResponse
- UpdateTaskStatusRequest
- AssignTaskRequest
- CreateCommentRequest
- CommentResponse
- NotificationResponse

Do not expose:
- PasswordHash
- RefreshTokenHash
- Internal security fields
- Unnecessary persistence fields

---

# 24. OVER-POSTING PROTECTION

Never bind entire entities directly from client requests.

Do not allow clients to modify protected properties such as:

```text
Id
CreatedAt
UpdatedAt
AssignedById
PasswordHash
Role
Security fields
```

unless the specific endpoint explicitly allows the operation.

Protected fields should be set server-side.

---

# 25. VALIDATION

Use FluentValidation where appropriate.

Validate:
- Required fields
- String lengths
- Email format
- Enum values
- Deadline
- Team membership
- Task assignment
- Pagination
- Sorting
- Filters

Backend validation is mandatory.

Frontend validation is not a security mechanism.

---

# 26. ERROR HANDLING

Use consistent API error responses.

Prefer ASP.NET Core `ProblemDetails`.

Do not return raw exceptions.

Do not expose:
- Stack traces
- SQL errors
- Connection strings
- JWT secrets
- Internal implementation details

Use appropriate HTTP status codes:

```text
200 OK
201 Created
204 No Content
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
422 Unprocessable Entity
500 Internal Server Error
```

---

# 27. CONTROLLERS

Controllers must be thin.

Preferred flow:

```text
HTTP Request
     ↓
Controller
     ↓
Application Service / Use Case
     ↓
Domain / Persistence abstraction
     ↓
Database
```

Avoid large business logic blocks inside controllers.

Controllers should primarily:
- Receive requests
- Bind DTOs
- Call application layer
- Return HTTP responses

Business rules belong outside controllers.

---

# 28. APPLICATION LAYER

Application layer should contain:
- Use cases
- Application services
- DTOs
- Interfaces
- Validators
- Authorization-related application rules where appropriate

Keep use cases focused.
Avoid huge service classes.
Prefer small, cohesive services.

---

# 29. DOMAIN BUSINESS RULES

Important rules include:
- A task assignee must belong to the task's team.
- A Manager can only manage their own team.
- A User cannot change their own role.
- A task's AssignedById comes from the authenticated user.
- A User can only access permitted tasks.

Implement rules in an appropriate layer.
Do not scatter the same authorization/business rule across many controllers.

---

# 30. ASYNC PROGRAMMING

Use async APIs for:
- EF Core operations
- Identity operations
- HTTP calls
- External services
- File operations

Prefer:
```csharp
async
await
```

Avoid unnecessary synchronous database operations.

Use cancellation tokens where appropriate.

---

# 31. EF CORE QUERY PERFORMANCE

Queries should execute efficiently at the database level.

Prefer:

```text
Database
    ↓
Filter
    ↓
Sort
    ↓
Project
    ↓
Paginate
    ↓
Return
```

Avoid loading entire tables into memory before filtering/sorting/pagination.

Watch for:
- N+1 queries
- Excessive Include()
- Loading unnecessary columns
- Large result sets
- Missing indexes
- Unbounded queries

Use projection to DTOs when appropriate.

---

# 32. PAGINATION

List endpoints should support pagination where appropriate.

Common parameters:
```text
page
pageSize
sortBy
sortDirection
```

Set a reasonable maximum `pageSize`.
Never allow unlimited records.
Return pagination metadata where appropriate.

---

# 33. TASK MANAGEMENT

Task entity:

```text
Id
Title
Description
Status
Priority
Deadline
TeamId
AssignedToId
AssignedById
CreatedAt
UpdatedAt
```

Task statuses:

```text
ToDo
InProgress
Done
```

Task operations:

```text
POST   /api/tasks
GET    /api/tasks
GET    /api/tasks/{id}
PATCH  /api/tasks/{id}
PATCH  /api/tasks/{id}/status
PATCH  /api/tasks/{id}/assign
DELETE /api/tasks/{id}
```

Task list should support relevant filters:
```text
status
priority
deadline
page
pageSize
sortBy
sortDirection
```

---

# 34. TASK ASSIGNMENT

When assigning a task:
1. Verify task exists.
2. Verify assignee exists.
3. Verify assignee belongs to the relevant team.
4. Verify current user has permission to assign.
5. Derive AssignedById from current authenticated user.
6. Persist the change.
7. Trigger notification where required.

Never trust AssignedById from the client.

---

# 35. TEAM MANAGEMENT

Team operations:

```text
POST   /api/teams
GET    /api/teams
GET    /api/teams/{id}
PATCH  /api/teams/{id}
POST   /api/teams/{id}/members
DELETE /api/teams/{id}/members/{userId}
```

Rules:
- Team must have a valid name.
- Manager must exist where required.
- Manager must have Manager role.
- Manager cannot manage another Manager's team.
- User membership must be validated.
- Current design assumes a user belongs to one team.

Do not silently introduce multi-team membership.
If multi-team support is required later, use a proper join entity/table.

---

# 36. USER MANAGEMENT

Expected operations:

```text
GET   /api/users/me
GET   /api/users
GET   /api/users/{id}
PATCH /api/users/{id}/role
PATCH /api/users/{id}/team
```

Rules:
- Admin has broad access.
- Manager access must be scoped appropriately.
- User can access their own permitted profile information.
- User cannot change their own role.
- User cannot assign themselves to unauthorized teams.
- Role changes must be server-authorized.

---

# 37. COMMENTS

Comment entity:

```text
Id
TaskId
UserId
Content
CreatedAt
```

Operations:

```text
POST   /api/tasks/{taskId}/comments
GET    /api/tasks/{taskId}/comments
DELETE /api/comments/{id}
```

Rules:
- Verify access to the task before accessing comments.
- Verify comment ownership where deletion is restricted.
- Admin may have broader deletion permissions according to specification.
- Validate comment content.
- Never allow arbitrary users to manipulate comments through task IDs.

---

# 38. NOTIFICATIONS

Notification entity:

```text
Id
UserId
TaskId
Type
Message
IsRead
CreatedAt
```

Notification types:
- Assignment
- StatusUpdate

Generate notifications for relevant events:
- Task assigned
- Task reassigned
- Task status updated

Users can only access their own notifications.

Operations:

```text
GET   /api/notifications
PATCH /api/notifications/{id}/read
PATCH /api/notifications/read-all
```

Mark-all operations must be scoped to the current authenticated user.

Do not create duplicate notifications unnecessarily.

Use a notification service abstraction so email or other providers can be added later.

For the assessment, a database/mock notification implementation is acceptable
where it satisfies the specification.

---

# 39. DASHBOARD

Dashboard data must respect authorization scope.

Admin:
- System-wide permitted information

Manager:
- Own team information

User:
- Own assigned task information

Task summary may include:

```text
Total
To Do
In Progress
Done
```

Dashboard queries must be efficient.
Prefer database aggregation.
Do not load every task into memory merely to calculate counts.

---

# 40. LOGGING

Use Serilog.

Logs should help with:
- Application diagnostics
- Errors
- Important business operations
- Troubleshooting

Never log:
- Passwords
- Password hashes
- JWT secrets
- Access tokens
- Refresh tokens
- Connection strings with credentials

Avoid excessive sensitive request logging.

---

# 41. CONFIGURATION

Use configuration/environment variables for:
- Database connection
- JWT settings
- JWT secret
- External service credentials

Never hardcode secrets.
Do not commit real credentials.

Use:
- appsettings.json
- appsettings.Development.json
- environment variables

appropriately.

Production secrets must be externalized.

---

# 42. CORS

Configure CORS explicitly.

Do not use unrestricted production configuration such as:

```text
AllowAnyOrigin()
AllowAnyMethod()
AllowAnyHeader()
```

unless there is a deliberate and documented reason.

Frontend origin should be configurable.

---

# 43. DEPENDENCY INJECTION

Use ASP.NET Core dependency injection.

Use appropriate service lifetimes:
- Scoped
- Transient
- Singleton

Avoid manually constructing services when they should be injected.

Do not create hidden service locators.

---

# 44. REPOSITORIES AND ABSTRACTIONS

Do not create abstractions only for the sake of abstraction.

Do not automatically create:

```text
IGenericRepository<T>
GenericRepository<T>
UnitOfWork
```

unless they provide real value in this architecture.

Use EF Core through appropriate application abstractions where suitable.

Keep persistence code maintainable and simple.

Avoid unnecessary complexity.

---

# 45. SWAGGER / OPENAPI

Swagger must document:
- Endpoints
- Request DTOs
- Response DTOs
- Authentication
- Validation errors
- Relevant authorization requirements

Configure JWT Bearer authentication in Swagger.

---

# 46. TESTING STRATEGY

Testing is required.

## Unit Tests
Test:
- Validators
- Application services
- Business rules
- Authorization-related logic where practical
- Task assignment rules
- Team rules

## Integration Tests
Test:
- Registration
- Login
- Invalid login
- JWT authentication
- Refresh token
- Logout
- Role authorization
- Resource authorization
- CRUD operations
- Database integration
- Important end-to-end flows

Security-related tests must include:
- Admin access
- Manager access
- User access
- 401 Unauthorized
- 403 Forbidden
- IDOR attempts
- Privilege escalation attempts
- Invalid input
- Unauthorized task assignment
- Cross-team access attempts

Do not consider security-sensitive functionality complete without tests.

---

# 47. FRONTEND

Frontend uses:
- React
- Axios
- React Router

Required features:
- Login
- Registration
- Logout
- Protected routes
- Role-based UI
- Dashboard
- Users
- Teams
- Tasks
- Task details
- Comments
- Notifications

Frontend must handle:
- Loading states
- Empty states
- Error states
- Form validation
- API errors
- Responsive design

Centralize API communication where practical.
Use Axios configuration/interceptors where appropriate.

---

# 48. FRONTEND SECURITY

Frontend role checks are only for UI behavior.

They are NOT security.

Backend authorization is always the final security boundary.

All sensitive operations must be authorized by the backend.

---

# 49. DOCKER

Docker should support:

```text
React Frontend
ASP.NET Core API
PostgreSQL
```

Expected files:
```text
Dockerfile
docker-compose.yml
.dockerignore
```

Use environment variables for configuration.
Use a PostgreSQL persistent volume.
Do not hardcode production credentials.

Verify:

```bash
docker compose build
docker compose up
```

and confirm the complete application works.

---

# 50. CI/CD

GitHub Actions should perform appropriate checks:

```text
Checkout
Restore dependencies
Build backend
Run backend tests
Build frontend
Run frontend tests where configured
Docker build
```

Pipeline must fail when important build/test steps fail.

Do not store secrets directly inside workflow files.
Use GitHub Secrets for real deployment credentials.

---

# 51. GIT WORKFLOW

Use meaningful commits:

```text
chore: initialize task management system
feat: implement domain model
feat: configure PostgreSQL and EF Core
feat: implement identity authentication
feat: implement user management
feat: implement team management
feat: implement task management
feat: implement comments and notifications
feat: implement dashboard
test: add unit and integration tests
feat: implement React frontend
chore: add Docker configuration
chore: add CI pipeline
docs: complete project documentation
```

Do not create one massive commit containing the entire project.

Commit after each stable phase.

Before committing:

```bash
dotnet build
dotnet test
git status
```

Review changes before commit.

---

# 52. EXISTING CODE SAFETY

Before modifying an existing file:
1. Read the file.
2. Understand its purpose.
3. Check references/usages.
4. Preserve existing working behavior.
5. Make the smallest appropriate change.

Do not rewrite the entire project to implement a small feature.

Do not delete working code without a clear reason.

---

# 53. NO UNNECESSARY REFACTORING

When implementing a requested feature, do not:
- Rename unrelated classes
- Move unrelated files
- Change architecture
- Replace working libraries
- Rewrite controllers unnecessarily
- Change database design
- Upgrade framework versions

unless explicitly requested or necessary to fix a real issue.

Keep changes focused.

---

# 54. NO GUESSING

If a requirement is unclear:
1. Read the specification.
2. Inspect existing code.
3. Determine whether the requirement is already established.
4. If still ambiguous, explain the ambiguity.
5. Make the smallest reasonable assumption only when appropriate.

Do not silently invent major business rules.
Do not silently modify relationships.

---

# 55. BUILD REQUIREMENT

After backend changes, run:

```bash
dotnet build
```

After relevant test changes, run:

```bash
dotnet test
```

For frontend changes, run the project's appropriate build/test commands.

Do not report a phase as complete if the project does not build.

If an issue remains:
- Clearly report the error.
- Explain the cause if known.
- Do not hide the failure.

---

# 56. PHASE COMPLETION REPORT

After every phase, report:

```text
Phase:
Status:

Implemented:
- ...

Files Created:
- ...

Files Modified:
- ...

Build:
PASS / FAIL

Tests:
PASS / FAIL / NOT APPLICABLE

Database:
PASS / FAIL / NOT APPLICABLE

Known Issues:
- ...

Assumptions:
- ...
```

Then STOP.

Do not continue to the next phase automatically.

---

# 57. CODE STYLE

Prefer:
- Meaningful names
- Small methods
- Focused classes
- SOLID principles where useful
- Dependency Injection
- Clear interfaces
- Explicit contracts
- Async APIs
- Consistent naming
- Nullable reference types
- Maintainable code

Avoid:
- God classes
- God controllers
- Huge methods
- Duplicate business logic
- Magic strings where constants/enums are appropriate
- Dead code
- Excessive comments for obvious code
- Unnecessary abstractions
- Premature optimization

---

# 58. PERFORMANCE

Performance should be considered without premature optimization.

Pay particular attention to:
- Database queries
- Pagination
- Indexes
- Projection
- N+1 queries
- Large API responses
- Repeated database calls
- Unnecessary serialization

Do not optimize blindly.
Measure or reason about the actual bottleneck.

---

# 59. SECURITY CHECKLIST

Before considering backend complete, verify:

```text
[ ] Passwords hashed through Identity
[ ] No plaintext passwords
[ ] JWT secret not hardcoded
[ ] Refresh tokens protected
[ ] Refresh tokens revocable
[ ] Access token expiration configured
[ ] Authentication required where appropriate
[ ] Role authorization implemented
[ ] Resource authorization implemented
[ ] IDOR protected
[ ] Privilege escalation prevented
[ ] User cannot change own role
[ ] Manager cannot access another team's resources
[ ] User cannot access another user's restricted tasks
[ ] Input validation implemented
[ ] DTOs used
[ ] Over-posting prevented
[ ] ProblemDetails/error handling implemented
[ ] Sensitive data not logged
[ ] CORS configured
[ ] Secrets externalized
[ ] Pagination implemented
[ ] Database indexes considered
[ ] N+1 queries checked
```

---

# 60. FINAL QA CHECKLIST

Before submission, verify the complete project against the specification.

## Backend

```text
[ ] Authentication
[ ] JWT
[ ] Refresh token
[ ] Logout
[ ] RBAC
[ ] Resource authorization
[ ] User management
[ ] Team management
[ ] Task management
[ ] Task assignment
[ ] Task status
[ ] Comments
[ ] Notifications
[ ] Dashboard
[ ] Validation
[ ] Error handling
[ ] Pagination
[ ] Filtering
[ ] Sorting
[ ] Swagger
[ ] Logging
```

## Database

```text
[ ] PostgreSQL
[ ] EF Core
[ ] Relationships
[ ] Foreign keys
[ ] Indexes
[ ] Unique constraints
[ ] Migrations
[ ] Delete behaviors
```

## Testing

```text
[ ] Unit tests
[ ] Integration tests
[ ] Authentication tests
[ ] Authorization tests
[ ] IDOR tests
[ ] Validation tests
[ ] Task tests
[ ] Team tests
```

## Frontend

```text
[ ] Authentication UI
[ ] Protected routes
[ ] Role-based UI
[ ] Dashboard
[ ] Users
[ ] Teams
[ ] Tasks
[ ] Comments
[ ] Notifications
[ ] Loading states
[ ] Error states
[ ] Responsive UI
```

## DevOps

```text
[ ] Dockerfile
[ ] Docker Compose
[ ] PostgreSQL container
[ ] Backend container
[ ] Frontend container
[ ] Environment configuration
[ ] GitHub Actions
```

## Documentation

```text
[ ] README
[ ] Setup instructions
[ ] Environment variables
[ ] Database setup
[ ] Migration instructions
[ ] API documentation
[ ] Authentication documentation
[ ] Role documentation
[ ] Testing instructions
[ ] Docker instructions
[ ] Deployment instructions
[ ] Screenshots
[ ] Demo/video information
```

---

# 61. README REQUIREMENTS

Final README should contain:

```text
Project Overview
Features
Technology Stack
Architecture
Project Structure
Prerequisites
Environment Variables
Database Setup
Migration Instructions
Running Backend
Running Frontend
API Documentation
Authentication
Authorization / Roles
Testing
Docker
CI/CD
Deployment
Screenshots
Demo Video
Sample Credentials
```

Never include real secrets in README.

Sample credentials must be safe development/demo credentials only.

---

# 62. ASSESSMENT ALIGNMENT

The implementation should remain aligned with the assessment requirements.

Important areas:

```text
Backend API + Authentication
Database
Frontend
RBAC
Code Quality
Testing
DevOps
Documentation
Presentation
```

Do not sacrifice authentication, authorization, database correctness, or
testing merely to add extra features.

Core requirements have priority over unnecessary bonus features.

---

# 63. AGENT BEHAVIOR

The AI coding agent must behave as a senior software engineer.

Before coding:

```text
Read
→ Understand
→ Inspect
→ Plan
→ Implement
```

After coding:

```text
Build
→ Test
→ Review
→ Report
→ Stop
```

The agent must not:
- Implement the entire project without instruction.
- Skip tests when tests are applicable.
- Ignore architecture rules.
- Ignore authorization rules.
- Hardcode secrets.
- Guess major requirements.
- Modify unrelated code.
- Continue to the next phase automatically.

---

# 64. IMPORTANT IMPLEMENTATION PRINCIPLE

The goal is not simply:

```text
"Make the application work."
```

The goal is:

```text
Make the application work
+
Keep the architecture clean
+
Keep authorization secure
+
Keep database design correct
+
Keep code maintainable
+
Write meaningful tests
+
Make the implementation explainable
```

Every important implementation decision should be understandable by the developer
maintaining this project.

---

# 65. FINAL RULE

When the developer asks for a specific phase, implement ONLY that phase.

Always follow this sequence:

```text
1. Read AGENTS.md
2. Read relevant project specification
3. Inspect existing code
4. Plan the requested change
5. Implement only the requested change
6. Build
7. Test
8. Fix issues
9. Review
10. Report
11. STOP
```

Never automatically start the next phase.

Wait for the developer's next instruction.
