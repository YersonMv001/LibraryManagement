# 📚 LibraryManagement API

REST API para la gestión de libros y autores, construida con **.NET 8** siguiendo los principios de **Clean Architecture**.

---

## 🚀 Tecnologías utilizadas

| Tecnología | Versión |
|---|---|
| .NET | 8 |
| ASP.NET Core Web API | 8 |
| Entity Framework Core | 8 |
| SQL Server | - |
| Swagger / OpenAPI | - |
| Ollama (LLM local) | - |

---

## 📁 Estructura del proyecto

```
LibraryManagement/
├── src/
│   ├── LibraryManagement.Domain/          # Entidades, interfaces e interfaces de repositorio
│   │   ├── Entities/
│   │   │   ├── Book.cs
│   │   │   └── Author.cs
│   │   ├── Interfaces/
│   │   │   ├── IBookRepository.cs
│   │   │   └── IAuthorRepository.cs
│   │   └── Exceptions/
│   │       ├── BookNotFoundException.cs
│   │       ├── AuthorNotFoundException.cs
│   │       └── MaximumBooksExceededException.cs
│   │
│   ├── LibraryManagement.Application/     # Lógica de negocio, servicios y DTOs
│   │   ├── DTOs/
│   │   │   ├── BookDto.cs
│   │   │   ├── BookCreateDto.cs
│   │   │   ├── AuthorDto.cs
│   │   │   ├── AuthorCreateDto.cs
│   │   │   └── Chat/
│   │   │       ├── ChatRequestDto.cs
│   │   │       └── ChatResponseDto.cs
│   │   ├── Interfaces/
│   │   │   ├── IBookService.cs
│   │   │   ├── IAuthorService.cs
│   │   │   └── IChatService.cs
│   │   ├── Services/
│   │   │   ├── BookService.cs
│   │   │   └── AuthorService.cs
│   │   └── Settings/
│   │       ├── BookSettings.cs
│   │       └── OllamaSettings.cs
│   │
│   ├── LibraryManagement.Infrastructure/  # Persistencia, EF Core y repositorios
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   └── Migrations/
│   │   ├── Configurations/
│   │   │   ├── BookConfiguration.cs
│   │   │   └── AuthorConfiguration.cs
│   │   ├── Repositories/
│   │   │   ├── BookRepository.cs
│   │   │   └── AuthorRepository.cs
│   │   └── Services/
│   │       └── ChatService.cs
│   │
│   └── LibraryManagement.API/             # Capa de presentación (controladores, middlewares)
│       ├── Controllers/
│       │   ├── BooksController.cs
│       │   ├── AuthorsController.cs
│       │   └── ChatController.cs
│       ├── Middlewares/
│       │   └── ExceptionHandlingMiddleware.cs
│       └── Program.cs
│
├── docs/
│   ├── ARCHITECTURE.md
│   └── DEVLOG.md
└── README.md
```

---

