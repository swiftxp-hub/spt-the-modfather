using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using SwiftXP.SPT.TheModfather.Server.Data;
using SwiftXP.SPT.TheModfather.Server.IO;
using SwiftXP.SPT.TheModfather.Server.Repositories;
using SwiftXP.SPT.TheModfather.Server.Services;

namespace SwiftXP.SPT.TheModfather.Server.Http;

[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.PreSptModLoader)]
public class ModHttpListener(ISptLogger<ModHttpListener> sptLogger,
    IServerConfigurationRepository serverConfigurationRepository,
    IServerManifestManager serverManifestManager,
    IServerFileResolver serverFileResolver) : IModHttpListener
{
    private static readonly PathString s_pathGetServerManifest = new($"{Constants.RoutePrefix}{Constants.RouteGetServerManifest}");
    private static readonly PathString s_pathGetFile = new($"{Constants.RoutePrefix}{Constants.RouteGetFile}");
    private static readonly PathString s_pathGetFileHashBlacklist = new($"{Constants.RoutePrefix}{Constants.RouteGetFileHashBlacklist}");

    public bool CanHandle(MongoId sessionId, HttpContext context)
    {
        return context.Request.Path.StartsWithSegments(Constants.RoutePrefix, StringComparison.OrdinalIgnoreCase);
    }

    public async Task Handle(MongoId sessionId, HttpContext context)
    {
        try
        {
            PathString path = context.Request.Path;

            if (path.Equals(s_pathGetServerManifest, StringComparison.OrdinalIgnoreCase))
            {
                await HandleGetServerManifestAsync(context);
            }
            else if (path.StartsWithSegments(s_pathGetFile, StringComparison.OrdinalIgnoreCase))
            {
                await HandleGetFileAsync(context);
            }
            else if (path.Equals(s_pathGetFileHashBlacklist, StringComparison.OrdinalIgnoreCase))
            {
                await HandleGetFileHashBlacklistAsync(context);
            }
            else
            {
                await HandleUnknownRouteAsync(context, path);
            }
        }
        catch (Exception ex)
        {
            sptLogger.Error($"{Constants.LoggerPrefix}[ERROR] Error handling request: {ex.Message}");

            context.Response.StatusCode = 500;
        }
    }

    private async Task HandleGetServerManifestAsync(HttpContext context)
    {
        await Task.Delay(2000);

        ServerManifest result = await serverManifestManager.GetServerManifestAsync();

        await context.Response.WriteAsJsonAsync(result, context.RequestAborted);
    }

    private async Task HandleGetFileAsync(HttpContext context)
    {
        string requestedFilePath = string.Empty;
        if (context.Request.Path.StartsWithSegments(s_pathGetFile, out PathString remainingPath))
        {
            requestedFilePath = remainingPath.Value?.TrimStart('/') ?? string.Empty;
        }

        requestedFilePath = Uri.UnescapeDataString(requestedFilePath);

        if (string.IsNullOrWhiteSpace(requestedFilePath))
        {
            sptLogger.Warning($"{Constants.LoggerPrefix}[WARNING] Blocked suspicious file request: {requestedFilePath}");
            context.Response.StatusCode = 400;

            return;
        }

        ServerConfiguration serverConfiguration = await serverConfigurationRepository.LoadOrCreateDefaultAsync(context.RequestAborted);

        FileInfo? fileInfo = serverFileResolver.GetFileInfo(requestedFilePath, serverConfiguration.IncludePatterns, serverConfiguration.ExcludePatterns);
        if (fileInfo != null && fileInfo.Exists)
        {
            context.Response.ContentType = ContentTypeUtility.GetContentType(fileInfo.FullName, fileInfo.Extension);
            context.Response.ContentLength = fileInfo.Length;
            context.Response.StatusCode = 200;

            await context.Response.SendFileAsync(fileInfo.FullName, 0, null, context.RequestAborted);
        }
        else
        {
            sptLogger.Warning($"{Constants.LoggerPrefix}[WARNING] File not found: {requestedFilePath}");

            context.Response.StatusCode = 404;
        }
    }

    private async Task HandleGetFileHashBlacklistAsync(HttpContext context)
    {
        ServerConfiguration serverConfiguration = await serverConfigurationRepository.LoadOrCreateDefaultAsync(context.RequestAborted);

        await context.Response.WriteAsJsonAsync(serverConfiguration.FileHashBlacklist, context.RequestAborted);
    }

    private Task HandleUnknownRouteAsync(HttpContext context, PathString path)
    {
        sptLogger.Warning($"{Constants.LoggerPrefix}[WARNING] Unknown route: {path}");

        context.Response.StatusCode = 404;

        return Task.CompletedTask;
    }
}