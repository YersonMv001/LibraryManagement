# LibraryManagement — Documento de Arquitectura

## Descripción general

**LibraryManagement** es una Web API RESTful construida con **ASP.NET Core (.NET 8)** y **C# 12**, siguiendo los principios de la **Arquitectura Limpia** (también conocida como Arquitectura de Cebolla). El objetivo de este patrón es forzar una separación estricta de responsabilidades, donde el dominio y la lógica de negocio están completamente aislados de frameworks, bases de datos y herramientas externas.

---

## Stack Tecnológico

| Capa           | Tecnología                                     |
|----------------|------------------------------------------------|
| API            | ASP.NET Core 8, Swashbuckle (Swagger) 6.6.2   |
| Application    | C# 12, .NET 8 Class Library                   |
| Domain         | C# 12, .NET 8 Class Library (sin dependencias)|
| Infrastructure | EF Core 8.0.15 + SQL Server Provider          |
| Diseño ORM     | Microsoft.EntityFrameworkCore.Design 8.0.15   |
| IA Local       | Ollama (API REST), `HttpClient` + `System.Text.Json` (integrados en .NET 8) |

---

## Estructura de la Solución

```
LibraryManagement/
├── LibraryManagement.slnx
└── src/
    ├── LibraryManagement.Domain/
    ├── LibraryManagement.Application/
    ├── LibraryManagement.Infrastructure/
    └── LibraryManagement.API/
```

---

## Descripción de las Capas

### 1. Dominio — `LibraryManagement.Domain`

La capa más interna. Contiene las reglas de negocio de la empresa. Tiene **cero dependencias externas**.

```
Domain/
├── Entities/
│   ├── Book.cs           → Id, Title, Year, Genre, NumberOfPages, AuthorId, Author
│   └── Author.cs         → Id, FullName, BirthDate, OriginCity, Email, Books
├── Interfaces/
│   ├── IBookRepository.cs
│   └── IAuthorRepository.cs
└── Exceptions/
    ├── BookNotFoundException.cs          → hereda de Exception
    ├── AuthorNotFoundException.cs        → hereda de Exception
    └── MaximumBooksExceededException.cs  → hereda de Exception
```

**Responsabilidades:**
- Definir las entidades de negocio centrales y sus propiedades.
- Declarar los contratos de repositorio (interfaces) que expresan *qué* operaciones de datos existen, sin especificar *cómo* se realizan.
- Definir excepciones propias del dominio que representan reglas de negocio incumplidas.

**Decisiones de diseño:**
- `Author` posee una `ICollection<Book> Books` (navegación uno-a-muchos).
- `Book` contiene la clave foránea `AuthorId` y una propiedad de navegación `Author`.
- Las interfaces de repositorio devuelven y aceptan entidades del dominio, manteniendo a Application desacoplada de EF Core.

---

### 2. Aplicación — `LibraryManagement.Application`

Contiene la lógica de los casos de uso. Depende únicamente de **Domain**.

```
Application/
├── DTOs/
│   ├── BookDto.cs
│   ├── BookCreateDto.cs
│   ├── AuthorDto.cs
│   ├── AuthorCreateDto.cs
│   └── Chat/
│       ├── ChatRequestDto.cs
│       └── ChatResponseDto.cs
├── Interfaces/
│   ├── IBookService.cs
│   ├── IAuthorService.cs
│   └── IChatService.cs
├── Services/
│   ├── BookService.cs
│   └── AuthorService.cs
└── Settings/
    ├── BookSettings.cs       → MaxBooksAllowed (leído de appsettings.json)
    └── OllamaSettings.cs     → Endpoint, Model (leído de appsettings.json)
```

**Responsabilidades:**
- Orquestar los casos de uso (CRUD de Libros y Autores).
- Mapear entidades del dominio a DTOs y viceversa (mapeo manual, sin dependencia de AutoMapper).
- Definir los contratos de servicio (`IBookService`, `IAuthorService`, `IChatService`) que consume la capa API.

