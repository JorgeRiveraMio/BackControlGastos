namespace ControlGastos.Infrastructure.Data;

public static class FechaLima
{
    private static readonly TimeZoneInfo ZonaHoraria = ObtenerZonaHoraria();

    public static (DateTimeOffset InicioUtc, DateTimeOffset FinUtc) ObtenerLimitesMensualesUtc(int anio, int mes)
    {
        var inicioLima = new DateTime(anio, mes, 1, 0, 0, 0, DateTimeKind.Unspecified);
        return (ConvertirAUtc(inicioLima), ConvertirAUtc(inicioLima.AddMonths(1)));
    }

    private static DateTimeOffset ConvertirAUtc(DateTime fechaLima) =>
        new(TimeZoneInfo.ConvertTimeToUtc(fechaLima, ZonaHoraria));

    private static TimeZoneInfo ObtenerZonaHoraria()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Lima"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time"); }
    }
}
