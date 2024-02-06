[![NuGet Version (SteamQuery.NET)](https://img.shields.io/nuget/v/SteamQuery.NET?style=for-the-badge&color=D800FF)](https://www.nuget.org/packages/SteamQuery.NET)
[![NuGet Downloads (SteamQuery.NET)](https://img.shields.io/nuget/dt/SteamQuery.NET?style=for-the-badge&color=D800FF)](https://www.nuget.org/packages/SteamQuery.NET)

# SteamQuery.NET
Yet another [Steam server queries](https://developer.valvesoftware.com/wiki/Server_queries) .NET wrapper.

# To-Do
- [x] Source protocol.
- [x] GoldSource protocol.
- [ ] Obsolete, pre-Steam GoldSource protocol.
- [x] The Ship: Murder Party, SiN, and Rag Doll Kung Fu support.
- [x] Split packets.
- [ ] bzip2 decompression. *(It's kind of implemented. I just couldn't find any server that uses bzip2 compression. If you know any, just hit me up. I'd appreciate it.)*
- [ ] Add CancellationToken as parameter to asynchronous methods.
- [ ] Use ConfigureAwait Fody weaver.
- [ ] Target older frameworks.

# Example
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

## For More Information
If you want to learn more about Steam's server queries and their weird behaviors, check [Steam server queries developer community page](https://developer.valvesoftware.com/wiki/Server_queries) and [talk page about it](https://developer.valvesoftware.com/wiki/Talk:Server_queries).
