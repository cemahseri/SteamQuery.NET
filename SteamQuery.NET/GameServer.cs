using System.Net;
using System.Net.Sockets;
using ICSharpCode.SharpZipLib.BZip2;
using SteamQuery.Enums;
using SteamQuery.Exceptions;
using SteamQuery.Extensions;
using SteamQuery.Helpers;
using SteamQuery.Models;

namespace SteamQuery;

/// <summary>
/// Game server class holds information related to a game server in it.
/// </summary>
/// <remarks>Thread-safe.</remarks>
public class GameServer : IDisposable
{
    /// <summary>
    /// Information of the server.
    /// <para>Available after calling <see cref="GetInformationAsync"/> or <see cref="PerformQueryAsync"/> - or obviously their synchronous versions.</para>
    /// </summary>
    public SteamQueryInformation Information { get; private set; } = new();

    /// <summary>
    /// Players of the server.
    /// <para>Available after calling <see cref="GetPlayersAsync"/> or <see cref="PerformQueryAsync"/> - or obviously their synchronous versions.</para>
    /// </summary>
    public IReadOnlyList<SteamQueryPlayer> Players { get; private set; } = [];

    /// <summary>
    /// Rules of the server.
    /// <para>Available after calling <see cref="GetRulesAsync"/> or <see cref="PerformQueryAsync"/> - or obviously their synchronous versions.</para>
    /// </summary>
    public IReadOnlyList<SteamQueryRule> Rules { get; private set; } = [];

    /// <summary>
    /// If the server is using compression before sending response.
    /// <para>If the server is using Source protocol, this property will be available after calling any query that will return multi-packet response.</para>
    /// <para>Steam uses a packet size of up to 1400 bytes + IP/UDP headers. If a request or response needs more packets for the data it starts the packets with an additional header.</para>
    /// </summary>
    public bool? IsUsingCompression { get; private set; }

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
    /// <returns>The time-out value, in milliseconds. If you set the property with a value between 1 and 499, the value will be changed to 500 - because how it is implemented in <see cref="Socket"/> class.
    /// <para>The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period.</para></returns>
    public int SendTimeout
    {
        get;
        set
        {
            if (value < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value == -1)
            {
                value = 0;
            }

            field = value;
            Reestablish();
        }
    }

    /// <summary>
    /// Gets or sets a value that specifies the amount of time in milliseconds, after which a query receive call will time out.
    /// </summary>
    /// <returns>The time-out value, in milliseconds. The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period.</returns>
    public int ReceiveTimeout
    {
        get;
        set
        {
            if (value < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value == -1)
            {
                value = 0;
            }

            field = value;
            Reestablish();
        }
    }

    private UdpClient _udpClient;
    private IPEndPoint _ipEndPoint;

    private bool _disposed;

    private static readonly byte[] PacketHeader     = [ 0xFF, 0xFF, 0xFF, 0xFF ];
    private static readonly byte[] DefaultChallenge = PacketHeader;

    private static readonly byte[] InformationRequest = [ (byte)PayloadIdentifier.Information, .."Source Engine Query\0"u8 ];
    private static readonly byte[] PlayersRequest     = [ (byte)PayloadIdentifier.Players,     ..DefaultChallenge ];
    private static readonly byte[] RulesRequest       = [ (byte)PayloadIdentifier.Rules,       ..DefaultChallenge ];

    /// <summary>
    /// Initialize a new instance of the <see cref="GameServer"/> class with given endpoint - in <see cref="string"/> type.
    /// </summary>
    /// <param name="endPoint">IP endpoint. Separating IP address (or hostname) and port number with colon is required.
    ///     <para>Example 1: 127.0.0.1:1337</para>
    ///     <para>Example 2: localhost:1337</para>
    /// </param>
    /// <param name="addressFamily">Target address family to be extracted from hostname.
    ///     <para>This has no effect if IP address is used, instead of hostname.</para>
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when endPoint is null or empty.</exception>
    /// <exception cref="FormatException">Thrown when endPoint is not in correct format.</exception>
    /// <exception cref="InvalidPortException">Thrown when port is not valid.</exception>
    /// <exception cref="AddressNotFoundException">Thrown when IP address hostname is not found.</exception>
    /// <exception cref="SocketException">Thrown when host is known.</exception>
    public GameServer(string endPoint, AddressFamily addressFamily = AddressFamily.InterNetwork) : this(IpHelper.CreateIpEndPoint(endPoint, addressFamily))
    {
    }

