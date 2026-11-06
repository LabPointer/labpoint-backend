namespace Backend.Services;

/// <summary>
/// Hora oficial de Brasília. O Brasil não tem mais horário de verão, então um deslocamento fixo evita depender do
/// tzdata do servidor. Datas e horários das reservas (DateOnly/TimeOnly) são sempre nesse fuso.
/// </summary>
public static class BrazilClock
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(-3);

    public static DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <summary>Dia atual em Brasília.</summary>
    public static DateOnly Today => DateOnly.FromDateTime((DateTime.UtcNow + Offset).Date);

    /// <summary>Instante (em UTC, como o Npgsql exige para timestamptz) de um dia e horário de Brasília.</summary>
    public static DateTimeOffset At(DateOnly date, TimeOnly time) =>
        new DateTimeOffset(date.ToDateTime(time), Offset).ToUniversalTime();

    /// <summary>Instante convertido para o horário de Brasília (para exibir em e-mails).</summary>
    public static DateTimeOffset ToLocal(DateTimeOffset instant) => instant.ToOffset(Offset);
}
