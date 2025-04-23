using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using SteamQuery.Enums;
using SteamQuery.Exceptions;
using SteamQuery.Extensions;
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
    public IPEndPoint IpEndPoint
    {
        get => _ipEndPoint;
        set
        {
            _ipEndPoint = value;

            Reestablish();
        }
    }

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
    /// <para>The default value is 30 seconds.</para>
    /// </summary>
    public TimeSpan SendTimeout
    {
        get;
        set
        {
            field = value;

            if (_udpClient != null && _udpClient.Client.Connected)
            {
                _udpClient.Client.SendTimeout = (int)value.TotalMilliseconds;
            }
        }
    } = TimeSpan.FromSeconds(30.0d);
    
    /// <summary>
    /// The timeout after which a query receive call should be faulted with a <see cref="TimeoutException"/> if it hasn't otherwise completed.
    /// <para>The default value is 30 seconds.</para>
    /// </summary>
    public TimeSpan ReceiveTimeout
    {
        get;
        set
        {
            field = value;

            if (_udpClient != null && _udpClient.Client.Connected)
            {
                _udpClient.Client.ReceiveTimeout = (int)value.TotalMilliseconds;
            }
        }
    } = TimeSpan.FromSeconds(30.0d);

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
        IpEndPoint = masterServerEndPoint.IpEndPoint;
    }
    
    /// <summary>
    /// Initialize a new instance of the <see cref="MasterServer"/> class.
    /// </summary>
    public MasterServer()
    {
    }
    
    // For comments, check the GamerServer class.
    /// <summary>
    /// Gets servers synchronously.
    /// </summary>
    public IEnumerable<MasterServerResponse> GetServers(MasterServerQueryFilters filters = null, SteamQueryRegion region = SteamQueryRegion.All)
    {
        var currentServerEndpoint = "0.0.0.0:0";
        
        var filterBytes = filters?.GetFilterBytes() ?? [ 0x00 ];

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

            if (response.Skip(results.Count - 6).All(b => b == 0x00))
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
    public async IAsyncEnumerable<MasterServerResponse> GetServersAsync(
        MasterServerQueryFilters filters = null,
        SteamQueryRegion region = SteamQueryRegion.All,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var currentServerEndpoint = "0.0.0.0:0";

        var filterBytes = filters?.GetFilterBytes() ?? [ 0x00 ];

        while (true)
        {
            byte[] request = [ PacketHeader, (byte)region, ..Encoding.UTF8.GetBytes(currentServerEndpoint + "\0"), ..filterBytes ];

            await _udpClient.SendAsync(request, request.Length).TimeoutAfterAsync(SendTimeout, cancellationToken);

            var response = (await _udpClient.ReceiveAsync().TimeoutAfterAsync(ReceiveTimeout, cancellationToken)).Buffer;

            var results = MasterServerResponseReader.ParseResponse(response);

            foreach (var result in results)
            {
                yield return result;
            }

            if (response.Skip(results.Count - 6).All(b => b == 0x00))
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
    /// Initializes socket.
    /// </summary>
    public void Initialize()
    {
        _udpClient = new UdpClient
        {
            Client =
            {
                SendTimeout = (int)SendTimeout.TotalMilliseconds,
                ReceiveTimeout = (int)ReceiveTimeout.TotalMilliseconds
            }
        };

        _udpClient.Connect(IpEndPoint);
    }
    
    /// <summary>
    /// Reestablishes socket.
    /// </summary>
    public void Reestablish()
    {
        Close();

        Initialize();
    }
    
    /// <summary>
    /// Closes socket.
    /// </summary>
    public void Close()
    {
        _udpClient?.Close();
    }
    
    /// <summary>
    /// Disposes the class.
    /// </summary>
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
            _udpClient?.Dispose();
            _udpClient = null;
        }

        _disposed = true;
    }
}