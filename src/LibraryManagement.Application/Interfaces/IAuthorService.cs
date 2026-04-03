using LibraryManagement.Application.DTOs;

namespace LibraryManagement.Application.Interfaces;

public interface IAuthorService
{
    Task<IEnumerable<AuthorDto>> GetAllAsync();
    Task<AuthorDto?> GetByIdAsync(int id);
    Task<AuthorDto> CreateAsync(AuthorCreateDto dto);
    Task UpdateAsync(int id, AuthorCreateDto dto);
    Task DeleteAsync(int id);
}
