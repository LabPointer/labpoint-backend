namespace Models;

public enum EAccountRole
{
    User,
    Admin,
    Root
}

public enum EShift
{
    Morning,
    Afternoon,
    Night
}

public enum EReportKind
{
    Spaces,
    Resources
}

public enum EReserveStatus
{
    Booking,
    Confirmed,
    Canceled,
    Rejected,
    Locked
}

/// <summary>Situação de uma sessão de uso (reserva x dia x turno).</summary>
public enum EUsageStatus
{
    /// <summary>Aguardando o check-in.</summary>
    Scheduled,
    /// <summary>Check-in feito; em uso até o check-out.</summary>
    CheckedIn,
    /// <summary>Check-out feito (pelo usuário ou por um admin).</summary>
    Completed,
    /// <summary>Sem check-in no prazo: os horários do turno foram liberados.</summary>
    Absent,
    /// <summary>Check-in feito, mas o prazo do check-out passou: só um admin fecha.</summary>
    CheckoutPending
}

/// <summary>Modalidade da turma informada no relatório de uso.</summary>
public enum EUsageModality
{
    Apprenticeship,
    Technical,
    Undergraduate,
    Postgraduate
}

/// <summary>Origem de um feriado: os três primeiros são gerados por ano; os extras são cadastrados pelo admin.</summary>
public enum EHolidaySource
{
    National,
    State,
    Municipal,
    Custom
}
