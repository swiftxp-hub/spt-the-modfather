using System.Threading;
using System.Threading.Tasks;
using SwiftXP.SPT.TheModfather.Client.Data;

namespace SwiftXP.SPT.TheModfather.Client.Repositories;

public interface IFileHashBlacklistRepository
{
    Task<FileHashBlacklist> LoadAsync(CancellationToken cancellationToken = default);
}