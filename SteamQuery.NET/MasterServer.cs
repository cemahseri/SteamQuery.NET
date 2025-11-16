using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using SteamQuery.Enums;
using SteamQuery.Exceptions;
using SteamQuery.Models;

namespace SteamQuery;

/// <summary>
/// Master server class.
/// </summary>
public class MasterServer : IDisposable
{
    /// <summary>
    /// IP endpoint of the server.
    /// </summary>
    public MasterServerEndPoint MasterServerEndPoint { get; set; }

    /// <summary>
    /// IP address of the server.
    /// </summary>
    public IPAddress IpAddress
    {
        get => MasterServerEndPoint.IpEndPoint.Address;
        set => MasterServerEndPoint.IpEndPoint.Address = value;
    }

    /// <summary>
    /// Port number of the server.
    /// </summary>
    public int Port
    {
        get => MasterServerEndPoint.IpEndPoint.Port;
        set => MasterServerEndPoint.IpEndPoint.Port = value;
    }
    
    /// <summary>
    /// The timeout after which a connection or query call should be faulted with a <see cref="TimeoutException"/> if it hasn't otherwise completed.
    /// <para>The default value is 30 seconds.</para>
    /// </summary>
    public TimeSpan SendTimeout { get; set; } = TimeSpan.FromSeconds(5.0d);
    
    /// <summary>
    /// The timeout after which a query receive call should be faulted with a <see cref="TimeoutException"/> if it hasn't otherwise completed.
    /// <para>The default value is 30 seconds.</para>
    /// </summary>
    public TimeSpan ReceiveTimeout { get; set; } = TimeSpan.FromSeconds(5.0d);
    
    private readonly Socket _socket = new(SocketType.Dgram, ProtocolType.Udp);

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
        MasterServerEndPoint = masterServerEndPoint;
    }
    
    /// <summary>
    /// Initialize a new instance of the <see cref="MasterServer"/> class.
    /// </summary>
    public MasterServer()
    {
    }
    
    /// <summary>
    /// Gets servers asynchronously.
    /// </summary>
    public async IAsyncEnumerable<MasterServerResponse> GetServersAsync(
        MasterServerQueryFilters? filters = null,
        SteamQueryRegion region = SteamQueryRegion.All,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var currentServerEndpoint = "0.0.0.0:0\0";

        var filterBytes = filters != null ? filters.GetFilterBytes() : [ 0x00 ];
        
        while (true)
        {
            using var sendCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            sendCancellationTokenSource.CancelAfter(SendTimeout);
            
            byte[] request = [ PacketHeader, (byte)region, ..Encoding.UTF8.GetBytes(currentServerEndpoint), ..filterBytes ];

            await _socket.SendToAsync(request, MasterServerEndPoint.IpEndPoint, sendCancellationTokenSource.Token);
            
            using var receiveCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            receiveCancellationTokenSource.CancelAfter(ReceiveTimeout);

            byte[] response;

            var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
            try
            {
                var receiveFromResult = await _socket.ReceiveFromAsync(buffer, MasterServerEndPoint.IpEndPoint, receiveCancellationTokenSource.Token);
                response = buffer.AsSpan(0, receiveFromResult.ReceivedBytes).ToArray();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            var results = MasterServerResponseReader.ParseResponse(response);

            foreach (var result in results)
            {
                yield return result;
            }

            if (response.TakeLast(6).All(b => b == 0x00))
            {
                yield break;
            }

            var lastServer = results.LastOrDefault();
            if (lastServer == default)
            {
                yield break;
            }

            currentServerEndpoint = $"{lastServer.IpAddress}:{lastServer.Port}\0";
        }
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
            _socket.Dispose();
        }

        _disposed = true;
    }
}