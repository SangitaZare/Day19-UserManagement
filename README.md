# Day 19 — User Management API

Two ASP.NET Core **REST APIs** over the same small domain (users), each built with a
different architecture. The point of the exercise is the contrast: the same feature set,
solved first with a **service layer**, then rebuilt with **CQRS**.

> Both projects are **backend only** — they serve JSON, not pages. There is no frontend
> here. Use Swagger UI, the included `.http` files, Postman, or curl to exercise them.

---

## The two projects

| | `UserManagement.Day19` | `UserManagement.Api` |
|---|---|---|
| **Architecture** | Layered / service | CQRS |
| **Flow** | Controller → `IUserService` → `DbContext` | Controller → MediatR → handler → `DbContext` |
| **Mapping** | AutoMapper profile | Hand-written projections |
| **Validation** | none | FluentValidation + pipeline behavior |
| **Logging** | Serilog (console + rolling file) | `LoggingBehavior<,>` in the MediatR pipeline |
| **Operations** | list, get, create | list, get, create, **update**, **delete** |
| **HTTPS port** | `7292` | `7227` |
| **HTTP port** | `5159` | `5213` |

Both target `net10.0` and both use **EF Core's In-Memory provider** — no connection
string, no migrations, nothing to install. Data lives in RAM and resets on every restart.

---

## 1. `UserManagement.Day19` — the layered version

The conventional approach. The controller is thin and delegates to a service; the service
owns the logic and the logging.

```
UserManagement.Day19/
├── Controllers/UsersController.cs   HTTP endpoints, delegates to the service
├── Services/
│   ├── IUserService.cs              Contract
│   └── UserService.cs               Logic + logging
├── Data/AppDbContext.cs             EF Core context
├── Models/User.cs                   Entity (Id, Name, Email, CreatedAt)
├── Dtos/
│   ├── CreateUserDto.cs             Request shape (Name, Email)
│   └── UserDto.cs                   Response shape
├── Mappings/UserMappingProfile.cs   AutoMapper rules
└── Program.cs                       DI, middleware, Serilog bootstrap
```

**Endpoints** — base route `/api/users`

| Method | Route | Description | Success | Failure |
|---|---|---|---|---|
| `GET` | `/api/users` | List all users | `200` | — |
| `GET` | `/api/users/{id}` | Get one user | `200` | `404` |
| `POST` | `/api/users` | Create a user | `201` | — |

**The mapping is doing real work.** `UserMappingProfile` isn't a passthrough — it reshapes
the entity on the way out and locks it down on the way in:

```csharp
// CreatedAt (DateTime) becomes CreatedOn, a preformatted string
CreateMap<User, UserDto>()
    .ForMember(d => d.CreatedOn, o => o.MapFrom(s => s.CreatedAt.ToString("yyyy-MM-dd HH:mm")));

// Id and CreatedAt are server-controlled — a client cannot spoof them
CreateMap<CreateUserDto, User>()
    .ForMember(d => d.Id, o => o.Ignore())
    .ForMember(d => d.CreatedAt, o => o.Ignore());
```

**Response:**

```json
{
  "id": 1,
  "name": "Sangita",
  "email": "sangita@example.com",
  "createdOn": "2026-10-27 11:24"
}
```

---

## 2. `UserManagement.Api` — the CQRS version

Same domain, restructured. Every operation is its own request object with its own handler,
grouped by feature rather than by technical layer. Reads (queries) and writes (commands)
are separated.

```
UserManagement.Api/
├── Controllers/UsersController.cs   Thin — only builds a request and sends it
├── Features/Users/
│   ├── Commands/                    Writes
│   │   ├── CreateUser/              Command + Handler + Validator
│   │   ├── UpdateUser/              Command + Handler + Validator
│   │   └── DeleteUser/              Command + Handler
│   ├── Queries/                     Reads
│   │   ├── GetAllUsersQuery/
│   │   └── GetUserById/
│   └── Dtos/UserDto.cs              record UserDto(int Id, string Name, string Email)
├── Behaviors/
│   ├── LoggingBehavior.cs           Logs every request in and out
│   └── ValidationBehavior.cs        Runs validators before the handler
├── Data/AppDbContext.cs
├── Models/User.cs
└── Program.cs
```

**Endpoints** — base route `/api/users`

| Method | Route | Description | Success | Failure |
|---|---|---|---|---|
| `GET` | `/api/users` | List all users | `200` | — |
| `GET` | `/api/users/{id}` | Get one user | `200` | `404` |
| `POST` | `/api/users` | Create a user | `201` | `400` |
| `PUT` | `/api/users/{id}` | Update a user | `204` | `400`, `404` |
| `DELETE` | `/api/users/{id}` | Delete a user | `204` | `404` |

