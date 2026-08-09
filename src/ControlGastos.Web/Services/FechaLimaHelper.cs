namespace ControlGastos.Web.Services;

public static class FechaLimaHelper
{
    private static readonly TimeZoneInfo ZonaHorariaLima = ObtenerZonaHorariaLima();

    public static DateTimeOffset ConvertirALima(DateTime fechaLocal)
    {
        var sinZona = DateTime.SpecifyKind(fechaLocal, DateTimeKind.Unspecified);
        return new DateTimeOffset(sinZona, ZonaHorariaLima.GetUtcOffset(sinZona));
    }

    public static DateTime ConvertirDesdeUtc(DateTimeOffset fecha) =>
        TimeZoneInfo.ConvertTime(fecha, ZonaHorariaLima).DateTime;

    public static DateTime ObtenerAhoraLima() =>
        TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, ZonaHorariaLima).DateTime;

    private static TimeZoneInfo ObtenerZonaHorariaLima()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Lima"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time"); }
    }
}
