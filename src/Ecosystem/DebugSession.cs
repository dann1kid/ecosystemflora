using System;
using System.IO;

namespace WildFarming.Ecosystem
{
    internal static class DebugSession
    {
        const string LogPath = @"d:\vintage_story_mods\vs-wildfarming\debug-d2af1c.log";
        const string SessionId = "d2af1c";

        public static void Log(string hypothesisId, string location, string message, string dataJson = "{}")
        {
            try
            {
                long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string line = $"{{\"sessionId\":\"{SessionId}\",\"timestamp\":{ts},\"hypothesisId\":\"{hypothesisId}\",\"location\":\"{location}\",\"message\":\"{EscapeJson(message)}\",\"data\":{dataJson}}}";
                File.AppendAllText(LogPath, line + "\n");
            }
            catch { }
        }

        static string EscapeJson(string s) => s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
    }
}
