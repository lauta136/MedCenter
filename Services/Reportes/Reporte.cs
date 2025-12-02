namespace MedCenter.Services.Reportes;

/// <summary>
/// The 'Product' class in Builder Pattern
/// Represents a complex report object being built
/// </summary>
public class Reporte(TipoReporte tipoReporte)
{
    private readonly Dictionary<ParteReporte, object> partes = [];
    private readonly TipoReporte tipoReporte = tipoReporte;
    
    public object this[ParteReporte key]
    {
        get => partes.ContainsKey(key) ? partes[key] : null!;
        set => partes[key] = value; 
    }
    
    /// <summary>
    /// Gets the generated report as byte array
    /// </summary>
    public byte[] GetBytes()
    {
        if (this[ParteReporte.Data] is byte[] bytes)
        {
            return bytes;
        }
        throw new InvalidOperationException("El reporte no contiene datos válidos");
    }
    
    /// <summary>
    /// Display report information (for debugging)
    /// </summary>
    public void Show()
    {
        Console.WriteLine("\n---------------------------");
        Console.WriteLine($"Tipo de Reporte: {tipoReporte}");
        Console.WriteLine($" Header     : {this[ParteReporte.Header]}");
        Console.WriteLine($" Data       : {(this[ParteReporte.Data] as byte[])?.Length ?? 0} bytes");
        Console.WriteLine($" Statistics : {this[ParteReporte.Statistics]}");
        Console.WriteLine($" Footer     : {this[ParteReporte.Footer]}");
        Console.WriteLine($" Format     : {this[ParteReporte.Format]}");
    }
}