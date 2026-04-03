# LibraryManagement — Dev Log

---

## [Session 1] — Initial Scaffolding

### Goal
Build a complete Clean Architecture solution for a technical test from scratch using .NET 8 and C# 12.

### Actions

**Solution file**
- The workspace already contained a `LibraryManagement.slnx` (VS 2026 format). The file was overwritten using `dotnet new sln -n LibraryManagement --force`, which regenerated it cleanly.

**Project creation**
Four projects were scaffolded inside `src/` using `dotnet new`:

| Command | Template |
|---|---|
| `dotnet new classlib` | Domain, Application, Infrastructure |
| `dotnet new webapi` | API |

All target `net8.0`.

**Projects added to solution**
```bash
dotnet sln add src/LibraryManagement.Domain/...
dotnet sln add src/LibraryManagement.Application/...
dotnet sln add src/LibraryManagement.Infrastructure/...
dotnet sln add src/LibraryManagement.API/...
```

---

### Project references wired up

```
Application  → Domain
Infrastructure → Domain, Application
API          → Application, Infrastructure
```

---

### NuGet packages

Initial attempt using floating version `8.*`:
```
Microsoft.EntityFrameworkCore.SqlServer 8.*   → Infrastructure
Microsoft.EntityFrameworkCore.Design    8.*   → API
```

**Issue encountered:** The NuGet resolver picked `10.0.5` (the latest available), which is incompatible with `net8.0`. Error:
```
NU1202: Package Microsoft.EntityFrameworkCore.Design 10.0.5 is not compatible with net8.0
```

