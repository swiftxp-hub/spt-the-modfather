using System;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftXP.SPT.TheModfather.Updater.Services;

public interface IUpdateManager
{
    Task ProcessUpdatesAsync(IProgress<int> progress, CancellationToken cancellationToken);
}