**Decisiones de diseño:**
- Los DTOs se dividen en `Dto` (lectura/respuesta) y `CreateDto` (escritura/solicitud) para aplicar el principio de mínima exposición de superficie por operación.
- Los servicios realizan la proyección de entidad a DTO de forma directa para no incorporar una librería de mapeo de terceros en esta etapa.
- Todos los métodos de servicio son `async/await` para alinearse con el modelo de I/O no bloqueante de EF Core y ASP.NET Core.
- `BookSettings` se inyecta mediante `IOptions<BookSettings>` para permitir configurar reglas de negocio (como `MaxBooksAllowed`) desde `appsettings.json` sin recompilar la aplicación.
- `IChatService` define el contrato del chatbot en Application para mantener el desacoplamiento: la capa API solo conoce la interfaz, no la implementación HTTP.
- `OllamaSettings` se inyecta mediante `IOptions<OllamaSettings>` para configurar el endpoint y el modelo de Ollama desde `appsettings.json`.
- `ChatRequestDto` y `ChatResponseDto` se ubican en `DTOs/Chat/` para agrupar el módulo y diferenciarlo del resto de DTOs.

---

### 3. Infraestructura — `LibraryManagement.Infrastructure`

Contiene la implementación de la persistencia de datos. Depende de **Domain** y **Application**.

```
Infrastructure/
├── Data/
│   └── AppDbContext.cs            → DbSet<Book>, DbSet<Author>
├── Configurations/
│   ├── BookConfiguration.cs
│   └── AuthorConfiguration.cs
├── Repositories/
│   ├── BookRepository.cs
│   └── AuthorRepository.cs
└── Services/
    └── ChatService.cs             → implementa IChatService; orquesta BD + Ollama HTTP
```

**Responsabilidades:**
- Implementar `IBookRepository` e `IAuthorRepository` de la capa Domain usando EF Core.
- Definir `AppDbContext` como el contexto de base de datos de EF Core.
- Configurar los mapeos de entidades usando el patrón `IEntityTypeConfiguration<T>` (Fluent API), cargados automáticamente mediante `ApplyConfigurationsFromAssembly`.
- Implementar `IChatService` mediante `ChatService`, que consulta repositorios y realiza llamadas HTTP a Ollama.

**Decisiones de diseño:**
- `AppDbContext.OnModelCreating` usa `ApplyConfigurationsFromAssembly` para descubrir automáticamente todas las implementaciones de `IEntityTypeConfiguration<T>`, manteniendo el contexto limpio y escalable.
- `BookConfiguration` establece la relación `HasOne → WithMany` entre `Book` y `Author` usando `AuthorId` como clave foránea.
- `AuthorConfiguration` aplica restricciones `IsRequired` y `HasMaxLength` a nivel de base de datos, no solo a nivel de API.
- Los repositorios usan `Include(...)` para la carga ansiosa de propiedades de navegación, evitando el problema de consultas N+1.
- `ChatService` construye un *system prompt* dinámico con todos los autores y libros de la BD antes de enviar la petición a Ollama. No usa ningún SDK externo; el serializado y deserializado se realizan con `System.Text.Json`.
- `ChatService` soporta tanto el formato nativo de Ollama (`message.content`) como el formato compatible OpenAI (`choices[0].message.content`), inspeccionando el JSON con `JsonDocument`.
- Cuando Ollama no está disponible (`HttpRequestException` o timeout), `ChatService` lanza `InvalidOperationException` con un mensaje descriptivo que el `ExceptionHandlingMiddleware` convierte en HTTP 500.

> 💡 **Enfoque actual vs. ideal:** La implementación serializa todos los autores y libros en el *system prompt* de cada petición a Ollama, lo cual es funcional para pruebas con volúmenes pequeños de datos. El enfoque recomendado a largo plazo es exponer los repositorios como *tools* mediante **MCP** (*Model Context Protocol*), permitiendo que el modelo LLM consulte los datos bajo demanda sin saturar la ventana de contexto.

**Paquetes NuGet:**
- `Microsoft.EntityFrameworkCore.SqlServer` v8.0.15

---

### 4. API — `LibraryManagement.API`

La capa más externa. El punto de entrada y raíz de composición. Depende de **Application** e **Infrastructure**.

```
API/
├── Controllers/
│   ├── BooksController.cs
│   ├── AuthorsController.cs
│   └── ChatController.cs
├── Middlewares/
│   └── ExceptionHandlingMiddleware.cs
└── Program.cs
```

**Responsabilidades:**
- Exponer los endpoints HTTP (CRUD completo de Libros y Autores, y `POST /api/chat/ask`).
- Registrar todos los bindings de inyección de dependencias en `Program.cs`.
- Manejar preocupaciones transversales (excepciones) mediante middleware.
- Configurar Swagger para la documentación de la API.

