using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Application.DTOs;

public class BookCreateDto
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(200, ErrorMessage = "El título no puede superar los 200 caracteres.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "El año es obligatorio.")]
    [Range(1000, 9999, ErrorMessage = "El año debe ser un valor válido.")]
    public int Year { get; set; }

    [Required(ErrorMessage = "El género es obligatorio.")]
    [StringLength(100, ErrorMessage = "El género no puede superar los 100 caracteres.")]
    public string Genre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de páginas es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El número de páginas debe ser mayor a cero.")]
    public int NumberOfPages { get; set; }

    [Required(ErrorMessage = "El autor es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe especificarse un autor válido.")]
    public int AuthorId { get; set; }
}
