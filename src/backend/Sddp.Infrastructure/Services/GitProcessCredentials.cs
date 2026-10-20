using System.Diagnostics;
using System.Text;
using Sddp.Abstractions.Interfaces;
namespace Sddp.Infrastructure.Services;

public static class GitProcessCredentials
{
    public static void Configure(ProcessStartInfo start, GitRemoteAccess? access)
    {
        start.Environment["GIT_TERMINAL_PROMPT"] = "0";
        start.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        start.Environment["GIT_CONFIG_GLOBAL"] = OperatingSystem.IsWindows() ? "NUL" : "/dev/null";
        var settings = new List<(string Key, string Value)> {
            ("credential.helper", ""), ("http.followRedirects", "false"), ("protocol.allow", "never"),
            ("protocol.https.allow", "always"), ("protocol.http.allow", "always") };
        if (!string.IsNullOrEmpty(access?.Token))
        {
            var header = "Authorization: Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{access.Username}:{access.Token}"));
            settings.Add(($"http.{access.Url}.extraHeader", header));
        }
        start.Environment["GIT_CONFIG_COUNT"] = settings.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        for (var i = 0; i < settings.Count; i++) {
            start.Environment[$"GIT_CONFIG_KEY_{i}"] = settings[i].Key;
            start.Environment[$"GIT_CONFIG_VALUE_{i}"] = settings[i].Value;
        }
    }
}
