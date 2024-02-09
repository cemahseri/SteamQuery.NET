namespace SteamQuery.Exceptions;

/// <summary>
/// The exception that is thrown when port is invalid.
/// </summary>
public class InvalidPortException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPortException"/> class.
    /// </summary>
    public InvalidPortException()
        : base("Port number was invalid.")
    {
    }
}