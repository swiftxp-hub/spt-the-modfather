using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using SwiftXP.SPT.TheModfather.Server.Services.Interfaces;

namespace SwiftXP.SPT.TheModfather;

[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.PreSptModLoader)]
public class ModHttpListener(
    ISptLogger<ModHttpListener> logger,
    IServerFileInfoService serverFileInfoService,
    IServerFilesHashingService serverFilesHashingService)
    : IModHttpListener
{
    private const string RoutePrefix = "/theModfather";
    
    private const string RouteGetHashes = "/getFileHashes";
    
    private const string RouteGetFile = "/getFile";

    public bool CanHandle(MongoId sessionId, HttpContext context)
    {
        return context.Request.Path.StartsWithSegments(RoutePrefix, StringComparison.OrdinalIgnoreCase);
    }

    public async Task Handle(MongoId sessionId, HttpContext context)
    {
        try
        {
            string requestPath = context.Request.Path.Value ?? string.Empty;

            if (IsRoute(requestPath, RouteGetHashes))
            {
                await HandleGetFileHashesAsync(context);
            }
            else if (requestPath.StartsWith($"{RoutePrefix}{RouteGetFile}", StringComparison.OrdinalIgnoreCase))
            {
                await HandleGetFileAsync(context);
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
        Dictionary<string, string> result = serverFilesHashingService.Get();

        await context.Response.WriteAsJsonAsync(result, context.RequestAborted);
    }

    private async Task HandleGetFileAsync(HttpContext context)
    {
        string pathSegment = context.Request.Path.Value?
            .Replace($"{RoutePrefix}{RouteGetFile}/", "", StringComparison.OrdinalIgnoreCase) ?? "";

        string requestedFilePath = Uri.UnescapeDataString(pathSegment);
        if (string.IsNullOrWhiteSpace(requestedFilePath) || requestedFilePath.Contains(".."))
        {
            logger.Warning($"[The Modfather] Blocked suspicious path request: {requestedFilePath}");
            context.Response.StatusCode = 400; // Bad Request

            return;
        }

        FileInfo? fileInfo = serverFileInfoService.Get(requestedFilePath);

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

    private static async Task HandleUnknownRouteAsync(HttpContext context, string requestPath)
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync($"[The Modfather] Unknown route '{requestPath}'", context.RequestAborted);
    }

    private static bool IsRoute(string path, string subRoute)
    {
        return path.Equals($"{RoutePrefix}{subRoute}", StringComparison.OrdinalIgnoreCase);
    }
}