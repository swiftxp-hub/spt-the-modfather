using SwiftXP.SPT.TheModfather.Server.Configurations.Models;

namespace SwiftXP.SPT.TheModfather.Server.Configurations.Interfaces;

public interface IServerConfigurationLoader
{
    ServerConfiguration LoadOrCreate();
}