    /// <summary>
    /// Initialize a new instance of the <see cref="GameServer"/> class with given IP address <i>(in <see cref="string"/> type)</i> and port number.
    /// </summary>
    /// <param name="hostNameOrIpAddress">IP address or hostname.
    ///     <para>Example 1: 127.0.0.1</para>
    ///     <para>Example 2: localhost</para></param>
    /// <param name="port">Port number.</param>
    /// <param name="addressFamily">Target address family to be extracted from hostname.
    ///     <para>This has no effect if IP address is used, instead of hostname.</para>
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when hostNameOrIpAddress is null or empty.</exception>
    /// <exception cref="AddressNotFoundException">Thrown when IP address or hostname is not found.</exception>
    /// <exception cref="SocketException">Thrown when host is known.</exception>
    public GameServer(string hostNameOrIpAddress, int port, AddressFamily addressFamily = AddressFamily.InterNetwork) : this(IpHelper.CreateIpEndPoint(hostNameOrIpAddress, port, addressFamily))
    {
    }

    /// <summary>
    /// Initialize a new instance of the <see cref="GameServer"/> class with given IP address <i>(in <see cref="IPAddress"/> type)</i> and port number.
    /// </summary>
    /// <param name="ip">IP address.</param>
    /// <param name="port">Port number.</param>
    public GameServer(IPAddress ip, int port) : this(new IPEndPoint(ip, port))
    {
    }

    /// <summary>
    /// Initialize a new instance of the <see cref="GameServer"/> class with given IP endpoint.
    /// </summary>
    /// <param name="ipEndPoint">IP endpoint.</param>
    public GameServer(IPEndPoint ipEndPoint)
    {
        _ipEndPoint = ipEndPoint;

        Initialize();
    }

    /// <summary>
    /// Gets information synchronously.
    /// </summary>
    public SteamQueryInformation GetInformation()
    {
        return Information = ResponseReader.ParseInformation(ExecuteQuery(InformationRequest));
    }

    /// <summary>
    /// Gets information asynchronously.
    /// </summary>
    public async Task<SteamQueryInformation> GetInformationAsync(CancellationToken cancellationToken = default)
    {
        return Information = ResponseReader.ParseInformation(await ExecuteQueryAsync(InformationRequest, cancellationToken));
    }

    /// <summary>
    /// Gets players synchronously.
    /// </summary>
    public IReadOnlyList<SteamQueryPlayer> GetPlayers()
    {
        return Players = ResponseReader.ParsePlayers(ExecuteQuery(PlayersRequest));
    }

    /// <summary>
    /// Gets players asynchronously.
    /// </summary>
    public async Task<IReadOnlyList<SteamQueryPlayer>> GetPlayersAsync(CancellationToken cancellationToken = default)
    {
        return Players = ResponseReader.ParsePlayers(await ExecuteQueryAsync(PlayersRequest, cancellationToken));
    }

    /// <summary>
    /// Gets rules synchronously.
    /// </summary>
    public IReadOnlyList<SteamQueryRule> GetRules()
    {
        return Rules = ResponseReader.ParseRules(ExecuteQuery(RulesRequest));
    }

    /// <summary>
    /// Gets rules asynchronously.
    /// </summary>
    public async Task<IReadOnlyList<SteamQueryRule>> GetRulesAsync(CancellationToken cancellationToken = default)
    {
        return Rules = ResponseReader.ParseRules(await ExecuteQueryAsync(RulesRequest, cancellationToken));
    }

