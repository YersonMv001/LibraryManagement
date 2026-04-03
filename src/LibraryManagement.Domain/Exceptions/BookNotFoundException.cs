namespace LibraryManagement.Domain.Exceptions;

public class BookNotFoundException : Exception
{
    private const string DefaultMessage = "El libro no está registrado";

    public BookNotFoundException() : base(DefaultMessage) { }
}