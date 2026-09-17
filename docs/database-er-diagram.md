# Database Entity-Relationship (ER) Diagram

This document details the complete relational database design and schema for the **Team Task Management System**, built using **PostgreSQL 17** and **Entity Framework Core 10**.

---

## 1. Mermaid Entity-Relationship Diagram

```mermaid
erDiagram
    AspNetUsers ||--o{ AspNetUserRoles : "has roles"
    AspNetRoles ||--o{ AspNetUserRoles : "assigned to"
    
    Teams ||--o{ AspNetUsers : "has members (1:N)"
    AspNetUsers ||--o{ Teams : "manages (ManagerId)"
    
    Teams ||--o{ Tasks : "contains (1:N, Restrict)"
    AspNetUsers ||--o{ Tasks : "assigned to (AssignedToId)"
    AspNetUsers ||--o{ Tasks : "created by (AssignedById)"
    
    Tasks ||--o{ Comments : "has comments (1:N, Cascade)"
    AspNetUsers ||--o{ Comments : "authored by (UserId)"
    
    AspNetUsers ||--o{ Notifications : "receives (1:N)"
    Tasks ||--o{ Notifications : "related task (1:N, SetNull)"
    
    AspNetUsers ||--o{ RefreshTokens : "owns (1:N)"

    AspNetUsers {
        uuid Id PK
        varchar_100 Name
        varchar_256 Email
        varchar_256 NormalizedEmail
        varchar_256 UserName
        varchar_256 NormalizedUserName
        text PasswordHash
        uuid TeamId FK "Nullable"
        timestamp_tz CreatedAt
        timestamp_tz UpdatedAt
    }

    AspNetRoles {
        uuid Id PK
        varchar_256 Name
        varchar_256 NormalizedName
    }

    AspNetUserRoles {
        uuid UserId PK, FK
        uuid RoleId PK, FK
    }

    Teams {
        uuid Id PK
        varchar_100 Name "Unique"
        uuid ManagerId FK
        timestamp_tz CreatedAt
        timestamp_tz UpdatedAt
    }

    Tasks {
        uuid Id PK
        varchar_150 Title
        text Description "Nullable"
        varchar_50 Status "ToDo, InProgress, Done"
        varchar_50 Priority "Low, Medium, High"
        date Deadline
        uuid TeamId FK
        uuid AssignedToId FK
        uuid AssignedById FK
        timestamp_tz CreatedAt
        timestamp_tz UpdatedAt
    }

    Comments {
        uuid Id PK
        uuid TaskId FK
        uuid UserId FK
        text Content
        timestamp_tz CreatedAt
        timestamp_tz UpdatedAt
    }

    Notifications {
        uuid Id PK
        uuid UserId FK
        uuid TaskId FK "Nullable"
        varchar_50 Type "Assignment, StatusUpdate"
        varchar_255 Message
        boolean IsRead "Default: false"
        timestamp_tz CreatedAt
        timestamp_tz UpdatedAt
    }

    RefreshTokens {
        uuid Id PK
        uuid UserId FK
        varchar_500 TokenHash
        timestamp_tz ExpiresAt
        timestamp_tz CreatedAt
        timestamp_tz UpdatedAt
        timestamp_tz RevokedAt "Nullable"
        uuid ReplacedByTokenId "Nullable"
    }
```

---

## 2. Table Schemas & Foreign Key Constraints

### 2.1 `AspNetUsers`
- **Primary Key**: `Id` (`uuid`)
- **Foreign Keys**:
  - `TeamId` $\rightarrow$ `Teams.Id` (`ON DELETE SET NULL`)
- **Indexes**:
  - `NormalizedEmail` (`UNIQUE`)
  - `NormalizedUserName` (`UNIQUE`)
  - `TeamId` (`Index`)

### 2.2 `Teams`
- **Primary Key**: `Id` (`uuid`)
- **Foreign Keys**:
  - `ManagerId` $\rightarrow$ `AspNetUsers.Id`
- **Indexes**:
  - `Name` (`Case-Insensitive Unique`)
  - `ManagerId` (`Index`)

### 2.3 `Tasks` (`TaskItem`)
- **Primary Key**: `Id` (`uuid`)
- **Foreign Keys**:
  - `TeamId` $\rightarrow$ `Teams.Id` (`ON DELETE RESTRICT`)
  - `AssignedToId` $\rightarrow$ `AspNetUsers.Id`
  - `AssignedById` $\rightarrow$ `AspNetUsers.Id`
- **Indexes**:
  - `TeamId` (`Index`)
  - `AssignedToId` (`Index`)
  - `AssignedById` (`Index`)
  - `Status` (`Index`)
  - `Priority` (`Index`)
  - `Deadline` (`Index`)

### 2.4 `Comments`
- **Primary Key**: `Id` (`uuid`)
- **Foreign Keys**:
  - `TaskId` $\rightarrow$ `Tasks.Id` (`ON DELETE CASCADE`)
  - `UserId` $\rightarrow$ `AspNetUsers.Id`
- **Indexes**:
  - `TaskId` (`Index`)
  - `UserId` (`Index`)

### 2.5 `Notifications`
- **Primary Key**: `Id` (`uuid`)
- **Foreign Keys**:
  - `UserId` $\rightarrow$ `AspNetUsers.Id`
  - `TaskId` $\rightarrow$ `Tasks.Id` (`ON DELETE SET NULL`)
- **Indexes**:
  - `UserId` (`Index`)
  - `IsRead` (`Index`)
  - `TaskId` (`Index`)

### 2.6 `RefreshTokens`
- **Primary Key**: `Id` (`uuid`)
- **Foreign Keys**:
  - `UserId` $\rightarrow$ `AspNetUsers.Id`
- **Indexes**:
  - `UserId` (`Index`)
  - `TokenHash` (`Index`)

---

## 3. Referential Integrity & Delete Behaviors

1. **Teams $\rightarrow$ Tasks**: `DeleteBehavior.Restrict` prevents accidental deletion of teams with active tasks.
2. **Tasks $\rightarrow$ Comments**: `DeleteBehavior.Cascade` automatically cleans up associated discussion threads when a task is permanently removed.
3. **Tasks $\rightarrow$ Notifications**: `DeleteBehavior.SetNull` retains historical notification logs for users while nullifying the dead task reference.
4. **Refresh Tokens**: Cryptographic SHA-256 token hashes are stored with explicit expiration and revocation timestamps (`RevokedAt`).
