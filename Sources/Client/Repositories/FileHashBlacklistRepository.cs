using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SPT.Common.Http;
using SwiftXP.SPT.TheModfather.Client.Data;

namespace SwiftXP.SPT.TheModfather.Client.Repositories;

public class FileHashBlacklistRepository() : IFileHashBlacklistRepository
{
    public async Task<FileHashBlacklist> LoadAsync(CancellationToken cancellationToken = default)
    {
        string json = await RequestHandler.GetJsonAsync($"{Constants.RoutePrefix}{Constants.RouteGetFileHashBlacklist}");

        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("Empty JSON-response");

        FileHashBlacklist? fileHashBlacklist = await Task.Run(() =>
        {
            return JsonConvert.DeserializeObject<FileHashBlacklist>(json);
        }, cancellationToken).ConfigureAwait(false);

        return fileHashBlacklist ?? throw new InvalidOperationException("JSON-response could not be deserialized");
    }
}