## ⚙️ Requisitos previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server) (local o remoto)
- [Visual Studio 2022+](https://visualstudio.microsoft.com/) o VS Code
- [Ollama](https://ollama.com/) *(opcional — requerido solo para el módulo de chatbot)*

---

## 🛠️ Configuración e instalación

### 1. Clonar el repositorio

```bash
git clone <url-del-repositorio>
cd LibraryManagement
```

### 2. Configurar la cadena de conexión

En `src/LibraryManagement.API/appsettings.json`, actualiza la cadena de conexión:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=LibraryManagementDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "BookSettings": {
    "MaxBooksAllowed": 100
  },
  "Ollama": {
    "Endpoint": "http://localhost:11434/api/chat",
    "Model": "llama3"
  }
}
```

### 3. Aplicar las migraciones

```bash
cd src/LibraryManagement.API
dotnet ef database update
```

### 4. Ejecutar la aplicación

```bash
dotnet run --project src/LibraryManagement.API
```

La API estará disponible en `https://localhost:{puerto}`.
Swagger UI disponible en `https://localhost:{puerto}/swagger`.

### 5. Configurar Ollama para el chatbot *(opcional)*

Omite este paso si no vas a usar el módulo de chatbot.

#### 5.1 Instalar Ollama

Descarga el instalador desde [https://ollama.com/download](https://ollama.com/download) o, en Windows, instálalo con **winget**:

```bash
winget install Ollama.Ollama
```

> Tras la instalación, Ollama arranca automáticamente como servicio en `http://localhost:11434`.

#### 5.2 Descargar un modelo de lenguaje

```bash
# Modelo por defecto configurado en el proyecto
ollama pull llama3

# Alternativas según la capacidad del hardware
ollama pull llama3.2     # más reciente y eficiente que llama3
ollama pull mistral      # buena calidad, menor uso de RAM (~4 GB)
ollama pull phi3         # muy ligero, ideal para hardware limitado (~2 GB)
```

> Elige el modelo que mejor se adapte a tu equipo. Si usas uno distinto a `llama3`, actualiza el campo `Model` en `appsettings.json` (ver paso siguiente).

#### 5.3 Verificar que el servidor está activo

```bash
ollama list          # lista los modelos descargados
ollama run llama3    # prueba interactiva (Ctrl+D para salir)
```

#### 5.4 Ajustar `appsettings.json`

La sección `Ollama` ya está incluida con los valores por defecto. Solo modifícala si cambiaste el modelo o el puerto:

```json
"Ollama": {
  "Endpoint": "http://localhost:11434/api/chat",
  "Model": "llama3"
}
```

---

## 📡 Endpoints de la API

### Books

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/books` | Obtener todos los libros |
| `GET` | `/api/books/{id}` | Obtener un libro por ID |
| `POST` | `/api/books` | Crear un nuevo libro |
| `PUT` | `/api/books/{id}` | Actualizar un libro existente |
| `DELETE` | `/api/books/{id}` | Eliminar un libro |

### Authors

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/authors` | Obtener todos los autores |
| `GET` | `/api/authors/{id}` | Obtener un autor por ID |
| `POST` | `/api/authors` | Crear un nuevo autor |
| `PUT` | `/api/authors/{id}` | Actualizar un autor existente |
| `DELETE` | `/api/authors/{id}` | Eliminar un autor |

### 🤖 Chatbot

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/api/chat/ask` | Envía un mensaje al chatbot local de biblioteca |

> ⚠️ Requiere Ollama corriendo en local (`http://localhost:11434`). El chatbot solo consulta datos; no crea ni modifica registros.

> 💡 **Enfoque actual (solo prueba):** Los datos de libros y autores se inyectan directamente en el *system prompt* de cada petición. Lo ideal sería implementarlo vía **MCP** (*Model Context Protocol*), exponiendo los repositorios como herramientas que el modelo puede invocar dinámicamente, evitando saturar el contexto con datos estáticos.

---

## 📖 Modelos

### BookCreateDto

```json
{
  "title": "string",
  "year": 2024,
  "genre": "string",
  "numberOfPages": 300,
  "authorId": 1
}
```

### AuthorCreateDto

```json
{
  "fullName": "string",
  "birthDate": "1980-01-01",
  "originCity": "string",
  "email": "author@example.com"
}
```

### ChatRequestDto

```json
{
  "message": "¿Qué libros de García Márquez están registrados?"
}
```

### ChatResponseDto

```json
{
  "reply": "En el sistema se encuentran registrados los siguientes libros de Gabriel García Márquez: ..."
}
```

---

## ✅ Reglas de negocio

- **Validación de autor:** Al crear o actualizar un libro, el `AuthorId` debe corresponder a un autor existente. Si no existe, se retorna `404 Not Found`.
- **Límite de libros:** El sistema tiene un máximo de libros permitidos configurado en `appsettings.json` (`BookSettings:MaxBooksAllowed`). Si se supera el límite, se retorna `400 Bad Request`.
- **Chatbot de solo lectura:** El módulo de chatbot únicamente consulta datos existentes. No puede crear, modificar ni eliminar libros ni autores.

---

## ❗ Manejo de errores

La API utiliza un middleware global de manejo de excepciones (`ExceptionHandlingMiddleware`) que convierte las excepciones del dominio en respuestas HTTP estructuradas:

| Excepción | Código HTTP |
|---|---|
| `BookNotFoundException` | `404 Not Found` |
| `AuthorNotFoundException` | `404 Not Found` |
| `MaximumBooksExceededException` | `400 Bad Request` |
| `Exception` (genérica) | `500 Internal Server Error` |

Formato de respuesta de error:

```json
{
  "error": "Mensaje descriptivo del error"
}
```
