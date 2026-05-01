# API Design

## Technology choice: ASP.NET Core Minimal APIs

The backend is built on **.NET 10** using **ASP.NET Core Minimal APIs** rather than the traditional MVC controller model.

### What was evaluated

Three approaches were considered:

| Option | Description |
|---|---|
| **MVC Controllers** | The classical `[ApiController]` + `ControllerBase` model used in most ASP.NET tutorials and in the original Shelter prototype. |
| **Minimal APIs** | Route handlers registered directly in code, no controller base class, no attribute routing. Introduced in .NET 6 and matured significantly through .NET 8–10. |
| **FastEndpoints** | A third-party library that wraps Minimal APIs and enforces a strict one-class-per-endpoint pattern with built-in validation and mediator-like dispatch. |

### Why Minimal APIs

The primary reason was simply to learn it. Every prior ASP.NET project I had worked on used MVC controllers, so this project was a deliberate opportunity to get hands-on experience with the newer model before it becomes the default in greenfield .NET work. A thesis project is exactly the right setting for that kind of intentional unfamiliarity.

Beyond the learning motivation, Minimal APIs also have concrete technical advantages that made the choice easy to justify:

**Less framework ceremony.** Controllers carry a lot of implicit behaviour: model binding, filter pipelines, `IActionResult` return types, attribute routing. For a project where the architecture is being deliberately designed, that implicit surface area is a liability rather than a convenience. Minimal APIs do exactly what the code says and nothing else.

**`TypedResults` for accurate OpenAPI schemas.** Controller actions return `IActionResult`, which is opaque — the framework cannot statically determine the response shape. Minimal API endpoints can return `Results<T1, T2, ...>`, a compile-time union of the possible responses. The OpenAPI middleware reads the generic type arguments directly, so the generated schema is accurate without any additional `[ProducesResponseType]` annotations:

```csharp
private static async Task<Results<Ok<AuthResponse>, Conflict<ProblemDetails>>> HandleAsync(
    RegisterRequest request,
    RegisterHandler handler,
    CancellationToken cancellationToken)
```

**Native DI injection into route handlers.** ASP.NET Core resolves parameters of route handlers from the DI container automatically. Handlers, cancellation tokens, and `ClaimsPrincipal` are injected the same way constructor dependencies are — no `[FromServices]` attributes needed for scoped/transient services.

**Alignment with vertical slice architecture.** The endpoint registration is plain code, which makes it natural to group endpoint definitions by feature rather than by HTTP method or controller class.

**Native AOT compatibility (in principle).** This is the largest structural advantage of Minimal APIs over Controllers. MVC is built around runtime reflection — scanning assemblies for `[ApiController]`, dispatching to action methods, binding models, evaluating filters — which the AOT compiler cannot statically analyse and the trimmer cannot safely prune. Minimal APIs, by contrast, ship with a request-delegate source generator (`RequestDelegateGenerator`) that emits the parameter-binding code at compile time. Combined with `System.Text.Json`'s source generation and OpenAPI generation built on top of `Microsoft.AspNetCore.OpenApi`, an idiomatic Minimal API project can be published as a single self-contained native binary with:

- **Sub-100ms cold start** (no JIT warmup, no assembly loading).
- **A fraction of the memory footprint** of the equivalent JIT-compiled application — useful for containerised or serverless deployment.
- **Smaller deployment artefacts** thanks to aggressive trimming of unreachable code.
- **No JIT compiler in the deployed binary**, which reduces the runtime attack surface.

#### How (and why) this project breaks AOT

Despite all of the above, the Shelter API as planned is **not** AOT-publishable, and the decision was deliberate. The two dependencies that break AOT compatibility — and that no amount of refactoring at the application layer can rescue — are **Entity Framework Core** and **ASP.NET Core Identity**.

**EF Core.** EF Core's entire query pipeline is built on runtime reflection and dynamic IL emission. The provider inspects entity types to build the model, walks LINQ expression trees to translate them to SQL, and emits materialiser delegates at runtime to hydrate query results into POCOs. Every step of that pipeline is exactly what the AOT compiler and the IL trimmer cannot reason about statically. The .NET team has been working on this for several releases — .NET 8 introduced compiled models, .NET 9 added precompiled queries — but as of .NET 10 a non-trivial EF Core application still emits a wall of trim warnings and does not run cleanly under AOT. For a project whose data layer needs spatial queries, change tracking, and migrations, EF Core is the correct tool, and accepting the AOT incompatibility is the price of using it.

