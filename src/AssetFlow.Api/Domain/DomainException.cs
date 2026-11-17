namespace AssetFlow.Api.Domain;

/// <summary>A business rule was broken (e.g. returning a loan that was never checked out).</summary>
public class DomainException(string message) : Exception(message);

/// <summary>The request clashes with the current state (e.g. the asset is already booked).</summary>
public class ConflictException(string message) : DomainException(message);

/// <summary>The requested record does not exist.</summary>
public class NotFoundException(string message) : Exception(message);
