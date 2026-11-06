namespace Models;

public enum EAccountRole
{
    User,
    Admin,
    Owner
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