using System.Text;

namespace SteamQuery.Exceptions;

/// <summary>
/// The exception that is thrown when an unexpected byte is received.
/// </summary>
public class UnexpectedByteException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnexpectedByteException"/> class.
    /// </summary>
    public UnexpectedByteException(byte received, byte[] bytes)
        : base(FormatMessage(received, bytes))
    {
    }

    private static string FormatMessage(byte received, byte[] bytes)
    {
        var stringBuilder = new StringBuilder(bytes.Length * 3);
        for (var i = 0; i < bytes.Length; i++)
        {
            if (i > 0)
            {
                stringBuilder.Append(", ");
            }

            stringBuilder.Append(bytes[i].ToString("X2"));
        }

        return $"{stringBuilder} bytes are expected but instead received {received:X2}.";
    }
}