**Fix:** Pinned to exact version `8.0.15` in both `.csproj` files:
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.15" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.15" />
```

---

## Domain Layer — `LibraryManagement.Domain`

### Files created

**Entities**

`Book.cs`
- Properties: `Id`, `Title`, `Year`, `Genre`, `NumberOfPages`, `AuthorId`, `Author`
- Navigation property `Author` marked `null!` (EF Core will always populate it after a query that includes it)

`Author.cs`
- Properties: `Id`, `FullName`, `BirthDate`, `OriginCity`, `Email`, `Books`
- Collection `Books` initialized as `new List<Book>()` to avoid null reference on new instances

**Interfaces**

`IBookRepository.cs` / `IAuthorRepository.cs`
- Declared `GetAllAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`
- Return `Task<T>` on all methods (fully async contract)

**Exceptions**

`AuthorNotFoundException.cs` / `MaximumBooksExceededException.cs`
- Both extend `Exception`
- Constructor accepts a `string message` and passes it to `base(message)`

---

## Application Layer — `LibraryManagement.Application`

### Files created

**DTOs**

| File | Purpose |
|---|---|
| `BookDto.cs` | Read/response DTO — includes `Id` |
| `BookCreateDto.cs` | Write/request DTO — excludes `Id` (set by DB) |
| `AuthorDto.cs` | Read/response DTO — includes `Id` |
| `AuthorCreateDto.cs` | Write/request DTO — excludes `Id` |

**Interfaces**

`IBookService.cs` / `IAuthorService.cs`
- Defined full CRUD signatures using DTOs as input/output (no domain entities leaked to API)

**Services**

`BookService.cs` / `AuthorService.cs`
- Inject repository interface via constructor
- Manual entity → DTO projection (no third-party mapper)
- All methods `async/await`

---

## Infrastructure Layer — `LibraryManagement.Infrastructure`

### Files created

**`AppDbContext.cs`**
- Extends `DbContext`
- Exposes `DbSet<Book> Books` and `DbSet<Author> Authors`
- `OnModelCreating` calls `ApplyConfigurationsFromAssembly` to auto-discover all `IEntityTypeConfiguration<T>` classes in the assembly

**`BookConfiguration.cs`**
- `HasKey(b => b.Id)`
- `Title`: required, max 200 chars
- `Genre`: required, max 100 chars
- Relationship: `HasOne(Book.Author).WithMany(Author.Books).HasForeignKey(Book.AuthorId)`

**`AuthorConfiguration.cs`**
- `HasKey(a => a.Id)`
- `FullName`: required, max 200 chars
- `Email`: required, max 150 chars
- `OriginCity`: required, max 100 chars

**`BookRepository.cs` / `AuthorRepository.cs`**
- Implement domain interfaces using `AppDbContext`
- `GetAllAsync` and `GetByIdAsync` use `.Include(...)` for eager loading of navigation properties
- `AddAsync` / `UpdateAsync` / `DeleteAsync` call `SaveChangesAsync()` after each operation

---

## API Layer — `LibraryManagement.API`

### Files created

**`BooksController.cs` / `AuthorsController.cs`**
- `[ApiController]` + `[Route("api/[controller]")]`
- Full CRUD: `GET /api/books`, `GET /api/books/{id}`, `POST /api/books`, `PUT /api/books/{id}`, `DELETE /api/books/{id}`
- `CreateAsync` returns `201 Created` with `CreatedAtAction` pointing to `GetById`

**`ExceptionHandlingMiddleware.cs`**
- Wraps `_next(context)` in a try/catch
- Maps domain exceptions to HTTP status codes:
  - `AuthorNotFoundException` → 404
  - `MaximumBooksExceededException` → 400
  - `Exception` (catch-all) → 500
- Writes JSON error body: `{ "error": "..." }`

**`Program.cs`**
- Replaced the default minimal API template content entirely
- Registered: `AppDbContext`, `IBookRepository`, `IAuthorRepository`, `IBookService`, `IAuthorService`
- Registered: `ExceptionHandlingMiddleware` as the first middleware in the pipeline
- Configured: Swagger (Swashbuckle), Controllers, HTTPS redirection

**Issue encountered — `Program.cs` partial replace**
The first `replace_string_in_file` call only matched the beginning of the file, leaving leftover Minimal API template code. A second targeted replace removed the remaining boilerplate (`summaries`, `WeatherForecast` record, `MapGet`).

**`appsettings.json`**
Added the `ConnectionStrings` section with a default SQL Server LocalDB connection string:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=LibraryManagementDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

---

## Issues & Resolutions

### Issue 1 — Floating NuGet version resolves to incompatible version
**Symptom:** `NU1202` — EF Core 10.0.5 is not compatible with net8.0  
**Root cause:** `dotnet add package` without `--version` resolves to the latest stable, which is now 10.x.  
**Fix:** Pinned to `Version="8.0.15"` in both affected `.csproj` files.

---

### Issue 2 — Root `LibraryManagement.csproj` conflicts with src projects
**Symptom:** VS IDE reported a large set of errors:
- `CS8802` — duplicate top-level statements (VS loaded root `.csproj` that glob-included `src/**/*.cs`)
- `CS0579` — duplicate assembly attributes (same .cs files compiled twice)
- `CS0234` — EF Core types not found (root project had no EF Core reference)

**Root cause:** A `LibraryManagement.csproj` existed at the workspace root (leftover from a previous VS-generated project). Being an `Sdk.Web` project at the workspace root, its default glob `**/*.cs` captured all source files under `src/`, including the API's `Program.cs` and all Infrastructure files.

**Fix:** Deleted `LibraryManagement.csproj` from the root. Deleted the root `obj/` and `bin/` directories to clear stale MSBuild artifacts.

**CLI build result after fix:**
```
Compilación realizado correctamente en 1,5s
0 Errores
```

---

### Issue 3 — VS IDE still reports errors after fix
**Symptom:** `run_build` (VS IDE build) continued to report errors even after the root `.csproj` was deleted and CLI build succeeded.  
**Root cause:** Visual Studio has the old project loaded in memory. The IDE build system operates on the in-memory project state, not the file system state. VS needs to reload the solution to pick up the changes.  
**Fix for user:** In Visual Studio: `File → Close Solution`, then reopen `LibraryManagement.slnx`.

---

## Build Status

| Tool | Result |
|---|---|
| `dotnet build` (CLI) | ✅ 0 errors, 0 warnings |
| VS IDE `run_build` | ✅ 0 errors, 0 warnings |

---

## File Inventory

### Domain
- `src/LibraryManagement.Domain/Entities/Book.cs`
- `src/LibraryManagement.Domain/Entities/Author.cs`
- `src/LibraryManagement.Domain/Interfaces/IBookRepository.cs`
- `src/LibraryManagement.Domain/Interfaces/IAuthorRepository.cs`
- `src/LibraryManagement.Domain/Exceptions/BookNotFoundException.cs`
- `src/LibraryManagement.Domain/Exceptions/AuthorNotFoundException.cs`
- `src/LibraryManagement.Domain/Exceptions/MaximumBooksExceededException.cs`

### Application
- `src/LibraryManagement.Application/DTOs/BookDto.cs`
- `src/LibraryManagement.Application/DTOs/BookCreateDto.cs`
- `src/LibraryManagement.Application/DTOs/AuthorDto.cs`
- `src/LibraryManagement.Application/DTOs/AuthorCreateDto.cs`
- `src/LibraryManagement.Application/DTOs/Chat/ChatRequestDto.cs` *(nuevo)*
- `src/LibraryManagement.Application/DTOs/Chat/ChatResponseDto.cs` *(nuevo)*
- `src/LibraryManagement.Application/Interfaces/IBookService.cs`
- `src/LibraryManagement.Application/Interfaces/IAuthorService.cs`
- `src/LibraryManagement.Application/Interfaces/IChatService.cs` *(nuevo)*
- `src/LibraryManagement.Application/Services/BookService.cs`
- `src/LibraryManagement.Application/Services/AuthorService.cs`
- `src/LibraryManagement.Application/Settings/BookSettings.cs`
- `src/LibraryManagement.Application/Settings/OllamaSettings.cs` *(nuevo)*

### Infrastructure
- `src/LibraryManagement.Infrastructure/Data/AppDbContext.cs`
- `src/LibraryManagement.Infrastructure/Data/Migrations/20260402055533_InitialCreate.cs`
- `src/LibraryManagement.Infrastructure/Data/Migrations/AppDbContextModelSnapshot.cs`
- `src/LibraryManagement.Infrastructure/Configurations/BookConfiguration.cs`
- `src/LibraryManagement.Infrastructure/Configurations/AuthorConfiguration.cs`
- `src/LibraryManagement.Infrastructure/Repositories/BookRepository.cs`
- `src/LibraryManagement.Infrastructure/Repositories/AuthorRepository.cs`
- `src/LibraryManagement.Infrastructure/Services/ChatService.cs` *(nuevo)*

### API
- `src/LibraryManagement.API/Controllers/BooksController.cs`
- `src/LibraryManagement.API/Controllers/AuthorsController.cs`
- `src/LibraryManagement.API/Controllers/ChatController.cs` *(nuevo)*
- `src/LibraryManagement.API/Middlewares/ExceptionHandlingMiddleware.cs`
- `src/LibraryManagement.API/Program.cs`
- `src/LibraryManagement.API/appsettings.json`
---

## [Session 2] — Reglas de negocio, BookSettings, migraciones y middleware

### Goal
Implementar las reglas de negocio del sistema, configuración flexible de límites, manejo centralizado de excepciones y la primera migración de base de datos.

---

### `BookNotFoundException` — Nueva excepción de dominio

- Añadida a `LibraryManagement.Domain/Exceptions/`.
- Lanzada en `BookService.UpdateAsync` cuando el libro no existe en la base de datos.
- El middleware la captura y devuelve `404 Not Found`.

---

### `BookSettings` — Configuración de reglas de negocio

- Clase `BookSettings` creada en `LibraryManagement.Application/Settings/`.
- Contiene la propiedad `MaxBooksAllowed` (entero).
- Registrada en DI mediante `builder.Services.Configure<BookSettings>(builder.Configuration.GetSection(BookSettings.SectionName))`.
- Inyectada en `BookService` a través de `IOptions<BookSettings>`.
- El valor se configura en `appsettings.json` bajo la clave `BookSettings:MaxBooksAllowed`.

**Motivación:** Permite modificar el límite de libros sin recompilar la aplicación, siguiendo el principio de configuración externalizada.

---

### `BookService` — Reglas de negocio implementadas

**Regla 1 — Validación de autor:**
```csharp
var author = await _authorRepository.GetByIdAsync(dto.AuthorId);
if (author is null)
    throw new AuthorNotFoundException();
```
Aplicada tanto en `CreateAsync` como en `UpdateAsync`.

**Regla 2 — Límite máximo de libros:**
```csharp
var currentBookCount = await _bookRepository.CountAsync();
if (currentBookCount >= _bookSettings.MaxBooksAllowed)
    throw new MaximumBooksExceededException();
```
Aplicada en `CreateAsync` tras validar el autor.

**Decisión técnica:** En `CreateAsync`, el nombre del autor en el `BookDto` de respuesta se obtiene del objeto `author` ya cargado en memoria, evitando una consulta adicional a la base de datos.

---

### `ExceptionHandlingMiddleware` — Actualizado

Añadida la captura de `BookNotFoundException`:

```csharp
catch (BookNotFoundException ex)
{
    await WriteErrorResponse(context, HttpStatusCode.NotFound, ex.Message);
}
```

Mapeo final de excepciones:

| Excepción | HTTP |
|---|---|
| `BookNotFoundException` | `404 Not Found` |
| `AuthorNotFoundException` | `404 Not Found` |
| `MaximumBooksExceededException` | `400 Bad Request` |
| `Exception` genérica | `500 Internal Server Error` |

---

### Primera migración de EF Core

```bash
dotnet ef migrations add InitialCreate --project src/LibraryManagement.Infrastructure --startup-project src/LibraryManagement.API
```

Archivos generados:
- `20260402055533_InitialCreate.cs` — crea las tablas `Authors` y `Books` con la relación FK.
- `AppDbContextModelSnapshot.cs` — snapshot del modelo actual.

Base de datos aplicada con:
```bash
dotnet ef database update --project src/LibraryManagement.Infrastructure --startup-project src/LibraryManagement.API
```

---

### Issues & Resolutions (Session 2)

**Ninguno.** La sesión transcurrió sin errores de compilación ni de NuGet.

---

### Build Status (Session 2)

| Tool | Result |
|---|---|
| `dotnet build` (CLI) | ✅ 0 errors, 0 warnings |
| VS IDE `run_build` | ✅ 0 errors, 0 warnings |

---

## [Session 3] — Módulo de Chatbot de IA Local

### Goal

Agregar un módulo de chatbot de IA completamente independiente que permita hacer preguntas sobre libros y autores registrados, respondidas por un modelo de lenguaje local (LLM) servido por Ollama en `http://localhost:11434`.

### Reglas de diseño aplicadas

- **Módulo completamente independiente:** si se eliminan todos los archivos nuevos y se revierten `Program.cs` y `appsettings.json`, el resto de la aplicación compila y funciona sin cambios.
- **Sin SDKs externos:** solo `HttpClient` y `System.Text.Json`, ambos integrados en .NET 8. No se agrega ningún paquete NuGet.
- **Solo lectura:** el chatbot no puede crear, modificar ni eliminar datos.
- **`stream: false`** en el payload enviado a Ollama para recibir la respuesta en una sola llamada JSON (sin SSE).

### Archivos nuevos

#### Application
- **`DTOs/Chat/ChatRequestDto.cs`** — DTO de entrada con propiedad `Message`.
- **`DTOs/Chat/ChatResponseDto.cs`** — DTO de salida con propiedad `Reply`.
- **`Interfaces/IChatService.cs`** — Contrato `AskAsync(ChatRequestDto, CancellationToken)` desacoplado de la implementación HTTP.
- **`Settings/OllamaSettings.cs`** — Configuración tipada: `Endpoint` y `Model`.

#### Infrastructure
- **`Services/ChatService.cs`** — Implementación de `IChatService`:
  1. Consulta todos los autores y libros con `GetAllAsync()`.
  2. Construye un *system prompt* dinámico con el contexto real de la BD.
  3. Serializa el payload y lo envía al endpoint de Ollama vía `HttpClient.PostAsync`.
  4. Parsea la respuesta con `JsonDocument`; soporta formato nativo Ollama (`message.content`) y formato compatible OpenAI (`choices[0].message.content`).
  5. En caso de `HttpRequestException` o `TaskCanceledException` (timeout), lanza `InvalidOperationException` con el mensaje `"El servicio de IA local no está disponible en este momento."`.

#### API
- **`Controllers/ChatController.cs`** — `POST /api/chat/ask` que delega a `IChatService.AskAsync`.

### Archivos modificados

**`Program.cs`**
- Añadido `using LibraryManagement.Infrastructure.Services;`
- Añadido bloque de registro:

```csharp
// ── Chatbot Local (módulo independiente) ──────────────────────────────────────
builder.Services.Configure<OllamaSettings>(builder.Configuration.GetSection("Ollama"));
builder.Services.AddHttpClient<IChatService, ChatService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(120);
});
// ──────────────────────────────────────────────────────────────────────────────
```

**`appsettings.json`**
- Añadida sección `"Ollama"` con `Endpoint` y `Model`.

### Decisiones de diseño

| Decisión | Justificación |
|---|---|
| `IChatService` en Application | Mantiene el contrato desacoplado de la implementación HTTP. La capa API no sabe nada de Ollama. |
| `ChatService` en Infrastructure | Accede a repositorios de dominio y realiza llamadas HTTP externas. |
| `AddHttpClient<IChatService, ChatService>` | Usa `IHttpClientFactory` internamente; evita *socket exhaustion* y gestiona el ciclo de vida de `HttpClient` correctamente. Tiempo de vida Transient. |
| Timeout de 120 s | Los modelos LLM locales pueden tardar entre 10 y 120 s según hardware y tamaño del modelo. |
| `JsonDocument` para parsear la respuesta | Soporta múltiples formatos (nativo Ollama y compatible OpenAI) sin clases de deserialización adicionales. |
| Excepciones HTTP → `InvalidOperationException` | El `ExceptionHandlingMiddleware` existente captura `Exception` genérica y responde HTTP 500. No fue necesario modificar el middleware. |

### Build Status

| Tool | Result |
|---|---|
| VS IDE `run_build` | ✅ 0 errores, 0 advertencias |
