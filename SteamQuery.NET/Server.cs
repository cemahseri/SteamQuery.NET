using System.Net;
using System.Net.Sockets;
using SteamQuery.Exceptions;
using SteamQuery.Helpers;
using SteamQuery.Models;

namespace SteamQuery;

/// <summary>
/// Server class holds information related to a game server in it.
/// </summary>
/// <remarks>Thread-safe.</remarks>
public sealed class Server : IDisposable
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
    /// Gets or sets a value that specifies the amount of time in milliseconds, after which a connection or query call will time out.
    /// </summary>
    /// <returns>The time-out value, in milliseconds. If you set the property with a value between 1 and 499, the value will be changed to 500 - because how it is implemented in <see cref="Socket"/> class. The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period.</returns>
    public int SendTimeout { get; set; } = 5000;
    /// <summary>
    /// Gets or sets a value that specifies the amount of time in milliseconds, after which a query receive call will time out.
    /// </summary>
    /// <returns>The time-out value, in milliseconds. The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period.</returns>
    public int ReceiveTimeout { get; set; } = 5000;

    private readonly UdpClient _udpClient;
    private IPEndPoint _ipEndPoint;

    private static readonly byte[] Informations = [ 0xFF, 0xFF, 0xFF, 0xFF, 0x54, 0x53, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x20, 0x45, 0x6E, 0x67, 0x69, 0x6E, 0x65, 0x20, 0x51, 0x75, 0x65, 0x72, 0x79, 0x00 ];
    private static readonly byte[] Players = [ 0xFF, 0xFF, 0xFF, 0xFF, 0x55, 0xFF, 0xFF, 0xFF, 0xFF ];
    private static readonly byte[] Rules = [ 0xFF, 0xFF, 0xFF, 0xFF, 0x56, 0xFF, 0xFF, 0xFF, 0xFF ];

    /// <summary>
    /// Initialize with given endpoint - in <see cref="string"/> type.
    /// </summary>
    /// <param name="endPoint">IP endpoint. Seperating IP address and port number with colon is required. Example: 127.0.0.1:1337</param>
    public Server(string endPoint) : this(IpHelper.CreateIpEndPoint(endPoint))
    {
    }

    /// <summary>
    /// Initialize with given IP address <i>(in <see cref="string"/> type)</i> and port number.
    /// </summary>
    /// <param name="ip">IP address.</param>
    /// <param name="port">Port number.</param>
    public Server(string ip, int port) : this(IpHelper.CreateIpEndPoint(ip, port))
    {
    }

    /// <summary>
    /// Initialize with given IP address <i>(in <see cref="IPAddress"/> type)</i> and port number.
    /// </summary>
    /// <param name="ip">IP address.</param>
    /// <param name="port">Port number.</param>
    public Server(IPAddress ip, int port) : this(new IPEndPoint(ip, port))
    {
    }

    /// <summary>
    /// Initialize with given IP endpoint.
    /// </summary>
    /// <param name="ipEndPoint">IP endpoint.</param>
    public Server(IPEndPoint ipEndPoint)
    {
        _ipEndPoint = ipEndPoint;

        _udpClient = new UdpClient
        {
            Client =
            {
                SendTimeout = SendTimeout,
                ReceiveTimeout = ReceiveTimeout
            }
        };

        _udpClient.Client.Connect(IpEndPoint);
    }

    /// <summary>
    /// Gets information synchronously.
    /// </summary>
    public Informations GetInformations() => ResponseReader.ParseInformation(ExecuteQuery(Informations));
    /// <summary>
    /// Gets players synchronously.
    /// </summary>
    public List<Player> GetPlayers() => ResponseReader.ParsePlayers(ExecuteQuery(Players));
    /// <summary>
    /// Gets rules synchronously.
    /// </summary>
    public List<Rule> GetRules() => ResponseReader.ParseRules(ExecuteQuery(Rules));

    /// <summary>
    /// Gets information asynchronously.
    /// </summary>
    public async Task<Informations> GetInformationsAsync() => ResponseReader.ParseInformation(await ExecuteQueryAsync(Informations));
    /// <summary>
    /// Gets players asynchronously.
    /// </summary>
    public async Task<List<Player>> GetPlayersAsync() => ResponseReader.ParsePlayers(await ExecuteQueryAsync(Players));
    /// <summary>
    /// Gets rules asynchronously.
    /// </summary>
    public async Task<List<Rule>> GetRulesAsync() => ResponseReader.ParseRules(await ExecuteQueryAsync(Rules));

    // The main reason that I seperated synchronous method and the asynchronous method is, there is no benefit writing synchronous method then running it in a Task.
    // So, instead of that, I had seperated two methods and used asynchronous methods on the asynchronous method, such as UdpClient.SendAsync (instead of Send), UdpClient.ReceiveAsync (Receive), etc.
    private byte[] ExecuteQuery(byte[] requestPayload)
    {
        _udpClient.Send(requestPayload);
        var response = _udpClient.Receive(ref _ipEndPoint);

        var requestPayloadHeader = requestPayload.ReadPayloadHeader();
        switch (requestPayloadHeader)
        {
            case 0x54: // Informations
                return response;
            case 0x55: // Players
            case 0x56: // Rules
                response[4] = requestPayloadHeader;
                break;
            default:
                throw new UnexpectedByteException(requestPayloadHeader, 0x54, 0x55, 0x56);
        }

        var index = 5;
        for (var i = 5; i <= 8; i++)
        {
            response[i] = response.ReadByte(ref index);
        }

        _udpClient.Send(response);
        return _udpClient.Receive(ref _ipEndPoint);
    }

    //TODO SendTimeout and ReceiveTimeout only works with synchronous calls. Make them work with asynchronous calls aswell.
    private async Task<byte[]> ExecuteQueryAsync(byte[] requestPayload)
    {
        await _udpClient.SendAsync(requestPayload);
        var response = (await _udpClient.ReceiveAsync()).Buffer;

        var requestPayloadHeader = requestPayload.ReadPayloadHeader();
        switch (requestPayloadHeader)
        {
            case 0x54: // Informations
                return response;
            case 0x55: // Players
            case 0x56: // Rules
                response[4] = requestPayloadHeader;
                break;
            default:
                throw new UnexpectedByteException(requestPayloadHeader, 0x54, 0x55, 0x56);
        }

        var index = 5;
        for (var i = 5; i <= 8; i++)
        {
            response[i] = response.ReadByte(ref index);
        }

        await _udpClient.SendAsync(response);
        return (await _udpClient.ReceiveAsync()).Buffer;
    }

    /// <summary>
    /// Closes socket.
    /// </summary>
    public void Close() => _udpClient.Close();

    /// <summary>
    /// Disposes the class.
    /// </summary>
    public void Dispose() => Close();
}