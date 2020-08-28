using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using SteamQuery.Exceptions;
using SteamQuery.Helpers;
using SteamQuery.Models;

namespace SteamQuery
{
    // Will add later comment blocks.
    public sealed class Server : IDisposable
    {
        private IPAddress _ip;
        public IPAddress Ip
        {
            get => _ip;
            set => _ip = value ?? throw new AddressNotFoundException();
        }

        private int _port;
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
        public int SendTimeout { get; set; } = 5000;
        public int ReceiveTimeout { get; set; } = 5000;

        private readonly UdpClient _udpClient;
        private IPEndPoint _ipEndPoint;

        private static readonly byte[] Informations = { 0xFF, 0xFF, 0xFF, 0xFF, 0x54, 0x53, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x20, 0x45, 0x6E, 0x67, 0x69, 0x6E, 0x65, 0x20, 0x51, 0x75, 0x65, 0x72, 0x79, 0x00 };
        private static readonly byte[] Players = { 0xFF, 0xFF, 0xFF, 0xFF, 0x55, 0xFF, 0xFF, 0xFF, 0xFF };
        private static readonly byte[] Rules = { 0xFF, 0xFF, 0xFF, 0xFF, 0x56, 0xFF, 0xFF, 0xFF, 0xFF };

        public Server()
        {
        }

        public Server(string endPoint) : this(IpHelper.CreateIpEndPoint(endPoint))
        {
        }

        public Server(string ip, int port) : this(IpHelper.CreateIpEndPoint(ip, port))
        {
        }

        public Server(IPAddress ip, int port) : this(new IPEndPoint(ip, port))
        {
        }

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

        public bool Connect()
        {
            _udpClient.Client.Connect(_ipEndPoint);

            return true;
        }

        public async Task<bool> ConnectAsync()
        {
            await _udpClient.Client.ConnectAsync(_ipEndPoint);

            return true;
        }

        public Informations GetInformations() => ResponseParser.ParseInformation(ExecuteQuery(Informations));
        public List<Player> GetPlayers() => ResponseParser.ParsePlayers(ExecuteQuery(Players));
        public List<Rule> GetRules() => ResponseParser.ParseRules(ExecuteQuery(Rules));

        public async Task<Informations> GetInformationsAsync() => ResponseParser.ParseInformation(await ExecuteQueryAsync(Informations));
        public async Task<List<Player>> GetPlayersAsync() => ResponseParser.ParsePlayers(await ExecuteQueryAsync(Players));
        public async Task<List<Rule>> GetRulesAsync() => ResponseParser.ParseRules(await ExecuteQueryAsync(Rules));

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

            _udpClient.SendAsync(response, response.Length);
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

        public bool Disconnect()
        {
            _udpClient.Client.Disconnect(false);

            return true;
        }

        public void Dispose()
        {
            Disconnect();
            _udpClient.Dispose();
        }
    }
}