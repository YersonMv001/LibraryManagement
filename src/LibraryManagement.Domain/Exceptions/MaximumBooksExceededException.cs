namespace LibraryManagement.Domain.Exceptions;

public class MaximumBooksExceededException : Exception
{
   
    private const string DefaultMessage = "No es posible registrar el libro, se alcanzó el máximo permitido.";

    public MaximumBooksExceededException() : base(DefaultMessage) { }
}