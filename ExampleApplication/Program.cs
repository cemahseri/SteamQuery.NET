using System.Text;
using SteamQuery;

Console.OutputEncoding = Encoding.UTF8;

// Pick a server from: https://www.gametracker.com/search/cs/
using var server = new GameServer("127.0.0.1:27015");

await server.PerformQueryAsync();

var information = server.Information;

Console.WriteLine($"Protocol: {information.ProtocolVersion}");
Console.WriteLine($"Server Name: {information.ServerName}");
Console.WriteLine($"Folder: {information.Folder}");
Console.WriteLine($"Game Name: {information.GameName}");
Console.WriteLine($"Players: {information.OnlinePlayers}/{information.MaxPlayers}");
Console.WriteLine($"Bots: {information.Bots}");
Console.WriteLine($"Server Type: {information.ServerType}");
Console.WriteLine($"Environment: {information.Environment}");
Console.WriteLine($"Visible: {information.Visible}");
Console.WriteLine($"VAC Secured: {information.VacSecured}");
Console.WriteLine($"Version: {information.Version}");

if (information.IsHalfLifeMod.HasValue)
{
    Console.WriteLine();

    Console.WriteLine($"Is Half-Life Mod: {information.IsHalfLifeMod}");

    if (information.IsHalfLifeMod == true)
    {
        Console.WriteLine($"Mod Link: {information.HalfLifeMod.Link}");
        Console.WriteLine($"Mod Download Link: {information.HalfLifeMod.DownloadLink}");
        Console.WriteLine($"Mod Version: {information.HalfLifeMod.Version}");
        Console.WriteLine($"Size in Bytes: {information.HalfLifeMod.SizeInBytes}");
        Console.WriteLine($"Is Multiplayer Only: {information.HalfLifeMod.IsMultiplayerOnly}");
        Console.WriteLine($"Has Own DLL: {information.HalfLifeMod.HasOwnDll}");
    }
}

if (information.ExtraDataFlag.HasValue)
{
    Console.WriteLine();

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

foreach (var player in server.Players)
{
    Console.WriteLine($"[{player.Index}] [{player.Duration}] {player.Score} - {player.Name}");
}

Console.WriteLine();

foreach (var rule in server.Rules)
{
    Console.WriteLine($"{rule.Name} = {rule.Value}");
}

Console.ReadKey(true);
