using LibraryManagement.Domain.Entities;

namespace LibraryManagement.Domain.Interfaces;

public interface IAuthorRepository
{
    Task<IEnumerable<Author>> GetAllAsync();
    Task<Author?> GetByIdAsync(int id);
    Task AddAsync(Author author);
    Task UpdateAsync(Author author);
    Task DeleteAsync(int id);
}
