using System.Buffers;
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
/// Game server class that holds information related to a game server in it.
/// </summary>
public class GameServer : IDisposable
{
    /// <summary>
    /// Information of the server.
    /// <para>Available after calling <see cref="GetInformationAsync"/> or <see cref="PerformQueriesAsync(SteamQueryA2SQuery, CancellationToken)"/>.</para>
    /// </summary>
    public SteamQueryInformation? Information { get; private set; }

    /// <summary>
    /// Players of the server.
    /// <para>Available after calling <see cref="GetPlayersAsync"/> or <see cref="PerformQueriesAsync(SteamQueryA2SQuery, CancellationToken)"/>.</para>
    /// </summary>
    public IReadOnlyList<SteamQueryPlayer>? Players { get; private set; }

    /// <summary>
    /// Rules of the server.
    /// <para>Available after calling <see cref="GetRulesAsync"/> or <see cref="PerformQueriesAsync(SteamQueryA2SQuery, CancellationToken)"/>.</para>
    /// </summary>
    public IReadOnlyList<SteamQueryRule>? Rules { get; private set; }

    /// <summary>
    /// If the server is using compression before sending response.
    /// <para>If the server is using Source protocol, this property will be available after calling any query that will return multi-packet response.</para>
    /// <para>Steam uses a packet size of up to 1400 bytes + IP/UDP headers. If a request or response needs more packets for the data it starts the packets with an additional header.</para>
    /// </summary>
    public bool? IsUsingCompression { get; private set; }

    /// <summary>
    /// IP endpoint of the server.
    /// </summary>
    public IPEndPoint IpEndPoint { get; set; }

    /// <summary>
    /// IP address of the server.
    /// </summary>
    public IPAddress IpAddress
    {
        get => IpEndPoint.Address;
        set => IpEndPoint.Address = value;
    }

    /// <summary>
    /// Port number of the server.
    /// </summary>
    public int Port
    {
        get => IpEndPoint.Port;
        set => IpEndPoint.Port = value;
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
    /// <exception cref="InvalidPortException">Thrown when port in endPoint is not in correct format.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when port is not valid.</exception>
    /// <exception cref="AddressNotFoundException">Thrown when IP address or hostname is not found.</exception>
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
    /// <exception cref="ArgumentOutOfRangeException">Thrown when port is not valid.</exception>
    /// <exception cref="AddressNotFoundException">Thrown when IP address or hostname is not found.</exception>
    public GameServer(string hostNameOrIpAddress, int port, AddressFamily addressFamily = AddressFamily.InterNetwork) : this(IpHelper.CreateIpEndPoint(hostNameOrIpAddress, port, addressFamily))
    {
    }

    /// <summary>
    /// Initialize a new instance of the <see cref="GameServer"/> class with given <see cref="MasterServerResponse"/>.
    /// </summary>
    /// <param name="masterServerResponse">Master server response that you got from <see cref="MasterServer"/>.</param>
    public GameServer(MasterServerResponse masterServerResponse) : this(masterServerResponse.IpAddress, masterServerResponse.Port)
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
        IpEndPoint = ipEndPoint;
    }

    /// <summary>
    /// Gets information.
    /// </summary>
    public async Task<SteamQueryInformation> GetInformationAsync(CancellationToken cancellationToken = default)
    {
        return Information = ServerQueryResponseReader.ParseInformation(await ExecuteQueryAsync(InformationRequest, cancellationToken));
    }

    /// <summary>
    /// Gets players.
    /// </summary>
    public async Task<IReadOnlyList<SteamQueryPlayer>> GetPlayersAsync(CancellationToken cancellationToken = default)
    {
        return Players = ServerQueryResponseReader.ParsePlayers(await ExecuteQueryAsync(PlayersRequest, cancellationToken));
    }

    /// <summary>
    /// Gets rules.
    /// </summary>
    public async Task<IReadOnlyList<SteamQueryRule>> GetRulesAsync(CancellationToken cancellationToken = default)
    {
        return Rules = ServerQueryResponseReader.ParseRules(await ExecuteQueryAsync(RulesRequest, cancellationToken));
    }
    
    /// <summary>
    /// Performs all queries.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    public Task PerformQueriesAsync(CancellationToken cancellationToken = default) => PerformQueriesAsync(SteamQueryA2SQuery.All, cancellationToken);

    /// <summary>
    /// Performs given queries.
    /// </summary>
    /// <param name="queries">Queries to be performed.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    public async Task PerformQueriesAsync(SteamQueryA2SQuery queries, CancellationToken cancellationToken = default)
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

    private async Task<byte[]> ExecuteQueryAsync(byte[] request, CancellationToken cancellationToken)
    {
        var shouldSend = true; //TODO refactor

        while (true)
        {
            if (shouldSend)
            {
                using var sendCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                sendCancellationTokenSource.CancelAfter(SendTimeout);
            
                await _socket.SendToAsync((byte[])[..PacketHeader, ..request], IpEndPoint, sendCancellationTokenSource.Token);

                shouldSend = false;
            }
            
            using var receiveCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            receiveCancellationTokenSource.CancelAfter(ReceiveTimeout);

            byte[] response;

            var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
            try
            {
                var receiveFromResult = await _socket.ReceiveFromAsync(buffer, IpEndPoint, receiveCancellationTokenSource.Token);
                response = buffer.AsSpan(0, receiveFromResult.ReceivedBytes).ToArray();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

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
                    byte[] packetResponse;

                    buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
                    try
                    {
                        var receiveFromResult = await _socket.ReceiveFromAsync(buffer, IpEndPoint, receiveCancellationTokenSource.Token);
                        packetResponse = buffer.AsSpan(0, receiveFromResult.ReceivedBytes).ToArray();
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }

                    remainingPackets.Add(packetResponse);
                }

                // Combine the first response and remaining packets - of course after ordering it by packet number and trimming the packet header, just like above.
                response = [..response, ..remainingPackets.OrderBy(p => p.ReadMultiPacketHeader().PacketNumber).SelectMany(p => p.Skip(payloadIndex))];

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
                request = [..request.ReadRequestPayloadIdentifier() == PayloadIdentifier.Information ? request : [request.First()], ..response.TakeLast(4)];
                shouldSend = true;
                continue;
            }

            // Obsolete GoldSource might send both obsolete and the new information packet on Information query.
            // It is not always guaranteed. Server won't tell us if there is a second packet.
            // So instead of reading the second packet, just reestablish the connection.
            if (responsePayloadHeader == PayloadIdentifier.ObsoleteGoldSource && request == InformationRequest)
            {
                continue;
            }
            
            return response;
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