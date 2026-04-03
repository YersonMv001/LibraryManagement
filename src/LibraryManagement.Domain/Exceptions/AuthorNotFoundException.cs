namespace LibraryManagement.Domain.Exceptions;

public class AuthorNotFoundException : Exception
{
    
    private const string DefaultMessage = "El autor no está registrado";

    // Constructor por defecto que usa el mensaje fijo
    public AuthorNotFoundException() : base(DefaultMessage) { }

}