**Decisiones de diseño:**
- `ExceptionHandlingMiddleware` se registra primero en el pipeline, antes del middleware de enrutamiento, para capturar cualquier excepción no controlada de todas las capas posteriores.
- Mapeo de excepciones:
  - `BookNotFoundException` → HTTP 404 Not Found
  - `AuthorNotFoundException` → HTTP 404 Not Found
  - `MaximumBooksExceededException` → HTTP 400 Bad Request
  - `Exception` no controlada → HTTP 500 Internal Server Error
- `Microsoft.EntityFrameworkCore.Design` se agrega con `PrivateAssets=all` para restringirlo a herramientas de diseño (migraciones) sin filtrarlo como dependencia transitiva en tiempo de ejecución.

**Paquetes NuGet:**
- `Swashbuckle.AspNetCore` v6.6.2
- `Microsoft.AspNetCore.OpenApi` v8.0.25
- `Microsoft.EntityFrameworkCore.Design` v8.0.15 *(solo diseño)*

---

## Grafo de Dependencias

```
┌──────────────────────────────────────────────┐
│                LibraryManagement.API          │
│  (Controllers, Middleware, Program.cs, DI)   │
└────────────────┬─────────────────────────────┘
                 │ referencia
       ┌─────────┴──────────┐
       ▼                    ▼
┌─────────────┐    ┌──────────────────────┐
│ Application │    │    Infrastructure    │
│  Services   │    │  Repositories + EF   │
│   + DTOs    │    │   Core + DbContext   │
└──────┬──────┘    └──────────┬───────────┘
       │ referencia           │ referencia
       └──────────┬───────────┘
                  ▼
         ┌────────────────┐
         │     Domain     │
         │ Entities +     │
         │ Interfaces +   │
         │ Exceptions     │
         └────────────────┘
```

**Regla:** Las dependencias solo apuntan hacia adentro. Domain no tiene conocimiento de ninguna otra capa.


---

## Registros de Inyección de Dependencias

Todos los registros de repositorios y servicios usan tiempo de vida **Scoped**, que es la elección correcta para EF Core dentro del contexto de una petición web. El cliente HTTP del chatbot usa tiempo de vida **Transient** gestionado por `IHttpClientFactory`.

```csharp
// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositorios (interfaz Domain → implementación Infrastructure)
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IAuthorRepository, AuthorRepository>();

// Servicios (interfaz Application → implementación Application)
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IAuthorService, AuthorService>();

// ── Chatbot Local (módulo independiente) ──────────────────────────────────────
builder.Services.Configure<OllamaSettings>(builder.Configuration.GetSection("Ollama"));
builder.Services.AddHttpClient<IChatService, ChatService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(120);
});
// ──────────────────────────────────────────────────────────────────────────────
```

---

## Relación entre Entidades

```
Author (1) ────────────────── (N) Book
  - Id                           - Id
  - FullName                     - Title
  - BirthDate                    - Year
  - OriginCity                   - Genre
  - Email                        - NumberOfPages
  - Books (nav)                  - AuthorId (FK)
                                 - Author (nav)
```

---

## Estrategia de Manejo de Excepciones

Las excepciones del dominio se lanzan desde las capas de servicio/repositorio y suben hasta `ExceptionHandlingMiddleware`, que las traduce en respuestas JSON estructuradas:

```json
{ "error": "No se encontró el autor con id 99." }
```

Esto mantiene las acciones de los controladores libres de bloques try/catch y centraliza la lógica de presentación de errores. Para el chatbot, `ChatService` envuelve los errores de conectividad con Ollama en `InvalidOperationException`, que también es capturada por el mismo middleware y devuelta como HTTP 500.

---

## Próximos Pasos

- [x] Agregar migraciones de EF Core (`20260402055533_InitialCreate`)
- [x] Implementar la regla de negocio de `MaximumBooksExceededException` en `BookService`
- [x] Implementar `BookNotFoundException` y lanzarla en `UpdateAsync`
- [x] Implementar `BookSettings` con `IOptions<T>` para configurar `MaxBooksAllowed`
- [x] Agregar validación de datos (`FluentValidation` o Data Annotations en los DTOs `CreateDto`)
- [x] Módulo de Chatbot de IA local con Ollama (lectura de BD + respuesta en lenguaje natural)