**ASP.NET Core Identity.** Identity is reflection-heavy across its entire surface: the user manager scans entity types for properties, the password hasher and token providers are wired up via a reflective options pipeline, and the role/claim infrastructure relies on generic type resolution at runtime. None of this has a source-generated equivalent. There is no published roadmap for making Identity AOT-compatible. For a project that needs role-based authorisation backed by a real user store, the alternatives are either rolling a bespoke auth system (a significant scope expansion that distracts from the actual research question) or accepting Identity's runtime requirements.

These two dependencies are the reason this project is not AOT-targeted. Everything else is downstream of them — once EF Core and Identity are in the dependency graph, an AOT build is impossible regardless of what the rest of the code looks like.

For completeness, the current code (before EF Core and Identity are wired) also has a handful of smaller AOT breaks that *would* be cheap to fix in isolation:

| Code | What breaks | Could be replaced with |
|---|---|---|
| `JwtSecurityTokenHandler` (in `JwtGenerator`) | The legacy `System.IdentityModel.Tokens.Jwt` namespace uses reflection to construct and inspect tokens. | `Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler`, the AOT-compatible successor. |
| `[FromForm] CreateShelterRequest` (complex type) | Form binding to a complex POCO requires reflection over its properties at request time. | `[AsParameters]` with primitive form fields. |
| `ConfigureHttpJsonOptions` without a `JsonSerializerContext` | Without a source-generated context, `System.Text.Json` falls back to reflection-based metadata. | A `[JsonSerializable(typeof(...))]`-annotated `JsonSerializerContext`. |
| `NetTopologySuite.IO.GeoJSON4STJ` | NTS relies on reflection internally and is not trim-safe. | No AOT-friendly equivalent; would require dropping GeoJSON or writing a custom converter. |

These are listed for honesty — but fixing them in isolation would not make the project AOT-publishable, because EF Core and Identity will reintroduce the reflection requirement at the persistence and authentication layers regardless.

**The reasoning is pragmatic.** Native AOT is a deployment optimisation, not a correctness requirement. EF Core and Identity are the right tools for the data and auth concerns of this project, and giving them up to chase sub-100ms cold-start times on a thesis project is the wrong tradeoff. The project is published as a standard JIT-compiled .NET application; this is fast enough for the workloads under consideration, and the architectural lessons of vertical slicing and Minimal APIs survive intact regardless of the deployment model.

---

## Project structure

The solution follows a strict four-layer layout with one-directional references:

```
Shelter.Domain        → entities, value objects, enums; zero external dependencies
Shelter.App           → use-case handlers, request/response DTOs
Shelter.Infrastructure → JWT, in-memory stores; implements App interfaces
Shelter.Api           → HTTP endpoints, OpenAPI config, Program.cs
```

`Shelter.Api` → `Shelter.Infrastructure` → `Shelter.App` → `Shelter.Domain`

One important naming note: `Shelter.App` sets `<RootNamespace>App</RootNamespace>` in its `.csproj`. All types in that project therefore live under `App.*` (e.g. `App.Features.Shelters.Create.CreateShelterHandler`), not `Shelter.App.*`. This keeps import statements short and signals clearly which layer a type belongs to.

---

## Vertical slice architecture: one handler per use case

Rather than organising code by technical role (a `ShelterService` with methods for create, update, delete, search), the application is organised by **feature slice**. Each use case gets its own folder containing everything it needs.

### Folder layout

```
Shelter.App/Features/<Aggregate>/<UseCase>/
    <UseCase>Request.cs     — plain C# class; no framework attributes
    <UseCase>Handler.cs     — the use case; dependencies injected via constructor
    <UseCase>Response.cs    — only if this response shape is not shared

Shelter.App/Features/<Aggregate>/Shared/
    <Aggregate>DetailResponse.cs   — promoted here when 2+ slices return the same shape

Shelter.Api/Features/<Aggregate>/<UseCase>/
    <UseCase>Endpoint.cs    — HTTP glue: parameter binding and handler call

Shelter.Api/Features/<Aggregate>/
    <Aggregate>Endpoints.cs — MapGroup("/api/<aggregate>") + calls into each slice
```

