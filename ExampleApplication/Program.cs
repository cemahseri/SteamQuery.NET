using System.Text;
using SteamQuery;

Console.OutputEncoding = Encoding.UTF8;

// Pick a server from: https://www.gametracker.com/search/cs/
using var server = new Server("127.0.0.1:27015");

var informations = await server.GetInformationsAsync();

Console.WriteLine($"Protocol: {informations.Protocol}");
Console.WriteLine($"Server Name: {informations.ServerName}");
Console.WriteLine($"Folder: {informations.Folder}");
Console.WriteLine($"Game Name: {informations.GameName}");
Console.WriteLine($"Players: {informations.Players.Count}/{informations.MaxPlayers}");
Console.WriteLine($"Bots: {informations.Bots}");
Console.WriteLine($"Server Type: {informations.ServerType}");
Console.WriteLine($"Environment: {informations.Environment}");
Console.WriteLine($"Visible: {informations.Visible}");
Console.WriteLine($"VAC Secured: {informations.VacSecured}");
Console.WriteLine($"Version: {informations.Version}");

Console.WriteLine();

var hasExtraDataFlag = informations.ExtraDataFlag.HasValue;

Console.WriteLine($"Has Extra Data Flag: {hasExtraDataFlag}");

if (hasExtraDataFlag)
{
    if (informations.Port.HasValue)
    {
        Console.WriteLine($"Port: {informations.Port.Value}");
    }

    if (informations.SteamId.HasValue)
    {
        Console.WriteLine($"SteamID: {informations.SteamId.Value}");
    }

    if (informations.GameId.HasValue)
    {
        Console.WriteLine($"Game ID: {informations.GameId.Value}");
    }

    if (informations.SourceTvPort.HasValue)
    {
        Console.WriteLine($"SourceTV Port: {informations.SourceTvPort.Value}");
    }

    if (!string.IsNullOrEmpty(informations.SourceTvName))
    {
        Console.WriteLine($"SourceTV Name: {informations.SourceTvName}");
    }

    if (!string.IsNullOrEmpty(informations.Keywords))
    {
        Console.WriteLine($"Keywords: {informations.Keywords}");
    }
}

Console.WriteLine();

var players = await server.GetPlayersAsync();

Console.WriteLine($"Online Players: {players.Count}/{informations.MaxPlayers}");

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