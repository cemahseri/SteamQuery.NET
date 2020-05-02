using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using SteamQuery.Helpers;
using SteamQuery.Models;

namespace SteamQuery
{
    public sealed class Server : IDisposable
    {
        public Informations Informations { get; set; } = new Informations();

        public IPAddress Ip { get; set; }
        public int Port { get; set; }
        
        // Don't ask me why the fuck I am not using T*DO. I just don't.
        // Will implement them later.
        public bool ParsePlayersOnConnection { get; set; }
        public bool ParseRulessOnConnection { get; set; }

        // Timeout times doesn't work on async as I see. Will look into this later. Might have to do something like "Task.WhenAny(DoQuery, Task.Delay(Timeout))" or something like that, IDK.
        public int SendTimeout { get; set; } = 5000;
        public int ReceiveTimeout { get; set; } = 5000;

        private readonly UdpClient _udpClient;
        private readonly IPEndPoint _ipEndPoint;

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

            // LMAO, what this shit does right here?
            ConnectAsync();
        }

        public async void ConnectAsync()
        {
            await _udpClient.Client.ConnectAsync(_ipEndPoint).ConfigureAwait(false);
            
            // ROFLULMAO, look at this mess.
            await GetInformationsAsync().ConfigureAwait(false);
            await GetPlayersAsync().ConfigureAwait(false);
            await GetRulesAsync().ConfigureAwait(false);
        }

        // Now that's a lot of code duplication!
        // And also, for now, this shit only supports Source games. Will make this support other protocols too.
        public async Task<Informations> GetInformationsAsync()
        {
            var query = QueryHelper.Prefix.Concat(QueryHelper.Informations).ToArray();
            await _udpClient.SendAsync(query, query.Length).ConfigureAwait(false);
            
            var response = _udpClient.ReceiveAsync().ConfigureAwait(false).GetAwaiter().GetResult().Buffer;
            return ResponseParseHelper.ParseInformation(Informations, response);
        }
        
        public async Task<List<Player>> GetPlayersAsync()
        {
            var query = QueryHelper.Prefix.Concat(QueryHelper.Players).ToArray();
            await _udpClient.SendAsync(query, query.Length).ConfigureAwait(false);
            
            var response = _udpClient.ReceiveAsync().ConfigureAwait(false).GetAwaiter().GetResult().Buffer;
            response[4] = 0x55;
            
            var index = 5;
            for (var i = 5; i <= 8; i++)
            {
                response[i] = response.ReadByte(ref index);
            }

            await _udpClient.SendAsync(response, response.Length).ConfigureAwait(false);

            response = _udpClient.ReceiveAsync().ConfigureAwait(false).GetAwaiter().GetResult().Buffer;
            return ResponseParseHelper.ParsePlayers(Informations.Players, response);
        }
        
        public async Task<List<Rule>> GetRulesAsync()
        {
            var query = QueryHelper.Prefix.Concat(QueryHelper.Rules).ToArray();
            await _udpClient.SendAsync(query, query.Length).ConfigureAwait(false);
            
            var response = _udpClient.ReceiveAsync().ConfigureAwait(false).GetAwaiter().GetResult().Buffer;
            response[4] = 0x56;
            
            var index = 5;
            for (var i = 5; i <= 8; i++)
            {
                response[i] = response.ReadByte(ref index);
            }
            
            await _udpClient.SendAsync(response, response.Length).ConfigureAwait(false);
            
            response = _udpClient.ReceiveAsync().ConfigureAwait(false).GetAwaiter().GetResult().Buffer;
            return ResponseParseHelper.ParseRules(Informations.Rules, response);
        }

        // Check DisconnectAsync later.
        public void Disconnect()
        {
            _udpClient.Client.Disconnect(false);
        }

        public void Dispose()
        {
            Disconnect();
            _udpClient.Dispose();
        }
    }
}