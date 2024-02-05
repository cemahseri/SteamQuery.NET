using System.Net;
using System.Net.Sockets;
using SteamQuery.Enums;
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

    private static readonly byte[] PacketHeader     = [ 0xFF, 0xFF, 0xFF, 0xFF ];
    private static readonly byte[] DefaultChallenge = PacketHeader;

    private static readonly byte[] Informations = [ (byte)PayloadHeader.Informations, .."Source Engine Query\0"u8.ToArray() ];
    private static readonly byte[] Players      = [ (byte)PayloadHeader.Players,      ..DefaultChallenge ];
    private static readonly byte[] Rules        = [ (byte)PayloadHeader.Rules,        ..DefaultChallenge ];

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
    private byte[] ExecuteQuery(byte[] request)
    {
        _udpClient.Send((byte[])[ ..PacketHeader, ..request ]);

        var response = _udpClient.Receive(ref _ipEndPoint);
        if (response.ReadResponsePayloadHeader() == PayloadHeader.Challenge)
        {
            return ExecuteQuery(GetRequestWithChallenge(request, response));
        }

        return response;
    }

    //TODO SendTimeout and ReceiveTimeout only works with synchronous calls. Make them work with asynchronous calls aswell.
    private async Task<byte[]> ExecuteQueryAsync(byte[] request)
    {
        await _udpClient.SendAsync((byte[])[ ..PacketHeader, ..request ]);
        
        var response = (await _udpClient.ReceiveAsync()).Buffer;
        if (response.ReadResponsePayloadHeader() == PayloadHeader.Challenge)
        {
            return await ExecuteQueryAsync(GetRequestWithChallenge(request, response));
        }

        return response;
    }

    private byte[] GetRequestWithChallenge(byte[] request, IEnumerable<byte> response)
    {
        return request.ReadRequestPayloadHeader() switch
        {
            PayloadHeader.Informations => [ ..request, ..response.TakeLast(4) ],
            PayloadHeader.Players
                or PayloadHeader.Rules => [ request[0], ..response.TakeLast(4) ]
        };
    }

    /// <summary>
    /// Closes socket.
    /// </summary>
    public void Close()
    {
        _udpClient.Close();
    }

    /// <summary>
    /// Disposes the class.
    /// </summary>
    public void Dispose() => Close();
}