using System.Net;
using System.Net.Sockets;
using ICSharpCode.SharpZipLib.BZip2;
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
    /// <returns>The time-out value, in milliseconds. If you set the property with a value between 1 and 499, the value will be changed to 500 - because how it is implemented in <see cref="Socket"/> class.
    /// <para>The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period.</para></returns>
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

    private static readonly byte[] InformationRequest = [ (byte)PayloadIdentifier.Information, .."Source Engine Query\0"u8.ToArray() ];
    private static readonly byte[] PlayersRequest     = [ (byte)PayloadIdentifier.Players,     ..DefaultChallenge ];
    private static readonly byte[] RulesRequest       = [ (byte)PayloadIdentifier.Rules,       ..DefaultChallenge ];

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
    public Information GetInformation() => ResponseReader.ParseInformation(ExecuteQuery(InformationRequest));
    /// <summary>
    /// Gets players synchronously.
    /// </summary>
    public List<Player> GetPlayers() => ResponseReader.ParsePlayers(ExecuteQuery(PlayersRequest));
    /// <summary>
    /// Gets rules synchronously.
    /// </summary>
    public List<Rule> GetRules() => ResponseReader.ParseRules(ExecuteQuery(RulesRequest));

    /// <summary>
    /// Gets information asynchronously.
    /// </summary>
    public async Task<Information> GetInformationAsync(CancellationToken cancellationToken = default) => ResponseReader.ParseInformation(await ExecuteQueryAsync(InformationRequest, cancellationToken));
    /// <summary>
    /// Gets players asynchronously.
    /// </summary>
    public async Task<List<Player>> GetPlayersAsync(CancellationToken cancellationToken = default) => ResponseReader.ParsePlayers(await ExecuteQueryAsync(PlayersRequest, cancellationToken));
    /// <summary>
    /// Gets rules asynchronously.
    /// </summary>
    public async Task<List<Rule>> GetRulesAsync(CancellationToken cancellationToken = default) => ResponseReader.ParseRules(await ExecuteQueryAsync(RulesRequest, cancellationToken));

    // The main reason that I seperated synchronous method and the asynchronous method is, there is no benefit writing synchronous method then running it in a Task.
    // So, instead of that, I had seperated two methods and used asynchronous methods on the asynchronous method, such as UdpClient.SendAsync (instead of Send), UdpClient.ReceiveAsync (Receive), etc.
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
                ..remainingPackets.OrderBy(p => p.ReadMultiPacketHeader().PacketNumber).SelectMany(p => p.Skip(payloadIndex)).ToArray()
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
        if (responsePayloadHeader == PayloadIdentifier.OldGoldSource)
        {
            throw new NotImplementedException("Older and pre-Steam GoldSource servers are not implemented yet.");
        }

        if (responsePayloadHeader == PayloadIdentifier.Challenge)
        {
            return ExecuteQuery(
            [
                ..request.ReadRequestPayloadIdentifier() == PayloadIdentifier.Information ? request : [ request[0] ],
                ..response.Skip(response.Length - 4)
            ]);
        }

        return response;
    }

    //TODO SendTimeout and ReceiveTimeout only works with synchronous calls. Make them work with asynchronous calls aswell.
    private async Task<byte[]> ExecuteQueryAsync(byte[] request, CancellationToken cancellationToken)
    {
#if NETCOREAPP2_0_OR_GREATER
        await _udpClient.SendAsync((byte[])[ ..PacketHeader, ..request ], cancellationToken);
#else
        await _udpClient.SendAsync([ ..PacketHeader, ..request ], PacketHeader.Length + request.Length);
#endif

#if NETCOREAPP2_0_OR_GREATER
        var response = (await _udpClient.ReceiveAsync(cancellationToken)).Buffer;
#else
        var response = (await _udpClient.ReceiveAsync()).Buffer;
#endif

        var packetHeader = response.ReadPacketIdentifier();
        if (packetHeader == PacketIdentifier.Split)
        {
            var multiPacketHeader = response.ReadMultiPacketHeader();

            if (!multiPacketHeader.IsGoldSourceServer)
            {
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

#if NETCOREAPP2_0_OR_GREATER
                remainingPackets.Add((await _udpClient.ReceiveAsync(cancellationToken)).Buffer);
#else
                remainingPackets.Add((await _udpClient.ReceiveAsync()).Buffer);
#endif
            }

            // Combine the first response and remaining packets - of course after ordering it by packet number and trimming the packet header, just like above.
            response =
            [
                ..response,
                ..remainingPackets.OrderBy(p => p.ReadMultiPacketHeader().PacketNumber).SelectMany(p => p.Skip(payloadIndex)).ToArray()
            ];

            //TODO This should work but first, test it. Also check uncompressed size and CRC32 checksum.
            if (multiPacketHeader.IsCompressed)
            {
                using var compressedMemoryStream = new MemoryStream(response); // Probably, I will need to strip the packet header before decompressing it.
                using var decompressedMemoryStream = new MemoryStream();

                BZip2.Decompress(compressedMemoryStream, decompressedMemoryStream, false);

                response = decompressedMemoryStream.ToArray();
            }

            return response;
        }

        var responsePayloadHeader = response.ReadResponsePayloadIdentifier();
        if (responsePayloadHeader == PayloadIdentifier.OldGoldSource)
        {
            throw new NotImplementedException("Older and pre-Steam GoldSource servers are not implemented yet.");
        }

        if (responsePayloadHeader == PayloadIdentifier.Challenge)
        {
            return await ExecuteQueryAsync(
            [
                ..request.ReadRequestPayloadIdentifier() == PayloadIdentifier.Information ? request : [ request[0] ],
                ..response.Skip(response.Length - 4)
            ], cancellationToken);
        }

        return response;
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