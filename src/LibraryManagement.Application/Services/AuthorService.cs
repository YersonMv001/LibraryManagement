using LibraryManagement.Application.DTOs;
using LibraryManagement.Application.Interfaces;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;

namespace LibraryManagement.Application.Services;

public class AuthorService : IAuthorService
{
    private readonly IAuthorRepository _authorRepository;

    public AuthorService(IAuthorRepository authorRepository)
    {
        _authorRepository = authorRepository;
    }

    public async Task<IEnumerable<AuthorDto>> GetAllAsync()
    {
        var authors = await _authorRepository.GetAllAsync();
        return authors.Select(a => new AuthorDto
        {
            Id = a.Id,
            FullName = a.FullName,
            BirthDate = a.BirthDate,
            OriginCity = a.OriginCity,
            Email = a.Email
        });
    }

    public async Task<AuthorDto?> GetByIdAsync(int id)
    {
        var author = await _authorRepository.GetByIdAsync(id);
        if (author is null) return null;
        return new AuthorDto
        {
            Id = author.Id,
            FullName = author.FullName,
            BirthDate = author.BirthDate,
            OriginCity = author.OriginCity,
            Email = author.Email
        };
    }

    public async Task<AuthorDto> CreateAsync(AuthorCreateDto dto)
    {
        var author = new Author
        {
            FullName = dto.FullName,
            BirthDate = dto.BirthDate,
            OriginCity = dto.OriginCity,
            Email = dto.Email
        };
        await _authorRepository.AddAsync(author);
        return new AuthorDto
        {
            Id = author.Id,
            FullName = author.FullName,
            BirthDate = author.BirthDate,
            OriginCity = author.OriginCity,
            Email = author.Email
        };
    }

    public async Task UpdateAsync(int id, AuthorCreateDto dto)
    {
        var author = await _authorRepository.GetByIdAsync(id);
        if (author is null) return;
        author.FullName = dto.FullName;
        author.BirthDate = dto.BirthDate;
        author.OriginCity = dto.OriginCity;
        author.Email = dto.Email;
        await _authorRepository.UpdateAsync(author);
    }

    public async Task DeleteAsync(int id)
    {
        await _authorRepository.DeleteAsync(id);
    }
}
