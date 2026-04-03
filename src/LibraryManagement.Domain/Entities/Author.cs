namespace LibraryManagement.Domain.Entities;

public class Author
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string OriginCity { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public ICollection<Book> Books { get; set; } = new List<Book>();
}
