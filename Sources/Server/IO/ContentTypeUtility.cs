using Microsoft.AspNetCore.StaticFiles;

namespace SwiftXP.SPT.TheModfather.Server.IO;

public static class ContentTypeUtility
{
    private static readonly FileExtensionContentTypeProvider s_contentTypeProvider = new();

    public static string GetContentType(string filePath, string extension)
    {
        if (s_contentTypeProvider.TryGetContentType(filePath, out string? contentType))
            return contentType;

        return extension.ToLowerInvariant() switch
        {
            // Web, Style & Scripts
            ".html" => "text/html",
            ".htm" => "text/html",
            ".css" => "text/css",
            ".js" => "text/javascript",
            ".mjs" => "text/javascript",
            ".json" => "application/json",
            ".less" => "text/css",
            ".scss" => "text/x-scss",
            ".rb" => "application/x-ruby",
            ".bat" => "application/x-msdos-program",

            // Configuration & Logs
            ".txt" => "text/plain",
            ".log" => "text/plain",
            ".ini" => "text/plain",
            ".cfg" => "text/plain",
            ".conf" => "text/plain",
            ".config" => "text/xml",
            ".info" => "text/plain",
            ".policy" => "text/plain",
            ".md" => "text/markdown",
            ".csv" => "text/csv",

            // Images & Icons
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            ".tiff" => "image/tiff",

            // Audio & Video
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".m4a" => "audio/mp4",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mov" => "video/quicktime",

            // Fonts
            ".ttf" => "font/ttf",
            ".otf" => "font/otf",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".eot" => "application/vnd.ms-fontobject",

            // Documents & Specialized
            ".pdf" => "application/pdf",
            ".msg" => "application/vnd.ms-outlook",
            ".pcl" => "application/vnd.hp-pcl",
            ".zip" => "application/zip",
            ".7z" => "application/x-7z-compressed",
            ".rar" => "application/x-rar-compressed",

            _ => "application/octet-stream"
        };
    }
}