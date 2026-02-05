using SwiftXP.SPT.TheModfather.Client.Data;

namespace SwiftXP.SPT.TheModfather.Client.Contexts;

public class PluginContext(string baseDirectory,
    ClientExcludes clientExcludes,
    ClientManifest? clientManifest) : IPluginContext
{
    public string BaseDirectory { get; } = baseDirectory;

    public ClientExcludes ClientExcludes { get; } = clientExcludes;

    public ClientManifest? ClientManifest { get; } = clientManifest;
}