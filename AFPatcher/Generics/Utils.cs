using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using AFPatcher.Patching;

namespace AFPatcher.Utility;

public static class Utils
{
    public static string Flatten(this string str)
    {
        return str.Replace("\r\n", "").Replace("\r", "").Replace("\t", "");
    }

    public static Process? StartFlashDecompiler(params string[] arguments)
    {
        var info = new ProcessStartInfo
        {
            FileName = "ffdec/ffdec-cli.exe",
            Arguments = "-config paramNamesEnable=false,getLocalNamesFromDebugInfo=false" + " " +
                        string.Join(" ", arguments),
            RedirectStandardOutput = true
        };

        var process = Process.Start(info);
        Task.Run(async () =>
        {
            while (process?.HasExited == false)
            {
#if false
                Console.WriteLine(await (process?.StandardOutput.ReadLineAsync() ?? Task.FromResult<string>("")!));
#else
                await (process?.StandardOutput.ReadLineAsync() ?? Task.FromResult<string>("")!);
#endif
            }
        });

        return process;
    }

    
}