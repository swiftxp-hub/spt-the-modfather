using SwiftXP.SPT.TheModfather.Client.Data;

namespace SwiftXP.SPT.TheModfather.Client.Contexts;

public interface IPluginContext
{
    string BaseDirectory { get; }

    ClientExcludes ClientExcludes { get; }

    ClientManifest? ClientManifest { get; }
}