using LibraryManagement.Domain.Entities;

namespace LibraryManagement.Domain.Interfaces;

public interface IBookRepository
{
    Task<IEnumerable<Book>> GetAllAsync();
    Task<Book?> GetByIdAsync(int id);
    Task AddAsync(Book book);
    Task UpdateAsync(Book book);
    Task DeleteAsync(int id);
    Task<int> CountAsync();
}
