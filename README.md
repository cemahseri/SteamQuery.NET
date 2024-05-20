[![NuGet Version (SteamQuery.NET)](https://img.shields.io/nuget/v/SteamQuery.NET?style=for-the-badge&color=D800FF)](https://www.nuget.org/packages/SteamQuery.NET)
[![NuGet Downloads (SteamQuery.NET)](https://img.shields.io/nuget/dt/SteamQuery.NET?style=for-the-badge&color=D800FF)](https://www.nuget.org/packages/SteamQuery.NET)

# SteamQuery.NET
Yet another [Steam server queries](https://developer.valvesoftware.com/wiki/Server_queries) .NET wrapper.

Supports Source, GoldSource, and obsolete GoldSource protocols.

# Usage Example
You can check the ExampleApplication project!
```csharp
var server = new GameServer("127.0.0.1:27015");

await server.PerformQueryAsync();

Console.WriteLine($"Server Name: {server.Information.ServerName}"); // Server Name: [TR] AnneTokatlayan Pro Public
Console.WriteLine($"Players: {server.Information.OnlinePlayers}/{server.Information.MaxPlayers}"); // Players: 13/37
Console.WriteLine($"Rule Count: {server.Rules.Count}"); // Rule Count: 420

server.Close();
```

Or instead of performing all queries, you can specify which queries should be performed and also use custom timeouts;
```csharp
var server = new GameServer("127.0.0.1:27015")
{
    SendTimeout = 1337,
    ReceiveTimeout = 7331
};

await server.PerformQueryAsync(SteamQueryA2SQuery.Information | SteamQueryA2SQuery.Rules);

Console.WriteLine($"Server Name: {server.Information.ServerName}"); // Server Name: [TR] AnneTokatlayan Pro Public
Console.WriteLine($"Players: {server.Players.Count}/{server.MaxPlayers}"); // Output will be like "Players: 0/31" because you did not perform the Players query.
Console.WriteLine($"Rule Count: {server.Rules.Count}"); // Rule Count: 420

server.Close();
```

Or you can use `using` keyword to let .NET take care of the disposal process of server object;
```csharp
using var server = new GameServer("127.0.0.1", 27015);

var information = await server.GetInformationAsync();
Console.WriteLine($"Server Name: {information.ServerName}"); // Server Name: [TR] AnneTokatlayan Pro Public

var players = await server.GetPlayersAsync();
Console.WriteLine($"Players: {players.Count}/{information.MaxPlayers}"); // Players: 13/37

var rules = await server.GetRulesAsync();
foreach (var rule in rules)
{
    Console.WriteLine($"{rule.Name} = {rule.Value}");
}
```

You can also use a host name, instead of IP address. And also the synchronous methods!
```csharp
using var server = new GameServer("localhost:27015");

var information = server.GetInformation();

Console.WriteLine($"Server Name: {information.ServerName}"); // Server Name: [TR] AnneTokatlayan Pro Public
```

FYI: Before you perform any queries, related properties will have default values.
```csharp
using var server = new GameServer("localhost:27015");

Console.WriteLine($"Server Name: {server.Information.ServerName}"); // Output will be like "Server Name: " because you did not perform the Information query.

var information await server.GetInformationAsync();

Console.WriteLine($"Server Name: {server.Information.ServerName}"); // Server Name: [TR] AnneTokatlayan Pro Public
Console.WriteLine($"Server Name: {information.ServerName}"); // Server Name: [TR] AnneTokatlayan Pro Public
```

# Supported Frameworks
| | ❌ Not Supported | ✅ Supported |
|-| :---: | :---: |
| **.NET**            |             | **5.0-8.0** |
| **.NET Core**       | **1.0-1.1** | **2.0-3.1** |
| **.NET Framework**<sup>[1]</sup>  | **1.0-4.6** | **4.6.1**<sup>[2]</sup>**-4.8.1** |

[[1]](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0#select-net-standard-version)
>The versions listed for .NET Framework apply to .NET Core 2.0 SDK and later versions of the tooling. Older versions used a different mapping for .NET Standard 1.5 and higher.

[[2]](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0#select-net-standard-version)
>The versions listed here represent the rules that NuGet uses to determine whether a given .NET Standard library is applicable. While NuGet considers .NET Framework 4.6.1 as supporting .NET Standard 1.5 through 2.0, there are several issues with consuming .NET Standard libraries that were built for those versions from .NET Framework 4.6.1 projects. For .NET Framework projects that need to use such libraries, we recommend that you upgrade the project to target .NET Framework 4.7.2 or higher.

# To-Do
- bzip2 decompression. *(It's kind of implemented. I just couldn't find any server that uses bzip2 compression. If you know any, just hit me up. I'd appreciate it.)*
- Master Server Query Protocol.
- Source RCON Protocol.

## For More Information
If you want to learn more about Steam's server queries and their weird behaviors, check [Steam server queries developer community page](https://developer.valvesoftware.com/wiki/Server_queries) and [talk page about it](https://developer.valvesoftware.com/wiki/Talk:Server_queries).
