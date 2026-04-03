using System.Text;
using System.Text.Json;
using LibraryManagement.Application.DTOs.Chat;
using LibraryManagement.Application.Interfaces;
using LibraryManagement.Application.Settings;
using LibraryManagement.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace LibraryManagement.Infrastructure.Services;

public class ChatService : IChatService
{
    private readonly IBookRepository _bookRepository;
    private readonly IAuthorRepository _authorRepository;
    private readonly OllamaSettings _settings;
    private readonly HttpClient _httpClient;

    public ChatService(
        IBookRepository bookRepository,
        IAuthorRepository authorRepository,
        IOptions<OllamaSettings> options,
        HttpClient httpClient)
    {
        _bookRepository = bookRepository;
        _authorRepository = authorRepository;
        _settings = options.Value;
        _httpClient = httpClient;
    }

    public async Task<ChatResponseDto> AskAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        var authors = await _authorRepository.GetAllAsync();
        var books = await _bookRepository.GetAllAsync();

        var authorLines = authors.Select(a => $"- [{a.Id}] {a.FullName} | {a.OriginCity} | {a.Email}");
        var bookLines = books.Select(b =>
            $"- [{b.Id}] {b.Title} ({b.Year}) | Género: {b.Genre} | Páginas: {b.NumberOfPages} | Autor: {b.Author?.FullName ?? "Desconocido"}");

        var systemPrompt =
            "Eres un asistente de biblioteca. Solo puedes responder preguntas sobre los libros\n" +
            "y autores registrados en el sistema. No puedes crear, modificar ni eliminar datos.\n" +
            "Responde siempre en español.\n\n" +
            "Autores registrados:\n" +
            string.Join("\n", authorLines) +
            "\n\nLibros registrados:\n" +
            string.Join("\n", bookLines);

        var payload = new
        {
            model = _settings.Model,
            stream = false,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = request.Message }
            }
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync(_settings.Endpoint, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("El servicio de IA local no está disponible en este momento.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("El servicio de IA local no está disponible en este momento.", ex);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        string replyText;

        // OpenAI-compatible format: choices[0].message.content
        if (root.TryGetProperty("choices", out var choices) &&
            choices.GetArrayLength() > 0 &&
            choices[0].TryGetProperty("message", out var choiceMessage) &&
            choiceMessage.TryGetProperty("content", out var choiceContent))
        {
            replyText = choiceContent.GetString() ?? string.Empty;
        }
        // Ollama native format: message.content
        else if (root.TryGetProperty("message", out var message) &&
                 message.TryGetProperty("content", out var messageContent))
        {
            replyText = messageContent.GetString() ?? string.Empty;
        }
        else
        {
            replyText = string.Empty;
        }

        return new ChatResponseDto { Reply = replyText };
    }
}
