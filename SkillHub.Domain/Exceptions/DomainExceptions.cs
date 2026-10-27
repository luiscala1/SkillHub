namespace SkillHub.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}

public class WorkshopCapacityExceededException : DomainException
{
    public WorkshopCapacityExceededException(string message = "El taller ha alcanzado su cupo máximo de participantes.")
        : base(message)
    {
    }
}

public class DuplicateEnrollmentException : DomainException
{
    public DuplicateEnrollmentException(string message = "El participante ya cuenta con una inscripción activa en este taller.")
        : base(message)
    {
    }
}

public class ScheduleConflictException : DomainException
{
    public ScheduleConflictException(string message = "El participante ya tiene otro taller activo que se cruza con este horario.")
        : base(message)
    {
    }
}

public class InvalidWorkshopDatesException : DomainException
{
    public InvalidWorkshopDatesException(string message) : base(message)
    {
    }
}

public class EnrollmentCancellationNotAllowedException : DomainException
{
    public EnrollmentCancellationNotAllowedException(string message) : base(message)
    {
    }
}
