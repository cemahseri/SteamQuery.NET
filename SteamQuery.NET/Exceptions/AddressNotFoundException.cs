namespace SteamQuery.Exceptions;

/// <summary>
/// The exception that is thrown when IP address or host could not be found.
/// </summary>
public class AddressNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddressNotFoundException" /> class.
    /// </summary>
    public AddressNotFoundException() : base("IP address or host could not be found.")
    {
    }
}