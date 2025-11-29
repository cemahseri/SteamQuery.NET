using System.Buffers;
using System.Net;
using System.Net.Sockets;
//using ICSharpCode.SharpZipLib.BZip2;
using SteamQuery.Enums;
using SteamQuery.Exceptions;
using SteamQuery.Extensions;
using SteamQuery.Helpers;
using SteamQuery.Models;

namespace SteamQuery;

/// <summary>
/// Game server class that holds information related to a game server in it.
/// </summary>
/// TODO make thread safe
public class GameServer : IDisposable
{
    /// <summary>
    /// If the server is using compression before sending response.
    /// <para>If the server is using Source protocol, this property will be available after calling any query that will return multi-packet response.</para>
    /// <para>Steam uses a packet size of up to 1400 bytes + IP/UDP headers. If a request or response needs more packets for the data it starts the packets with an additional header.</para>
    /// </summary>
    public bool? IsUsingCompression { get; private set; }

    /// <summary>
    /// IP endpoint of the server.
    /// </summary>
    public IPEndPoint IpEndPoint { get; }

    /// <summary>
    /// IP address of the server.
    /// </summary>
    public IPAddress IpAddress => IpEndPoint.Address;

    /// <summary>
    /// Port number of the server.
    /// </summary>
    public int Port => IpEndPoint.Port;

