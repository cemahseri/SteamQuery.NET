namespace SteamQuery.Models;

public class MasterServerQueryFiltersBuilder
{
    private readonly List<Action<MasterServerQueryFilters>> _actions = [];
    
    public MasterServerQueryFiltersBuilder WithServerName(string serverName) => Configure(a => a.ServerName = serverName);

    public MasterServerQueryFiltersBuilder WithDedicated(bool dedicated) => Configure(a => a.Dedicated = dedicated);

    public MasterServerQueryFiltersBuilder WithSecure(bool secure) => Configure(a => a.Secure = secure);

    public MasterServerQueryFiltersBuilder WithGameDirectory(string gameDirectory) => Configure(a => a.GameDirectory = gameDirectory);

    public MasterServerQueryFiltersBuilder WithMap(string map) => Configure(a => a.Map = map);

    public MasterServerQueryFiltersBuilder WithOnLinux(bool onLinux) => Configure(a => a.OnLinux = onLinux);

    public MasterServerQueryFiltersBuilder WithPublic(bool @public) => Configure(a => a.Public = @public);

    public MasterServerQueryFiltersBuilder WithEmpty(bool? empty) => Configure(a => a.Empty = empty);
    public MasterServerQueryFiltersBuilder WithNoEmpty() => WithEmpty(null);

    public MasterServerQueryFiltersBuilder WithNotFull(bool notFull) => Configure(a => a.NotFull = notFull);

    public MasterServerQueryFiltersBuilder WithSpectatorProxy(bool spectatorProxy) => Configure(a => a.SpectatorProxy = spectatorProxy);

    public MasterServerQueryFiltersBuilder WithAppId(int appId) => Configure(a => a.AppId = appId);

    public MasterServerQueryFiltersBuilder WithNotAppId(int notAppId) => Configure(a => a.NotAppId = notAppId);

    public MasterServerQueryFiltersBuilder WithWhitelist(bool whitelist) => Configure(a => a.Whitelist = whitelist);

    public MasterServerQueryFiltersBuilder WithGameTypes(params IEnumerable<string> gameTypes) => Configure(a => a.GameTypes.AddRange(gameTypes));
    public MasterServerQueryFiltersBuilder WithNoGameTypes() => Configure(a => a.GameTypes.Clear());

    public MasterServerQueryFiltersBuilder WithGameData(params IEnumerable<string> gameData) => Configure(a => a.GameData.AddRange(gameData));
    public MasterServerQueryFiltersBuilder WithNoGameData() => Configure(a => a.GameData.Clear());
    public MasterServerQueryFiltersBuilder WithGameDataOr(params IEnumerable<string> gameDataOr) => Configure(a => a.GameDataOr.AddRange(gameDataOr));
    public MasterServerQueryFiltersBuilder WithNoGameDataOr() => Configure(a => a.GameDataOr.Clear());

    public MasterServerQueryFiltersBuilder WithVersion(string version) => Configure(a => a.Version = version);

    public MasterServerQueryFiltersBuilder WithUniqueIpAddress(bool uniqueIpAddress) => Configure(a => a.UniqueIpAddress = uniqueIpAddress);

    public MasterServerQueryFiltersBuilder WithIpAddress(string ipAddress) => Configure(a => a.IpAddress = ipAddress);
    
    private MasterServerQueryFiltersBuilder Configure(Action<MasterServerQueryFilters> action)
    {
        lock (_actions)
        {
            _actions.Add(action);
        }

        return this;
    }

    public MasterServerQueryFilters Build()
    {
        var model = new MasterServerQueryFilters();

        foreach (var action in _actions)
        {
            action(model);
        }

        return model;
    }
}