### Handler contract

A handler is a plain class with a single `HandleAsync` method. It knows nothing about HTTP:

```csharp
public sealed class CreateShelterHandler(ILogger<CreateShelterHandler> logger)
{
    public Task<ShelterDetailResponse> HandleAsync(
        CreateShelterRequest request,
        Guid ownerId,
        IReadOnlyList<FileUpload> pictures,
        CancellationToken cancellationToken)
    { ... }
}
```

- **No `HttpContext`, `ClaimsPrincipal`, or `IFormFile`** — those are HTTP concerns and stay in the endpoint layer.
- **Constructor-injected dependencies** — only the dependencies this specific use case needs.
- **Returns a DTO**, not a domain entity. The domain model is an internal implementation detail.
- `Task.FromResult` while infrastructure is mocked; flipped to `async`/`await` when real I/O is wired.

### Endpoint contract

The endpoint translates HTTP into the handler's language and back:

```csharp
public static class CreateShelterEndpoint
{
    public static RouteGroupBuilder MapCreateShelter(this RouteGroupBuilder group)
    {
        group.MapPost("/", HandleAsync)
            .WithName("CreateShelter")
            .WithSummary("Create a new shelter")
            .Accepts<CreateShelterRequest>("multipart/form-data")
            .Produces<ShelterDetailResponse>(StatusCodes.Status201Created)
            .RequireAuthorization(AppRoles.ShelterOwner)
            .DisableAntiforgery();

        return group;
    }

    private static async Task<Created<ShelterDetailResponse>> HandleAsync(
        [FromForm] CreateShelterRequest request,
        IFormFileCollection pictures,
        CreateShelterHandler handler,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var uploads = pictures
            .Select(p => new FileUpload(p.OpenReadStream(), p.ContentType, p.FileName))
            .ToList();

        var response = await handler.HandleAsync(request, user.GetUserId(), uploads, cancellationToken);

        return TypedResults.Created($"/api/shelters/{response.Id}", response);
    }
}
```

The endpoint's only responsibilities are: bind HTTP parameters, translate framework types to application types (e.g. `IFormFile` → `FileUpload`, `ClaimsPrincipal` → `Guid`), call the handler, and map the result to an HTTP response.

### Route group wiring

Each aggregate has a `<Aggregate>Endpoints.cs` that creates a route group and calls each slice's `Map*` extension:

```csharp
public static class ShelterEndpoints
{
    public static IEndpointRouteBuilder MapShelterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/shelters").WithTags("Shelters");
        group.MapCreateShelter();
        return app;
    }
}
```

`Program.cs` calls `app.MapShelterEndpoints()` and `app.MapAuthEndpoints()`. Adding a new endpoint is three steps: create the handler, create the endpoint file, add one `group.Map*()` call.

### Why handler-per-use-case rather than service-per-aggregate

This project is developed solo, but the architectural choice was made with collaborative development in mind — and directly informed by friction experienced at work.

In a large team, shared service classes become merge conflict hotspots. When multiple developers are working on different features for the same aggregate simultaneously, they all end up editing the same `ShelterService.cs` file. Even unrelated changes collide: one developer adds a method at the bottom while another renames a dependency near the top. The file is a coordination point that Git cannot automatically resolve, so someone always has to manually untangle the diff. The larger the team and the faster the pace, the worse this gets.

Vertical slices sidestep the problem by construction. Each use case lives in its own folder and its own files. Two developers building `CreateShelter` and `SearchShelters` at the same time touch completely separate parts of the repository. Neither branch is aware of the other until the moment of merge, and at that point there is typically nothing to resolve.

