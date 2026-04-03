using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Application.DTOs;

public class AuthorCreateDto
{
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(200, ErrorMessage = "El nombre completo no puede superar los 200 caracteres.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    public DateTime BirthDate { get; set; }

    [Required(ErrorMessage = "La ciudad de procedencia es obligatoria.")]
    [StringLength(100, ErrorMessage = "La ciudad no puede superar los 100 caracteres.")]
    public string OriginCity { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
    [StringLength(150, ErrorMessage = "El correo no puede superar los 150 caracteres.")]
    public string Email { get; set; } = string.Empty;
}
