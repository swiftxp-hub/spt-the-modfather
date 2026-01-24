using SwiftXP.SPT.TheModfather.Client.Configurations.Models;

namespace SwiftXP.SPT.TheModfather.Client.Configurations.Interfaces;

public interface IClientConfigurationLoader
{
    ClientConfiguration LoadOrCreate();
}