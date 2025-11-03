using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace MedCenter.Attributes;
public class NotWhiteSpace : ValidationAttribute //Creado para evitar que el campo sea rellenado con barra espaciadora
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
        {
            return new ValidationResult(ErrorMessage ?? "El campo no puede estar vacío o con solo espacios.");
        }

        return ValidationResult.Success;
    }
}

