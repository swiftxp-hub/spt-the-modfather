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
            if (IsRoute(context, s_pathGetServerManifest))
            {
                await HandleGetServerManifestAsync(context);
            }
            else if (IsRoute(context, s_pathGetFileHashBlacklist))
            {
                await HandleGetFileHashBlacklistAsync(context);
            }
            else if (context.Request.Path.StartsWithSegments(s_pathGetFile, out PathString remainingPath))
            {
                await HandleGetFileAsync(context, remainingPath);
            }
            else
            {
                await HandleUnknownRouteAsync(context, context.Request.Path);
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
        ServerManifest result = await serverManifestManager.GetServerManifestAsync();

        await context.Response.WriteAsJsonAsync(result, context.RequestAborted);
    }

    private async Task HandleGetFileHashBlacklistAsync(HttpContext context)
    {
        ServerConfiguration serverConfiguration = await serverConfigurationRepository.LoadOrCreateDefaultAsync(context.RequestAborted);

        await context.Response.WriteAsJsonAsync(serverConfiguration.FileHashBlacklist, context.RequestAborted);
    }

    private async Task HandleGetFileAsync(HttpContext context, PathString remainingPath)
    {
        string requestedFilePath = Uri.UnescapeDataString(remainingPath.Value?.TrimStart('/') ?? string.Empty);

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

    private Task HandleUnknownRouteAsync(HttpContext context, PathString path)
    {
        sptLogger.Warning($"{Constants.LoggerPrefix}[WARNING] Unknown route: {path}");
        context.Response.StatusCode = 404;

        return Task.CompletedTask;
    }

    private static bool IsRoute(HttpContext context, PathString route)
    {
        return context.Request.Path.Equals(route, StringComparison.OrdinalIgnoreCase);
    }
}