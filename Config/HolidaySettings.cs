namespace Services;

/// <summary>Seção "Holidays": feriados municipais fixos (a BrasilAPI só tem nacionais e estaduais).</summary>
public class HolidaySettings
{
    /// <summary>UF consultada na BrasilAPI.</summary>
    public string State { get; set; } = "GO";

    public List<FixedHoliday> Municipal { get; set; } = [];
}

/// <summary>Feriado de data fixa. <see cref="Date"/> no formato "MM-dd".</summary>
public class FixedHoliday
{
    public string Date { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
