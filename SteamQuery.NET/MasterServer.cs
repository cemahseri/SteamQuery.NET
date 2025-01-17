using System.Net;
using System.Net.Sockets;
using System.Text;
using SteamQuery.Enums;
using SteamQuery.Exceptions;
using SteamQuery.Models;

namespace SteamQuery;

/// <summary>
/// Master server class.
/// </summary>
/// <remarks>Thread-safe.</remarks>
public class MasterServer : IDisposable
{
    /// <summary>
    /// IP endpoint of the server.
    /// </summary>
    public IPEndPoint IpEndPoint => _ipEndPoint;

    /// <summary>
    /// IP address of the server.
    /// </summary>
    public IPAddress IpAddress => IpEndPoint.Address;

    /// <summary>
    /// Port number of the server.
    /// </summary>
    public int Port => IpEndPoint.Port;
    
    /// <summary>
    /// The timeout after which a connection or query call should be faulted with a <see cref="TimeoutException"/> if it hasn't otherwise completed.
    /// <para>The default value is <see cref="Timeout.InfiniteTimeSpan"/>.</para>
    /// </summary>
    public TimeSpan SendTimeout
    {
        get;
        set
        {
            field = value;
            Reestablish();
        }
    } = Timeout.InfiniteTimeSpan;
    
    /// <summary>
    /// The timeout after which a query receive call should be faulted with a <see cref="TimeoutException"/> if it hasn't otherwise completed.
    /// <para>The default value is <see cref="Timeout.InfiniteTimeSpan"/>.</para>
    /// </summary>
    public TimeSpan ReceiveTimeout
    {
        get;
        set
        {
            field = value;
            Reestablish();
        }
    } = Timeout.InfiniteTimeSpan;

    private UdpClient _udpClient;
    private IPEndPoint _ipEndPoint;
    
    private bool _disposed;
    
    private const byte PacketHeader = 0x31;

    /// <summary>
    /// Initialize a new instance of the <see cref="MasterServer"/> class with given master server IP endpoint.
    /// </summary>
    /// <param name="masterServerEndPoint">Master server endpoint.</param>
    /// <exception cref="ArgumentNullException">Thrown when endPoint is null or empty.</exception>
    /// <exception cref="FormatException">Thrown when endPoint is not in correct format.</exception>
    /// <exception cref="InvalidPortException">Thrown when port is not valid.</exception>
    /// <exception cref="AddressNotFoundException">Thrown when IP address hostname is not found.</exception>
    /// <exception cref="SocketException">Thrown when host is known.</exception>
    public MasterServer(MasterServerEndPoint masterServerEndPoint)
    {
        _ipEndPoint = masterServerEndPoint.IpEndPoint;

        Initialize();
    }
    
    // For comments, check the GamerServer class.
    /// <summary>
    /// Gets servers synchronously.
    /// </summary>
    public IEnumerable<MasterServerResponse> GetServers(SteamQueryRegion region = SteamQueryRegion.All, string filters = null)
    {
        var currentServerEndpoint = "0.0.0.0:0";

        var filterBytes = Encoding.UTF8.GetBytes(filters + "\0");

        while (true)
        {
            byte[] request = [ PacketHeader, (byte)region, ..Encoding.UTF8.GetBytes(currentServerEndpoint + "\0"), ..filterBytes ];
            
            _udpClient.Send(request, request.Length);

            var response = _udpClient.Receive(ref _ipEndPoint);

            var results = MasterServerResponseReader.ParseResponse(response);

            foreach (var result in results)
            {
                yield return result;
            }

            if (response[^6..].All(b => b == 0x00))
            {
                yield break;
            }

            var lastServer = results.LastOrDefault();
            if (lastServer == default)
            {
                yield break;
            }

            currentServerEndpoint = $"{lastServer.IpAddress}:{lastServer.Port}";
        }
    }

    /// <summary>
    /// Gets servers asynchronously.
    /// </summary>
    public async IAsyncEnumerable<MasterServerResponse> GetServersAsync(SteamQueryRegion region = SteamQueryRegion.All, string filters = null, CancellationToken cancellationToken = default)
    {
        var currentServerEndpoint = "0.0.0.0:0";

        var filterBytes = Encoding.UTF8.GetBytes(filters + "\0");

        while (true)
        {
            byte[] request = [ PacketHeader, (byte)region, ..Encoding.UTF8.GetBytes(currentServerEndpoint + "\0"), ..filterBytes ];

            await _udpClient.SendAsync(request, request.Length).WaitAsync(SendTimeout, cancellationToken);

            var response = (await _udpClient.ReceiveAsync().WaitAsync(ReceiveTimeout, cancellationToken)).Buffer;

            var results = MasterServerResponseReader.ParseResponse(response);

            foreach (var result in results)
            {
                yield return result;
            }

            if (response[^6..].All(b => b == 0x00))
            {
                yield break;
            }

            var lastServer = results.LastOrDefault();
            if (lastServer == default)
            {
                yield break;
            }

            currentServerEndpoint = $"{lastServer.IpAddress}:{lastServer.Port}";
        }
    }

    public void Initialize()
    {
        _udpClient = new UdpClient
        {
            Client =
            {
                SendTimeout = (int)SendTimeout.TotalSeconds,
                ReceiveTimeout = (int)ReceiveTimeout.TotalSeconds
            }
        };

        _udpClient.Client.Connect(IpEndPoint);
    }

    public void Reestablish()
    {
        Close();

        Initialize();
    }

    public void Close()
    {
        _udpClient.Close();
    }

    public void Dispose()
    {
        Dispose(true);

        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _udpClient.Dispose();
            _udpClient = null;
        }

        _disposed = true;
    }
}