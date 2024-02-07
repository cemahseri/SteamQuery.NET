using System.Text;
using SteamQuery;

Console.OutputEncoding = Encoding.UTF8;

// Pick a server from: https://www.gametracker.com/search/cs/
using var server = new Server("127.0.0.1:27015");

var information = await server.GetInformationAsync();

Console.WriteLine($"Protocol: {information.Protocol}");
Console.WriteLine($"Server Name: {information.ServerName}");
Console.WriteLine($"Folder: {information.Folder}");
Console.WriteLine($"Game Name: {information.GameName}");
Console.WriteLine($"Players: {information.Players.Count}/{information.MaxPlayers}");
Console.WriteLine($"Bots: {information.Bots}");
Console.WriteLine($"Server Type: {information.ServerType}");
Console.WriteLine($"Environment: {information.Environment}");
Console.WriteLine($"Visible: {information.Visible}");
Console.WriteLine($"VAC Secured: {information.VacSecured}");
Console.WriteLine($"Version: {information.Version}");

Console.WriteLine();

var hasExtraDataFlag = information.ExtraDataFlag.HasValue;

Console.WriteLine($"Has Extra Data Flag: {hasExtraDataFlag}");

if (hasExtraDataFlag)
{
    if (information.Port.HasValue)
    {
        Console.WriteLine($"Port: {information.Port.Value}");
    }

    if (information.SteamId.HasValue)
    {
        Console.WriteLine($"SteamID: {information.SteamId.Value}");
    }

    if (information.GameId.HasValue)
    {
        Console.WriteLine($"Game ID: {information.GameId.Value}");
    }

    if (information.SourceTvPort.HasValue)
    {
        Console.WriteLine($"SourceTV Port: {information.SourceTvPort.Value}");
    }

    if (!string.IsNullOrEmpty(information.SourceTvName))
    {
        Console.WriteLine($"SourceTV Name: {information.SourceTvName}");
    }

    if (!string.IsNullOrEmpty(information.Keywords))
    {
        Console.WriteLine($"Keywords: {information.Keywords}");
    }
}

Console.WriteLine();

var players = await server.GetPlayersAsync();

Console.WriteLine($"Online Players: {players.Count}/{information.MaxPlayers}");

foreach (var player in players)
{
    Console.WriteLine($"[{player.Index}] [{player.Duration}] {player.Score} - {player.Name}");
}

Console.WriteLine();

var rules = await server.GetRulesAsync();

foreach (var rule in rules)
{
    Console.WriteLine($"{rule.Name} = {rule.Value}");
}

Console.ReadKey(true);