### Pipeline behaviors

The interesting part. Both behaviors are registered once in `Program.cs` and then apply to
**every** command and query automatically — no handler has to remember to call them:

```csharp
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
```

`ValidationBehavior` collects every `IValidator<TRequest>` registered for the incoming
request, runs them in parallel, and throws if any rule fails — so handlers only ever run
against valid input. `LoggingBehavior` brackets each request with a log line on the way in
and out.

### Validation in practice

`CreateUserCommandValidator` requires a non-empty name of at most 100 characters and a
well-formed email. A bad request never reaches the handler — it is caught by the behavior
and turned into a structured `400`:

```json
{
  "title": "Validation failed",
  "errors": {
    "Name":  ["Name is required."],
    "Email": ["Email is not valid."]
  }
}
```

Errors are grouped by property name, so a client can attach each message to the right
field.

---

## Running them

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download)

```bash
git clone https://github.com/SangitaZare/Day19-UserManagement.git
cd Day19-UserManagement
```

Then pick one:

```bash
# The layered version  -> https://localhost:7292
cd UserManagement.Day19/UserManagement.Day19
dotnet run

# The CQRS version     -> https://localhost:7227
cd UserManagement.Api/UserManagement.Api
dotnet run
```

Swagger UI opens automatically at **`/swagger`** in Development. Each project also ships a
`.http` file with ready-made requests you can fire straight from Visual Studio or the
VS Code REST Client extension.

---

## Logging

`UserManagement.Day19` uses **Serilog**, writing to the console and to a daily rolling file
at `logs/log-YYYYMMDD.txt`. Three things worth noting in its setup:

1. A **bootstrap logger** is created before the host is built, so a crash *during startup*
   is still captured rather than vanishing silently.
2. `UseSerilogRequestLogging()` emits one line per HTTP request — method, path, status,
   elapsed ms — instead of the framework's several.
3. The service logs domain events: users fetched, user created, and a warning on a miss.

`UserManagement.Api` logs through `LoggingBehavior<,>` instead, which covers every request
uniformly without touching a single handler.

Log files are gitignored.

---

## Error handling

Both projects install a global exception handler at the top of the pipeline, so no
unhandled exception ever reaches the client as a stack trace.

`UserManagement.Day19` logs the failure with its method and path and returns:

```json
{ "title": "An unexpected error occurred." }
```

`UserManagement.Api` does the same, but first checks for a FluentValidation
`ValidationException` and converts it into the structured `400` shown above. Anything else
falls through to a generic `500`.

---

## What the comparison shows

**The layered version** is fewer files and easier to follow end to end. `UserService` is
one place to look. The cost is that the service grows with every feature, and read and
write concerns sit side by side in the same class.

**The CQRS version** is more files up front — each operation needs a request, a handler,
and often a validator. In exchange, each one is tiny and independently testable, features
are grouped in folders you can delete as a unit, and cross-cutting concerns like logging
and validation are solved *once* in a behavior rather than repeated in every method.

At this size the layered version is the pragmatic choice. The CQRS structure starts paying
for itself as the number of operations grows.

---

## Next steps

- [ ] Port validation back into `UserManagement.Day19` for a like-for-like comparison
- [ ] Add `PUT` and `DELETE` to `UserManagement.Day19`
- [ ] Swap the In-Memory provider for SQL Server + EF Core migrations
- [ ] Unit tests — `UserService` directly, and the CQRS handlers in isolation
- [ ] Pagination on the list endpoints

---

## Folder layout

Reorganised to match the rest of the workspace — one folder per project, with `backend/`
and `frontend/` inside. Each `.csproj` sits directly in its own folder (no repeated
nesting):

```
user-management/
├─ backend/
│  ├─ UserManagement.Api/      service-layer version → https://localhost:7227
│  │  ├─ UserManagement.Api.csproj
│  │  └─ UserManagement.Api.slnx
│  └─ UserManagement.Day19/    CQRS version          → https://localhost:7292
│     ├─ UserManagement.Day19.csproj
│     └─ UserManagement.Day19.slnx
└─ frontend/                   reserved; this exercise is backend-only by design
```

```bash
cd user-management/backend/UserManagement.Api && dotnet run
```

> Port `7292` collides with `file-handling`'s backend. Change one of them in
> `Properties/launchSettings.json` before running both.