    /// <summary>
    /// The timeout after which a query receive call should be faulted with a <see cref="TimeoutException"/> if it hasn't otherwise completed.
    /// <para>The default value is 5 seconds.</para>
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5.0d);
    
    /// <summary>
    /// Initial response and GoldSource multi-packet response buffer size.
    /// <para>The default value is 2048 bytes. (Value is clamped between 1500 and 65507 bytes.)</para>
    /// </summary>
    public int BufferSize
    {
        get;
        set => field = Math.Clamp(value, 1500, 65507);
    } = 2048;
    
    private readonly Socket _socket = new(SocketType.Dgram, ProtocolType.Udp);

    private bool _disposed;

    private static readonly byte[] PacketHeader     = [ 0xFF, 0xFF, 0xFF, 0xFF ];
    private static readonly byte[] DefaultChallenge = PacketHeader;

    private static readonly byte[] InformationRequest = [ (byte)PayloadIdentifier.Information, .."Source Engine Query\0"u8 ];
    private static readonly byte[] PlayersRequest     = [ (byte)PayloadIdentifier.Players,     ..DefaultChallenge ];
    private static readonly byte[] RulesRequest       = [ (byte)PayloadIdentifier.Rules,       ..DefaultChallenge ];
    
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    private readonly record struct Packet(int PacketNumber, byte[] Buffer, int Length);

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

        _socket.Connect(IpEndPoint);
    }

    /// <summary>
    /// Gets information.
    /// </summary>
    public async Task<SteamQueryInformation> GetInformationAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(GameServer));

        using var responseMemoryOwner = await ExecuteQueryAsync(InformationRequest, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
        return ServerQueryResponseReader.ParseInformation(responseMemoryOwner.Memory.Span);
    }

    /// <summary>
    /// Gets players.
    /// </summary>
    public async Task<SteamQueryPlayer[]> GetPlayersAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(GameServer));

        using var responseMemoryOwner = await ExecuteQueryAsync(PlayersRequest, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
        return ServerQueryResponseReader.ParsePlayers(responseMemoryOwner.Memory.Span);
    }

    /// <summary>
    /// Gets rules.
    /// </summary>
    public async Task<SteamQueryRule[]> GetRulesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(GameServer));

        using var responseMemoryOwner = await ExecuteQueryAsync(RulesRequest, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
        return ServerQueryResponseReader.ParseRules(responseMemoryOwner.Memory.Span);
    }
    
    /// <summary>
    /// Performs all queries.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    public Task<(SteamQueryInformation? Information, SteamQueryPlayer[]? Players, SteamQueryRule[]? Rules)>
        PerformQueriesAsync(CancellationToken cancellationToken = default) => PerformQueriesAsync(SteamQueryA2SQuery.All, cancellationToken);


    /// <summary>
    /// Performs given queries.
    /// </summary>
    /// <param name="queries">Queries to be performed.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    /// Tuple where non-requested items are null: (<see cref="SteamQueryInformation"/>, <see cref="SteamQueryPlayer"/>[], <see cref="SteamQueryRule"/>[])
    /// </returns>
    public async Task<(SteamQueryInformation? Information, SteamQueryPlayer[]? Players, SteamQueryRule[]? Rules)>
        PerformQueriesAsync(SteamQueryA2SQuery queries, CancellationToken cancellationToken = default)
    {
        SteamQueryInformation? information = null;
        SteamQueryPlayer[]? players = null;
        SteamQueryRule[]? rules = null;

        if (queries.HasFlag(SteamQueryA2SQuery.Information))
        {
            information = await GetInformationAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
        }

        if (queries.HasFlag(SteamQueryA2SQuery.Players))
        {
            players = await GetPlayersAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
        }

        if (queries.HasFlag(SteamQueryA2SQuery.Rules))
        {
            rules = await GetRulesAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
        }

        return (information, players, rules);
    }

    private async Task<IMemoryOwner<byte>> ExecuteQueryAsync(byte[] request, CancellationToken cancellationToken)
    {
        await _semaphoreSlim.WaitAsync(cancellationToken);

        try
        {
            var shouldSend = true; //TODO refactor

            while (true)
            {
                if (shouldSend)
                {
                    var sendBufferLength = PacketHeader.Length + request.Length;

                    Span<byte> sendBuffer = stackalloc byte[sendBufferLength];
                    PacketHeader.CopyTo(sendBuffer);
                    request.AsSpan().CopyTo(sendBuffer[PacketHeader.Length..]);

                    _socket.Send(sendBuffer);

                    shouldSend = false;
                }
                
                using var receiveCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                if (Timeout.TotalMilliseconds > 0)
                {
                    receiveCancellationTokenSource.CancelAfter(Timeout);
                }

                var initialBuffer = ArrayPool<byte>.Shared.Rent(BufferSize);

                int firstReceived;
                try
                {
                    firstReceived = await _socket.ReceiveAsync(initialBuffer, receiveCancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException operationCanceledException) when (!cancellationToken.IsCancellationRequested && receiveCancellationTokenSource.IsCancellationRequested)
                {
                    ArrayPool<byte>.Shared.Return(initialBuffer);
                    throw new TimeoutException(null, operationCanceledException);
                }
                catch (SocketException socketException) when (socketException.SocketErrorCode == SocketError.TimedOut)
                {
                    ArrayPool<byte>.Shared.Return(initialBuffer);
                    throw new TimeoutException(null, socketException);
                }

                var bufferSpan = new ReadOnlyMemory<byte>(initialBuffer, 0, firstReceived);

                var packetHeader = bufferSpan.Span.ReadPacketIdentifier();
                if (packetHeader == PacketIdentifier.Split)
                {
                    var firstPacketHeader = bufferSpan.Span.ReadMultiPacketHeader();

                    if (!firstPacketHeader.IsGoldSourceServer)
                    {
                        IsUsingCompression = firstPacketHeader.IsCompressed;

                        if (firstPacketHeader.IsCompressed)
                        {
                            ArrayPool<byte>.Shared.Return(initialBuffer);
                            throw new NotImplementedException("Compressed packets not implemented yet.");
                        }
                    }

                    var payloadIndex = firstPacketHeader.IsGoldSourceServer switch
                    {
                        // Packet Header(4) + ID(4) + Total Packets(1) + Packet Number(1) + Maximum Packet Size(2) + Decompressed Size(4) + CRC32 Checksum(4)
                        false when firstPacketHeader.IsCompressed => 20,
                        // Packet Header(4) + ID(4) + Total Packets(1) + Packet Number(1) + Maximum Packet Size(2)
                        false when !firstPacketHeader.IsCompressed => 12,
                        // Packet Header(4) + ID(4) + Packet Number(1)
                        _ => 9
                    };

                    var packetCount = firstPacketHeader.TotalPackets - 1;
                    var packets = new Packet[packetCount];
                    
                    var packetBufferSize = firstPacketHeader.IsGoldSourceServer || firstPacketHeader.MaximumPacketSize <= 0
                        ? BufferSize
                        : firstPacketHeader.MaximumPacketSize;

                    try
                    {
                        for (var i = 0; i < packetCount; i++)
                        {
                            var packetBuffer = ArrayPool<byte>.Shared.Rent(packetBufferSize);

                            try
                            {
                                var receiveFromResult = await _socket.ReceiveAsync(packetBuffer, receiveCancellationTokenSource.Token).ConfigureAwait(false);

                                var multiPacketHeader = packetBuffer.AsSpan(0, receiveFromResult).ReadMultiPacketHeader();
                                packets[i] = new Packet(multiPacketHeader.PacketNumber, packetBuffer, receiveFromResult);
                            }
                            catch
                            {
                                ArrayPool<byte>.Shared.Return(packetBuffer);
                                throw;
                            }
                        }

                        Array.Sort(packets, (a, b) => a.PacketNumber.CompareTo(b.PacketNumber));

                        var orderedPacketsMemory = new ReadOnlyMemory<byte>[packets.Length];
                        for (var i = 0; i < packets.Length; i++)
                        {
                            var packet = packets[i];

                            orderedPacketsMemory[i] = packet.Buffer.AsMemory(0, packet.Length);
                        }

                        var firstPayloadLength = Math.Max(0, bufferSpan.Span.Length - payloadIndex);

                        var totalLength = firstPayloadLength + packets.Sum(p => Math.Max(0, p.Length - payloadIndex));
                        var responseMemoryOwner = MemoryPool<byte>.Shared.Rent(totalLength);

                        var destinationOffset = 0;

                        if (firstPayloadLength > 0)
                        {
                            bufferSpan.Span.Slice(payloadIndex, firstPayloadLength).CopyTo(responseMemoryOwner.Memory.Span.Slice(destinationOffset, firstPayloadLength));
                            destinationOffset += firstPayloadLength;
                        }

                        foreach (var packet in orderedPacketsMemory)
                        {
                            var packetSpanLength = Math.Max(0, packet.Span.Length - payloadIndex);
                            if (packetSpanLength <= 0)
                            {
                                continue;
                            }

                            packet.Span.Slice(payloadIndex, packetSpanLength).CopyTo(responseMemoryOwner.Memory.Span.Slice(destinationOffset, packetSpanLength));
                            destinationOffset += packetSpanLength;
                        }

                        //TODO Add controls for uncompressed size and CRC32 checksum.
                        /*if (multiPacketHeader.IsCompressed)
                        {
                            // Need to strip the packet header before decompressing it.
                            using var compressedMemoryStream = new MemoryStream(response);
                            using var decompressedMemoryStream = new MemoryStream();

                            BZip2.Decompress(compressedMemoryStream, decompressedMemoryStream, false);

                            response = decompressedMemoryStream.ToArray();
                        }*/

                        return responseMemoryOwner;
                    }
                    finally
                    {
                        foreach (var packet in packets)
                        {
                            ArrayPool<byte>.Shared.Return(packet.Buffer);
                        }

                        ArrayPool<byte>.Shared.Return(initialBuffer);
                    }
                }

                var responsePayloadHeader = bufferSpan.Span.ReadResponsePayloadIdentifier();
                if (responsePayloadHeader == PayloadIdentifier.Challenge)
                {
                    //TODO reduce allocation
                    request = [..request.ReadRequestPayloadIdentifier() == PayloadIdentifier.Information ? request : [request.First()], ..bufferSpan[^4..].ToArray()];
                    shouldSend = true;

                    ArrayPool<byte>.Shared.Return(initialBuffer);
                    continue;
                }

                // Obsolete GoldSource might send both obsolete and the new information packet on Information query.
                // It is not always guaranteed. Server won't tell us if there is a second packet.
                // So instead of reading the second packet, just reestablish the connection.
                if (responsePayloadHeader == PayloadIdentifier.ObsoleteGoldSource && request == InformationRequest)
                {
                    ArrayPool<byte>.Shared.Return(initialBuffer);
                    continue;
                }

                var ownerSingle = MemoryPool<byte>.Shared.Rent(firstReceived);

                initialBuffer.AsSpan(0, firstReceived).CopyTo(ownerSingle.Memory.Span);
                ArrayPool<byte>.Shared.Return(initialBuffer);

                return ownerSingle;
            }
        }
        finally
        {
            _semaphoreSlim.Release();
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
        
        _disposed = true;

        if (disposing)
        {
            _socket.Dispose();
            _semaphoreSlim.Dispose();
        }
    }
}