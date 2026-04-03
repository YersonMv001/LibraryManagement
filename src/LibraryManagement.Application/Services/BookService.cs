using LibraryManagement.Application.DTOs;
using LibraryManagement.Application.Interfaces;
using LibraryManagement.Application.Settings;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Exceptions;
using LibraryManagement.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace LibraryManagement.Application.Services;

public class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;
    private readonly IAuthorRepository _authorRepository;
    private readonly BookSettings _bookSettings;

    public BookService(
        IBookRepository bookRepository,
        IAuthorRepository authorRepository,
        IOptions<BookSettings> bookSettings)
    {
        _bookRepository = bookRepository;
        _authorRepository = authorRepository;
        _bookSettings = bookSettings.Value;
    }

    public async Task<IEnumerable<BookDto>> GetAllAsync()
    {
        var books = await _bookRepository.GetAllAsync();
        return books.Select(b => new BookDto
        {
            Id = b.Id,
            Title = b.Title,
            Year = b.Year,
            Genre = b.Genre,
            NumberOfPages = b.NumberOfPages,
            AuthorId = b.AuthorId,
            AuthorName = b.Author.FullName
        });
    }

    public async Task<BookDto?> GetByIdAsync(int id)
    {
        var book = await _bookRepository.GetByIdAsync(id);
        if (book is null) return null;
        return new BookDto
        {
            Id = book.Id,
            Title = book.Title,
            Year = book.Year,
            Genre = book.Genre,
            NumberOfPages = book.NumberOfPages,
            AuthorId = book.AuthorId,
            AuthorName = book.Author.FullName
        };
    }

    public async Task<BookDto> CreateAsync(BookCreateDto dto)
    {
        // --- REGLA DE NEGOCIO 1: Validar Autor ---
        var author = await _authorRepository.GetByIdAsync(dto.AuthorId);
        if (author is null)
            throw new AuthorNotFoundException();

        // --- REGLA DE NEGOCIO 2: Límite de Libros ---
        var currentBookCount = await _bookRepository.CountAsync();
        if (currentBookCount >= _bookSettings.MaxBooksAllowed)
            throw new MaximumBooksExceededException();

        var book = new Book
        {
            Title = dto.Title,
            Year = dto.Year,
            Genre = dto.Genre,
            NumberOfPages = dto.NumberOfPages,
            AuthorId = dto.AuthorId
        };

        await _bookRepository.AddAsync(book);

        return new BookDto
        {
            Id = book.Id,
            Title = book.Title,
            Year = book.Year,
            Genre = book.Genre,
            NumberOfPages = book.NumberOfPages,
            AuthorId = book.AuthorId,
            AuthorName = author.FullName  // Desición tecnica, se puede obtener el nombre del autor directamente del objeto author que ya tenemos
        };
    }

    public async Task UpdateAsync(int id, BookCreateDto dto)
    {
        var book = await _bookRepository.GetByIdAsync(id);
        if (book is null) throw new BookNotFoundException();

        var author = await _authorRepository.GetByIdAsync(dto.AuthorId);
        if (author is null) throw new AuthorNotFoundException();

        book.Title = dto.Title;
        book.Year = dto.Year;
        book.Genre = dto.Genre;
        book.NumberOfPages = dto.NumberOfPages;
        book.AuthorId = dto.AuthorId;

        await _bookRepository.UpdateAsync(book);
    }

    public async Task DeleteAsync(int id)
    {
        await _bookRepository.DeleteAsync(id);
    }
}