    /// <summary>
    /// Performs given queries synchronously.
    /// </summary>
    /// <param name="queries">Queries to be performed.</param>
    public void PerformQuery(SteamQueryA2SQuery queries = SteamQueryA2SQuery.All)
    {
        if (queries.HasFlag(SteamQueryA2SQuery.Information))
        {
            GetInformation();
        }

        if (queries.HasFlag(SteamQueryA2SQuery.Players))
        {
            GetPlayers();
        }

        if (queries.HasFlag(SteamQueryA2SQuery.Rules))
        {
            GetRules();
        }
    }

    /// <summary>
    /// Performs given queries asynchronously.
    /// </summary>
    /// <param name="queries">Queries to be performed.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    public async Task PerformQueryAsync(SteamQueryA2SQuery queries = SteamQueryA2SQuery.All, CancellationToken cancellationToken = default)
    {
        if (queries.HasFlag(SteamQueryA2SQuery.Information))
        {
            await GetInformationAsync(cancellationToken);
        }

        if (queries.HasFlag(SteamQueryA2SQuery.Players))
        {
            await GetPlayersAsync(cancellationToken);
        }

        if (queries.HasFlag(SteamQueryA2SQuery.Rules))
        {
            await GetRulesAsync(cancellationToken);
        }
    }

    // For comments, check the asynchronous method.
    private byte[] ExecuteQuery(byte[] request)
    {
        _udpClient.Send([ ..PacketHeader, ..request ], PacketHeader.Length + request.Length);

        var response = _udpClient.Receive(ref _ipEndPoint);

        var packetHeader = response.ReadPacketIdentifier();
        if (packetHeader == PacketIdentifier.Split)
        {
            var multiPacketHeader = response.ReadMultiPacketHeader();

            if (!multiPacketHeader.IsGoldSourceServer)
            {
                IsUsingCompression = multiPacketHeader.IsCompressed;

                if (multiPacketHeader.IsCompressed)
                {
                    throw new NotImplementedException("Compressed packets not implemented yet. Please report server IP address and port, so I can test it.");
                }
            }

            var payloadIndex = multiPacketHeader.IsGoldSourceServer switch
            {
                false when multiPacketHeader.IsCompressed => 20,
                false when !multiPacketHeader.IsCompressed => 12,
                _ => 9
            };

            response = response.Skip(payloadIndex).ToArray();

            var remainingPackets = new List<byte[]>(multiPacketHeader.TotalPackets);

            for (var i = 1; i < multiPacketHeader.TotalPackets; i++)
            {
                remainingPackets.Add(_udpClient.Receive(ref _ipEndPoint));
            }

            response =
            [
                ..response,
                ..remainingPackets.OrderBy(p => p.ReadMultiPacketHeader().PacketNumber).SelectMany(p => p.Skip(payloadIndex))
            ];

            if (multiPacketHeader.IsCompressed)
            {
                using var compressedMemoryStream = new MemoryStream(response);
                using var decompressedMemoryStream = new MemoryStream();

                BZip2.Decompress(compressedMemoryStream, decompressedMemoryStream, false);

                response = decompressedMemoryStream.ToArray();
            }

            return response;
        }

        var responsePayloadHeader = response.ReadResponsePayloadIdentifier();
        if (responsePayloadHeader == PayloadIdentifier.Challenge)
        {
            return ExecuteQuery(
            [
                ..request.ReadRequestPayloadIdentifier() == PayloadIdentifier.Information ? request : [ request[0] ],
                ..response.Skip(response.Length - 4)
            ]);
        }

        if (responsePayloadHeader == PayloadIdentifier.ObsoleteGoldSource && request == InformationRequest)
        {
            Reestablish();
        }

        return response;
    }

