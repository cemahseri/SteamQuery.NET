[![NuGet Version (SteamQuery.NET)](https://img.shields.io/nuget/v/SteamQuery.NET?style=for-the-badge&color=D800FF)](https://www.nuget.org/packages/SteamQuery.NET)
[![NuGet Downloads (SteamQuery.NET)](https://img.shields.io/nuget/dt/SteamQuery.NET?style=for-the-badge&color=D800FF)](https://www.nuget.org/packages/SteamQuery.NET)

# SteamQuery.NET
Yet another [Steam server queries](https://developer.valvesoftware.com/wiki/Server_queries) .NET wrapper.

Supports GoldSource and Source protocols and The Ship: Murder Party, SiN, and Rag Doll Kung Fu servers.

# Usage Example
You can check the ExampleApplication project!
```csharp
var server = new Server("127.0.0.1:27015");

var informations = await server.GetInformationsAsync();

Console.WriteLine($"Server Name: {informations.ServerName}");

server.Close();
```

Or you can use `using` keyword to let .NET take care of the disposal process of server object;
```csharp
using var server = new Server("127.0.0.1", 27015);

var informations = await server.GetInformationsAsync();
Console.WriteLine($"Server Name: {informations.ServerName}");

var players = await server.GetPlayersAsync();
Console.WriteLine($"Online Players: {players.Count}/{informations.MaxPlayers}");

var rules = await server.GetRulesAsync();
foreach (var rule in rules)
{
    Console.WriteLine($"{rule.Name} = {rule.Value}");
}
```

You can also use a host name, instead of IP address. And also the synchronous methods!
```csharp
using var server = new Server("localhost:27015");

var informations = server.GetInformations();

Console.WriteLine($"Server Name: {informations.ServerName}");
```

# Supported Frameworks
| | ❌ Not Supported | ✅ Supported |
|-| :---: | :---: |
| **.NET**            |             | **5.0-8.0** |
| **.NET Core**       | **1.0-1.1** | **2.0-3.1** |
| **.NET Framework**<sup>[1]</sup>  | **1.0-4.6** | **4.6.1<sup>[2]</sup>-4.8.1** |

[[1]](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0#select-net-standard-version)
>The versions listed for .NET Framework apply to .NET Core 2.0 SDK and later versions of the tooling. Older versions used a different mapping for .NET Standard 1.5 and higher.

[[2]](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0#select-net-standard-version)
>The versions listed here represent the rules that NuGet uses to determine whether a given .NET Standard library is applicable. While NuGet considers .NET Framework 4.6.1 as supporting .NET Standard 1.5 through 2.0, there are several issues with consuming .NET Standard libraries that were built for those versions from .NET Framework 4.6.1 projects. For .NET Framework projects that need to use such libraries, we recommend that you upgrade the project to target .NET Framework 4.7.2 or higher.

# To-Do
- Obsolete, pre-Steam GoldSource protocol.
- bzip2 decompression. *(It's kind of implemented. I just couldn't find any server that uses bzip2 compression. If you know any, just hit me up. I'd appreciate it.)*

## For More Information
If you want to learn more about Steam's server queries and their weird behaviors, check [Steam server queries developer community page](https://developer.valvesoftware.com/wiki/Server_queries) and [talk page about it](https://developer.valvesoftware.com/wiki/Talk:Server_queries).
