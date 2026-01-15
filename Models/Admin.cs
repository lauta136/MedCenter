namespace MedCenter.Models;

public class Admin
{
    public int Id;
    public DateOnly FechaIngreso = new DateOnly();
    public string Cargo{get;set;}
    public bool Activo{get;set;}

    public virtual Persona IdNavigation {get;set;}
}