A `ShelterService` class also tends to grow unbounded over time. It starts with `CreateAsync` and accumulates `UpdateAsync`, `DeleteAsync`, `SearchAsync`, `ActivateAsync`, and so on. Each method is responsible for its own logic but the class is responsible for all of them, and it ends up carrying the union of all dependencies ever needed by any operation on that aggregate — a database context, a storage client, a geocoder, an event bus. A handler for `CreateShelter` only declares the dependencies it actually uses.

Other practical consequences:

- **Deleting a feature is `rm -rf` on one folder.** No shared service class to audit for other callers.
- **Cross-cutting behaviour** (logging, validation, timing) can be added as a decorator around `IHandler<TReq, TRes>` without touching individual handlers.
- **Reading a use case** means reading one file. There is no inherited state, no shared fields, no execution path that branches based on which method was called.
- **Testing** a handler means constructing one class with a handful of mocked collaborators, not a service with twelve constructor parameters of which eight are irrelevant to the operation being tested.

---

## Where shared logic lives

Vertical slicing eliminates shared *workflow* — the orchestration class that becomes a coordination point for every operation on an aggregate. It does not eliminate the need for *shared contracts*: every slice that operates on a Shelter has to agree on what a Shelter is and what operations are valid on it. That agreement has to live somewhere.

The temptation, when shared logic appears, is to introduce a `BaseShelterHandler` and inherit from it. This reintroduces the very problem the architecture was designed to escape. A base class accumulates methods over time, derived handlers inherit dependencies they don't actually use, the base becomes a coordination point that every change has to consider, and "delete a folder = delete a feature" stops being true.

The project uses a different layering, in roughly this order of preference:

### 1. Domain logic on the entity

The aggregate root owns its invariants. Mutating state goes through named methods, not through public setters. Concretely:

