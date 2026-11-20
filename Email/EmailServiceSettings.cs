namespace Email;

/// <summary>Seção <c>EmailService</c> do appsettings: ritmo da fila e horário dos avisos de check-in/check-out.</summary>
public sealed class EmailServiceSettings
{
    /// <summary>
    /// Intervalo máximo entre varreduras da fila. O NOTIFY do backend acorda o serviço na hora; a varredura cobre
    /// avisos perdidos (ex.: serviço reiniciando) e as novas tentativas agendadas.
    /// </summary>
    public int OutboxPollSeconds { get; set; } = 30;

    /// <summary>E-mails processados por vez.</summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>Tentativas antes de marcar o e-mail como falho (status 2).</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>De quanto em quanto tempo o agendador procura sessões que precisam do aviso de check-in/check-out.</summary>
    public int UsageScanSeconds { get; set; } = 60;

    /// <summary>
    /// Quantos minutos antes de a janela de check-in abrir o aviso sai. A janela abre 15 min antes do 1º horário do
    /// turno; com o padrão (15) o e-mail chega 30 min antes da aula.
    /// </summary>
    public int CheckInEmailMinutesBeforeWindow { get; set; } = 15;

    /// <summary>Quantos minutos antes de a janela de check-out abrir (início do último horário) o aviso sai.</summary>
    public int CheckOutEmailMinutesBeforeWindow { get; set; } = 0;
}