    // The main reason that I separated synchronous method and the asynchronous method is, there is not that much benefit
    //   writing synchronous method then running it in a Task and calling it the "asynchronous version".
    // So, instead of that, I had separated two methods and used asynchronous methods on the asynchronous method, such as UdpClient.SendAsync (instead of Send), UdpClient.ReceiveAsync (Receive),
    //   and used CancellationToken.
    private async Task<byte[]> ExecuteQueryAsync(byte[] request, CancellationToken cancellationToken)
    {
        await _udpClient.SendAsync([ ..PacketHeader, ..request ], PacketHeader.Length + request.Length).TimeoutAfterAsync(TimeSpan.FromMilliseconds(SendTimeout), cancellationToken);

        var response = (await _udpClient.ReceiveAsync().TimeoutAfterAsync(TimeSpan.FromMilliseconds(ReceiveTimeout), cancellationToken)).Buffer;

        var packetHeader = response.ReadPacketIdentifier();
        if (packetHeader == PacketIdentifier.Split)
        {
            var multiPacketHeader = response.ReadMultiPacketHeader();

            if (!multiPacketHeader.IsGoldSourceServer)
            {
                IsUsingCompression = multiPacketHeader.IsCompressed;

                if (multiPacketHeader.IsCompressed)
                {
                    throw new NotImplementedException("Compressed packets not implemented yet. Please report server IP address and port, so I can test it.");
                }
            }

            var payloadIndex = multiPacketHeader.IsGoldSourceServer switch
            {
                // Packet Header(4) + ID(4) + Total Packets(1) + Packet Number(1) + Maximum Packet Size(2) + Decompressed Size(4) + CRC32 Checksum(4)
                false when multiPacketHeader.IsCompressed => 20,
                // Packet Header(4) + ID(4) + Total Packets(1) + Packet Number(1) + Maximum Packet Size(2)
                false when !multiPacketHeader.IsCompressed => 12,
                // Packet Header(4) + ID(4) + Packet Number(1)
                _ => 9
            };

            // We do not need the packet header. We already processed it and won't need again. So, just trim it.
            response = response.Skip(payloadIndex).ToArray();

            var remainingPackets = new List<byte[]>(multiPacketHeader.TotalPackets);

            for (var i = 1; i < multiPacketHeader.TotalPackets; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                remainingPackets.Add((await _udpClient.ReceiveAsync().TimeoutAfterAsync(TimeSpan.FromMilliseconds(ReceiveTimeout), cancellationToken)).Buffer);
            }

            // Combine the first response and remaining packets - of course after ordering it by packet number and trimming the packet header, just like above.
            response =
            [
                ..response,
                ..remainingPackets.OrderBy(p => p.ReadMultiPacketHeader().PacketNumber).SelectMany(p => p.Skip(payloadIndex))
            ];

            //TODO Add controls for uncompressed size and CRC32 checksum.
            if (multiPacketHeader.IsCompressed)
            {
                // Need to strip the packet header before decompressing it.
                using var compressedMemoryStream = new MemoryStream(response);
                using var decompressedMemoryStream = new MemoryStream();

                BZip2.Decompress(compressedMemoryStream, decompressedMemoryStream, false);

                response = decompressedMemoryStream.ToArray();
            }

            return response;
        }

        var responsePayloadHeader = response.ReadResponsePayloadIdentifier();
        if (responsePayloadHeader == PayloadIdentifier.Challenge)
        {
            return await ExecuteQueryAsync(
            [
                ..request.ReadRequestPayloadIdentifier() == PayloadIdentifier.Information ? request : [ request.First() ],
                ..response.Skip(response.Length - 4)
            ], cancellationToken);
        }

        // Obsolete GoldSource might send both obsolete and the new information packet on Information query.
        // It is not always guaranteed. Server won't tell us if there is a second packet.
        // So instead of reading the second packet, just reestablish the connection.
        if (responsePayloadHeader == PayloadIdentifier.ObsoleteGoldSource && request == InformationRequest)
        {
            Reestablish();
        }

        return response;
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
                SendTimeout = SendTimeout,
                ReceiveTimeout = ReceiveTimeout
            }
        };

        _udpClient.Client.Connect(IpEndPoint);
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
        _udpClient.Close();
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
            _udpClient.Dispose();
            _udpClient = null;
        }

        _disposed = true;
    }
}