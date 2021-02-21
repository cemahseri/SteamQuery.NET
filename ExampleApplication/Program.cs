using System;
using System.Threading.Tasks;
using SteamQuery;

namespace ExampleApplication
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            // Pick a server from: https://www.gametracker.com/search/cs/
            var server = new Server("45.10.56.63:27015");

            if (await server.ConnectAsync())
            {
                Console.WriteLine("Connected successfully!");
            }
            else
            {
                Console.WriteLine("Error while connecting."); // Throwing error on connection error will be useful. Will probably add.
                return;
            }

            var informations = await server.GetInformationsAsync();
            
            Console.WriteLine("Protocol: " + informations.Protocol.ToString("X2"));
            Console.WriteLine("Server Name: " + informations.ServerName);
            Console.WriteLine("Folder: " + informations.Folder);
            Console.WriteLine("Game Name: " + informations.GameName);
            Console.WriteLine("Players: " + informations.Players.Count + "/" + informations.MaxPlayers);
            Console.WriteLine("Bots: " + informations.Bots);
            Console.WriteLine("Server Type: " + informations.ServerType);
            Console.WriteLine("Environment: " + informations.Environment);
            Console.WriteLine("Visible: " + informations.Visible);
            Console.WriteLine("VAC Secured: " + informations.VacSecured);
            Console.WriteLine("Version: " + informations.Version);

            var hasExtraDataFlag = informations.ExtraDataFlag.HasValue;

            Console.WriteLine("Has Extra Data Flag: " + hasExtraDataFlag);

            if (hasExtraDataFlag)
            {
                if (informations.Port.HasValue)
                {
                    Console.WriteLine("Port: " + informations.Port.Value);
                }

                if (informations.SteamId.HasValue)
                {
                    Console.WriteLine("SteamID: " + informations.SteamId.Value);
                }

                if (informations.GameId.HasValue)
                {
                    Console.WriteLine("Game ID: " + informations.GameId.Value);
                }

                if (informations.SourceTvPort.HasValue)
                {
                    Console.WriteLine("SourceTV Port: " + informations.SourceTvPort.Value);
                }

                if (!string.IsNullOrEmpty(informations.SourceTvName))
                {
                    Console.WriteLine("SourceTV Name: " + informations.SourceTvName);
                }

                if (!string.IsNullOrEmpty(informations.Keywords))
                {
                    Console.WriteLine("Keywords: " + informations.Keywords);
                }
            }

            Console.WriteLine();
            
            var players = await server.GetPlayersAsync();

            foreach (var player in players)
            {
                Console.WriteLine("Index: " + player.Index);
                Console.WriteLine("Name: " + player.Name);
                Console.WriteLine("Score: " + player.Score);
                Console.WriteLine("Duration: " + player.DurationTimeSpan);

                Console.WriteLine();
            }

            Console.ReadKey();
        }
    }
}