- Private setters on every property.
- A private parameterless constructor (used only by EF Core's reflection-based materialiser).
- A static `Create` factory on the App layer's behalf.
- Named mutator methods (`UpdateDetails`, `Relocate`, `AddPicture`, `Deactivate`/`Reactivate`) that validate inputs, mutate state, and bump `UpdatedAt`.
- Predicates for ownership and state checks (`shelter.OwnedBy(userId)`).

Crucially, these methods take primitive parameters and have **no constructor injection**. The entity remains dependency-free and is unit-testable without any mocks. `now` is passed in as a `DateTimeOffset` parameter rather than retrieved from an injected clock — pushing the time concern up to the handler.

This places shared logic in a category that's structurally different from a shared service class:

| Shared service (`ShelterService`) | Domain methods on entity (`Shelter`) |
|---|---|
| Aggregates infrastructure dependencies (DbContext, repos, etc.) in its constructor. | Has no dependencies. Constructor takes nothing. |
| Methods share mutable state (DbContext, caches, transaction scope). | Methods only mutate the entity itself. |
| Adding a method may break callers by changing shared state or behaviour. | Adding a method is purely additive — no other slice can be affected unless it explicitly calls the new method. |
| Refactoring a shared dependency forces every method to change. | The entity has no dependencies to refactor. |

Two developers adding `Create()` and `UpdateDetails()` to `Shelter.cs` will merge cleanly — Git auto-merges non-adjacent text additions. The residual conflict surface (both adding a method at the same line) resolves in seconds and has no semantic consequences. This is qualitatively different from the shared-service case, where adding a dependency to one method ripples through every other method's signature.

### 2. Narrow interfaces in App, implementations in Infrastructure

For repeated I/O — loading and saving aggregates, accessing the file system, reading the clock — the project defines an interface in `Shelter.App/<Aggregate>/` (or `Shelter.App/Common/` for cross-aggregate concerns) and implements it in `Shelter.Infrastructure/<Aggregate>/`.

The repository interface is deliberately narrow:

```csharp
public interface IShelterRepository
{
    Task<Shelter?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(Shelter shelter, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
```

There is **no `UpdateAsync`**. EF Core (and the in-memory placeholder) tracks changes on loaded aggregates: load → mutate via domain methods → call `SaveChangesAsync`. An explicit update method is a code smell that signals the aggregate has been bypassed — its presence pushes mutation logic out of the entity and back into the handler.

Filtered or paged reads do *not* live on this interface. When the project needs them, they go behind a separate `IShelterQueryService` that returns DTOs directly. Mixing aggregate-shaped writes and DTO-shaped reads on the same interface is a small CQRS-lite split that pays for itself: the write-side stays focused on consistency, the read-side stays focused on projection.

### 3. Ambient dependencies behind one-property interfaces

Time is the canonical example. `IClock` exposes a single property:

```csharp
public interface IClock { DateTimeOffset UtcNow { get; } }
```

Handlers inject it; domain methods take `now` as a parameter. The benefit is testability — a handler test can substitute a fake clock without monkey-patching `DateTimeOffset.UtcNow` — at the cost of one constructor parameter. Cheap.

### 4. Decorators for cross-cutting concerns

Logging, validation, timing, transaction scope: these wrap handlers via DI registration (`services.Decorate<IHandler<...>, LoggingDecorator<...>>`), not via inheritance. The current handler count is too small to justify pulling in a decorator framework, but the architecture admits one without restructuring.

---

## Validation layering

"Validation" lumps together three different concerns that belong in three different places:

| Category | Example | Where | On failure |
| --- | --- | --- | --- |
| Format / shape | "name required", "capacity is an integer", "lat in [-90, 90]" | HTTP boundary (FluentValidation, endpoint filter, DataAnnotations) | 400 with field-level errors |
| Domain invariants | "capacity > 0", "an active shelter has ≥ 1 picture", "valid state transition" | Entity factory or mutator method | Throws `DomainValidationException` |
| Contextual / cross-entity | "user owns this shelter", "name unique within owner", "user under shelter quota" | Handler (uses repository or external lookup) | Typed failure (e.g. the `RegisterFailure` enum pattern) or `DomainNotFoundException` |

Boundary validation and entity invariants overlap on simple rules ("capacity > 0" is genuinely both). The project intentionally duplicates them. They serve different audiences:

- **Boundary validation is a UX feature.** It produces a friendly 400 with all field errors aggregated, before any work begins.
- **Entity invariants are a correctness feature.** They guarantee that a `Shelter` cannot exist in an invalid state, regardless of how it's constructed — which matters for tests, background jobs, and any future entry path that doesn't go through the HTTP request layer.

Boundary-only validation is dangerous because non-HTTP paths bypass it. Entity-only validation alone is acceptable but loses friendly aggregated 400s — the user gets a 500 unless something translates the exception. The duplication cost is low (one rule restated in two places); the cost of getting it wrong is either a poor error UX or a corrupted model.

### Domain exceptions and centralised translation

For invariants thrown by the domain to surface as 400-class responses (rather than 500s with a stack trace) the project translates them in one place. The mechanism is a small exception hierarchy in `Shelter.Domain/Common/`:

```csharp
public abstract class DomainException(string message) : Exception(message);
public sealed class DomainValidationException(string message) : DomainException(message);
public sealed class DomainNotFoundException(string message)   : DomainException(message);
```

Entity validators throw the appropriate subtype:

```csharp
private static void ValidateCapacity(int capacity)
{
    if (capacity <= 0) throw new DomainValidationException("Capacity must be positive.");
}
```

A single `IExceptionHandler` implementation in `Shelter.Api/Configuration/DomainExceptionHandler.cs` maps each exception type to a status code and an RFC 7807 `ProblemDetails` body. Stack traces are included only when `IHostEnvironment.IsDevelopment()` is true.

Two important properties of this design:

- **The domain is unaware of HTTP.** It throws `DomainValidationException`, not "BadRequest" or "400". The HTTP status code is decided by the API layer alone — the domain stays free of transport concerns.
- **Only intentional, user-presentable failures are translated.** A bare `ArgumentException` from a third-party library still bubbles to a 500 (and to the developer exception page in dev). The exception type is the explicit marker for "this failure is meant to reach the user." Catching `Exception` broadly would silently turn unrelated bugs into 400s; `DomainException` avoids that.

Adding a new failure category — say, a 409 Conflict for duplicate shelter names — is a two-line change: a new `DomainConflictException` class and a new case in the handler's `switch`. No endpoint or handler code changes.

---

## Authentication and authorisation

JWT Bearer authentication is configured in `Shelter.Infrastructure` via `JwtSettings`, which binds the `Jwt` section of `appsettings.json` and calls `AddAuthentication().AddJwtBearer()` during DI registration. Tokens are HS256-signed and carry `sub`, `email`, `jti`, `NameIdentifier`, and one `Role` claim per role.

### Roles vs policies

ASP.NET Core distinguishes two related concepts that this project keeps deliberately separate:

- A **role** is an identity tag carried as a claim on the user — a noun that says what the user *is*. `AppRoles.ShelterOwner`. Lives in `Shelter.Domain/Auth/AppRoles.cs`.
- A **policy** is a named authorisation rule the framework evaluates per request — an action-oriented name that says what's *being permitted*. `AppPolicies.CanManageShelters`. Lives in `Shelter.Domain/Auth/AppPolicies.cs`.

Endpoints reference the policy, not the role:

```csharp
.RequireAuthorization(AppPolicies.CanManageShelters)
```

The policy is registered once, in `JwtSettings.OnConfigure`:

```csharp
options.AddPolicy(AppPolicies.CanManageShelters, p => p.RequireRole(AppRoles.ShelterOwner));
```

The temptation in a small codebase is to skip this indirection and use the role name directly (`.RequireAuthorization("ShelterOwner")`), since the policy is currently a 1:1 mapping. The reason the project pays the small indirection cost up front is that the two concepts diverge as soon as authorisation gets even slightly more nuanced. The day Admins should also be able to manage shelters, the policy registration becomes `RequireRole(AppRoles.ShelterOwner, AppRoles.Admin)` — a one-line change in one file. Endpoints stay untouched, and the policy name (`CanManageShelters`) still accurately describes what it permits. With role-named policies, the same change would either rename a policy at every call site or end up with a policy called `ShelterOwner` that admits non-shelter-owners — a trap that's easy to fall into and unpleasant to undo.

The authenticated user's identity is resolved in the endpoint from the injected `ClaimsPrincipal`:

```csharp
private static Guid GetUserId(this ClaimsPrincipal principal) =>
    Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
```

This keeps the handler free of any auth knowledge — it receives a plain `Guid ownerId` and has no dependency on the security infrastructure.

The current user store is an in-memory `ConcurrentDictionary` (dev-only stub). The intended production replacement is ASP.NET Core Identity backed by PostgreSQL, at which point `IUserStore` gets a real implementation and `StoredUser` is retired in favour of the Identity `User` entity.

---

## Current state

The infrastructure layer is not yet wired to a real database, but the abstractions that EF Core will eventually implement are already in place:

- `IShelterRepository` is defined in the App layer with the narrow `GetById` / `Add` / `SaveChanges` surface described above. The current implementation is a `ConcurrentDictionary`-backed in-memory store living in `Shelter.Infrastructure.Shelters.InMemoryShelterRepository`. Replacing it with an EF Core `DbContext` is a single class swap with no changes upstream.
- `IClock` is defined in `App.Common` with a `SystemClock` implementation in `Shelter.Infrastructure.Common`, both registered as singletons.
- `Shelter` is a real aggregate: private setters, a `Create` factory that enforces invariants, and named mutator methods. Public mutation paths into the domain go through these methods exclusively. Invariant violations throw `DomainValidationException`.
- The authorisation surface uses named policies (`AppPolicies.CanManageShelters`) registered against role requirements (`RequireRole(AppRoles.ShelterOwner)`); endpoints reference policies only.
- `DomainExceptionHandler` (`IExceptionHandler`) translates `DomainException` subtypes to RFC 7807 `ProblemDetails` responses, with a dev-only stack trace in `extensions.stackTrace`.

Picture URLs are still fabricated in the handler (`https://mock.storage/shelters/{id}/{guid}-{filename}`); `IPictureStorage` is the next abstraction to introduce when real file uploads are wired.

The three auth endpoints and the shelter creation endpoint are fully functional end-to-end — login, register, issue JWT, attach JWT, create shelter, 201 response — against the in-memory stores. Validation failures (e.g. `Capacity = 0`) come back as 400 Bad Request with a structured `ProblemDetails` body. State persists for the process lifetime and is wiped on restart.
