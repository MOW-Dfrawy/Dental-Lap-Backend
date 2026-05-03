namespace DentalLab.Application.Common;

public class NotFoundException : Exception
{
    public NotFoundException(string entity, object id)
        : base($"{entity} with id '{id}' was not found.") { }

    public NotFoundException(string message) : base(message) { }
}

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}
