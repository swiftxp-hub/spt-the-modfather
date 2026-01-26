using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using SwiftXP.SPT.TheModfather.Http.Interfaces;
using SwiftXP.SPT.TheModfather.Server.Configurations.Interfaces;
using SwiftXP.SPT.TheModfather.Server.Configurations.Models;
using SwiftXP.SPT.TheModfather.Server.Services.Interfaces;

namespace SwiftXP.SPT.TheModfather.Server.Http;

[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.PreSptModLoader)]
public class ModHttpListener(
    ISptLogger<ModHttpListener> logger,
    IServerConfigurationLoader serverConfigurationLoader,
    IServerFileInfoService serverFileInfoService,
    IServerFilesHashingService serverFilesHashingService)
    : IModHttpListener
{
    public bool CanHandle(MongoId sessionId, HttpContext context)
    {
        return context.Request.Path.StartsWithSegments(Constants.RoutePrefix, StringComparison.OrdinalIgnoreCase);
    }

    public async Task Handle(MongoId sessionId, HttpContext context)
    {
        try
        {
            string requestPath = context.Request.Path.Value ?? string.Empty;

            if (IsRoute(requestPath, Constants.RouteGetHashes))
            {
                await HandleGetFileHashesAsync(context);
            }
            else if (requestPath.StartsWith($"{Constants.RoutePrefix}{Constants.RouteGetFile}", StringComparison.OrdinalIgnoreCase))
            {
                await HandleGetFileAsync(context);
            }
            else if (IsRoute(requestPath, Constants.RouteGetServerConfiguration))
            {
                await HandleGetServerConfigurationAsync(context);
            }
            else
            {
                await HandleUnknownRouteAsync(context, requestPath);
            }
        }
        catch (Exception ex)
        {
            logger.Error($"[The Modfather] Error handling request: {ex.Message}");

            context.Response.StatusCode = 500;
        }
    }

    private async Task HandleGetFileHashesAsync(HttpContext context)
    {
        Dictionary<string, string> result = serverFilesHashingService.GetServerFileHashes();

        await context.Response.WriteAsJsonAsync(result, context.RequestAborted);
    }

    private async Task HandleGetFileAsync(HttpContext context)
    {
        string pathSegment = context.Request.Path.Value?
            .Replace($"{Constants.RoutePrefix}{Constants.RouteGetFile}/", "", StringComparison.OrdinalIgnoreCase) ?? "";

        string requestedFilePath = Uri.UnescapeDataString(pathSegment);
        if (string.IsNullOrWhiteSpace(requestedFilePath) || requestedFilePath.Contains(".."))
        {
            logger.Warning($"[The Modfather] Blocked suspicious path request: {requestedFilePath}");
            context.Response.StatusCode = 400;

            return;
        }

        FileInfo? fileInfo = serverFileInfoService.GetFileInfo(requestedFilePath);

        if (fileInfo != null && fileInfo.Exists)
        {
            context.Response.ContentType = "application/octet-stream";
            context.Response.ContentLength = fileInfo.Length;
            context.Response.StatusCode = 200;

            await context.Response.SendFileAsync(fileInfo.FullName, 0, null, context.RequestAborted);
        }
        else
        {
            logger.Warning($"[The Modfather] File not found: {requestedFilePath}");

            context.Response.StatusCode = 404;
        }
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators.
    private async Task HandleUnknownRouteAsync(HttpContext context, string requestPath)
#pragma warning restore CS1998 // This async method lacks 'await' operators.
    {
        logger.Warning($"[The Modfather] Unknown route: {requestPath}");

        context.Response.StatusCode = 404;
    }

    private async Task HandleGetServerConfigurationAsync(HttpContext context)
    {
        ServerConfiguration result = serverConfigurationLoader.LoadOrCreate();

        await context.Response.WriteAsJsonAsync(result, context.RequestAborted);
    }

    private static bool IsRoute(string path, string subRoute)
    {
        return path.Equals($"{Constants.RoutePrefix}{subRoute}", StringComparison.OrdinalIgnoreCase);
    }
}