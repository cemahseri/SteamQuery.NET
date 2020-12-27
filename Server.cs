using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using SteamQuery.Exceptions;
using SteamQuery.Models;
using SteamQuery.Parsers;
using SteamQuery.Utils;

namespace SteamQuery
{
    /// <summary>
    /// Server class holds information related to a game server in it.
    /// </summary>
    /// <remarks>Thread-safe.</remarks>
    public sealed class Server : IDisposable
    {
        private IPAddress _ip;
        /// <summary>
        /// IP address of the server.
        /// </summary>
        public IPAddress Ip
        {
            get => _ip;
            set => _ip = value ?? throw new AddressNotFoundException();
        }

        private int _port;
        /// <summary>
        /// Port number of the server.
        /// </summary>
        public int Port
        {
            get => _port;
            set
            {
                if (value < 0 || value > 65535)
                {
                    throw new InvalidPortException();
                }

                _port = value;
            }
        }

        // Timeout times doesn't work on async as I see. Will look into this later. Might have to do something like "Task.WhenAny(DoQuery, Task.Delay(Timeout))" or something like that, IDK.
        // Update: Yeah, I got why those doesn't work on asynchronous methods. SendTimeout and ReceiveTimeout only affects synchronous Send and Receives methods. I still might do something I've said before.
        /// <summary>
        /// Gets or sets a value that specifies the amount of time after which a synchronous ExecuteQuery call will time out.
        /// </summary>
        /// <returns>The time-out value, in milliseconds. If you set the property with a value between 1 and 499, the value will be changed to 500. The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period.</returns>
        public int SendTimeout { get; set; } = 5000;
        /// <summary>
        /// Gets or sets a value that specifies the amount of time after which a synchronous ExecuteQuery call will time out.
        /// </summary>
        /// <returns>The time-out value, in milliseconds. The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period.</returns>
        public int ReceiveTimeout { get; set; } = 5000;

        private readonly UdpClient _udpClient;
        private IPEndPoint _ipEndPoint;

        private static readonly byte[] Informations = { 0xFF, 0xFF, 0xFF, 0xFF, 0x54, 0x53, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x20, 0x45, 0x6E, 0x67, 0x69, 0x6E, 0x65, 0x20, 0x51, 0x75, 0x65, 0x72, 0x79, 0x00 };
        private static readonly byte[] Players = { 0xFF, 0xFF, 0xFF, 0xFF, 0x55, 0xFF, 0xFF, 0xFF, 0xFF };
        private static readonly byte[] Rules = { 0xFF, 0xFF, 0xFF, 0xFF, 0x56, 0xFF, 0xFF, 0xFF, 0xFF };

        /// <summary>
        /// Initialize without providing IP address nor port number. Be sure that you set them before connecting!
        /// </summary>
        public Server()
        {
        }

        /// <summary>
        /// Initialize with given IP address and port number with a string parameter.
        /// </summary>
        /// <param name="endPoint">IP end point. Seperating IP address and port number with colon (:) required. Example: 127.0.0.1:1337</param>
        public Server(string endPoint) : this(IpUtils.CreateIpEndPoint(endPoint))
        {
        }

        /// <summary>
        /// Initialize with given IP address <i>(in string type)</i> and port number.
        /// </summary>
        /// <param name="ip">IP address.</param>
        /// <param name="port">Port number.</param>
        public Server(string ip, int port) : this(IpUtils.CreateIpEndPoint(ip, port))
        {
        }

        /// <summary>
        /// Initialize with given IP address <i>(in IPAddress type)</i> and port number.
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
            Ip = ipEndPoint.Address;
            Port = ipEndPoint.Port;

            _ipEndPoint = ipEndPoint;

            _udpClient = new UdpClient
            {
                Client =
                {
                    SendTimeout = SendTimeout,
                    ReceiveTimeout = ReceiveTimeout
                }
            };
        }

        /// <summary>
        /// Connects to the game server synchronously.
        /// </summary>
        /// <returns>true if successfully connected to the game server.</returns>
        public bool Connect()
        {
            _udpClient.Client.Connect(_ipEndPoint);

            return true;
        }

        /// <summary>
        /// Connects to the game server asynchronously.
        /// </summary>
        /// <returns>true if successfully connected to the game server.</returns>
        public async Task<bool> ConnectAsync()
        {
            await _udpClient.Client.ConnectAsync(_ipEndPoint);

            return true;
        }

        /// <summary>
        /// Gets informations synchronously.
        /// </summary>
        public Informations GetInformations() => InformationParser.Instance.Parse(ExecuteQuery(Informations));
        /// <summary>
        /// Gets players synchronously.
        /// </summary>
        public List<Player> GetPlayers() => PlayerlistParser.Instance.Parse(ExecuteQuery(Players));
        /// <summary>
        /// Gets rules synchronously.
        /// </summary>
        public List<Rule> GetRules() => RuleParser.Instance.Parse(ExecuteQuery(Rules));

        /// <summary>
        /// Gets informations asynchronously.
        /// </summary>
        public async Task<Informations> GetInformationsAsync() => InformationParser.Instance.Parse(await ExecuteQueryAsync(Informations));
        /// <summary>
        /// Gets players asynchronously.
        /// </summary>
        public async Task<List<Player>> GetPlayersAsync() => PlayerlistParser.Instance.Parse(await ExecuteQueryAsync(Players));
        /// <summary>
        /// Gets rules asynchronously.
        /// </summary>
        public async Task<List<Rule>> GetRulesAsync() => RuleParser.Instance.Parse(await ExecuteQueryAsync(Rules));

        private byte[] ExecuteQuery(byte[] query)
        {
            _udpClient.Send(query, query.Length);
            var response = _udpClient.Receive(ref _ipEndPoint);

            switch (query[0])
            {
                case 0x54: // Informations
                    return response;
                case 0x55: // Players
                case 0x56: // Rules
                    response[4] = query[0];
                    break;
                default:
                    throw new UnexpectedByteException(query[0], 0x54, 0x55, 0x56);
            }

            var index = 5;
            for (var i = 5; i <= 8; i++)
            {
                response[i] = response.ReadByte(ref index);
            }

            _udpClient.Send(response, response.Length);
            return _udpClient.Receive(ref _ipEndPoint);
        }

        private async Task<byte[]> ExecuteQueryAsync(byte[] query)
        {
            await _udpClient.SendAsync(query, query.Length);
            var result = await _udpClient.ReceiveAsync();

            var response = result.Buffer;

            switch (query[0])
            {
                case 0x54: // Informations
                    return response;
                case 0x55: // Players
                case 0x56: // Rules
                    response[4] = query[0];
                    break;
                default:
                    throw new UnexpectedByteException(query[0], 0x54, 0x55, 0x56);
            }

            var index = 5;
            for (var i = 5; i <= 8; i++)
            {
                response[i] = response.ReadByte(ref index);
            }

            await _udpClient.SendAsync(response, response.Length);
            var secondResult = await _udpClient.ReceiveAsync();
            return secondResult.Buffer;
        }

        /// <summary>
        /// Disconnects from the game server.
        /// </summary>
        /// <returns>true if successfully disconnected from the game server.</returns>
        public bool Disconnect()
        {
            _udpClient.Client.Disconnect(false);

            return true;
        }

        /// <summary>
        /// Disposes the class.
        /// </summary>
        public void Dispose()
        {
            Disconnect();
            _udpClient.Dispose();
        }
    }
}