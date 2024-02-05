namespace SteamQuery.Exceptions;

/// <summary>
/// The exception that is thrown when an unexpected byte is received.
/// </summary>
public class UnexpectedByteException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnexpectedByteException" /> class.
    /// </summary>
    public UnexpectedByteException(byte received, params byte[] bytes) : base($"{string.Join(", ", bytes.Select(b => b.ToString("X2")))} bytes are expected but instead received {received:X2}.")
    {
    }
}