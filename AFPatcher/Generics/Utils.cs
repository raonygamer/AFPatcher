using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using AFPatcher.Patching;
using SharpFileDialog;

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


        return process;
    }

    public static string? OpenFile(IEnumerable<(IEnumerable<string> Extensions, string Name)> filters,
        string? defaultPath)
    {
        if (!NativeFileDialog.OpenDialog(
                filters.Select(t => new NativeFileDialog.Filter()
                {
                    Extensions = t.Extensions.ToArray(),
                    Name = t.Name
                }).ToArray(),
                defaultPath,
                out var file))
            return null;
        return file;
    }

    public static string? SaveFile(IEnumerable<(IEnumerable<string> Extensions, string Name)> filters,
        string? defaultPath)
    {
        if (!NativeFileDialog.SaveDialog(
                filters.Select(t => new NativeFileDialog.Filter()
                {
                    Extensions = t.Extensions.ToArray(),
                    Name = t.Name
                }).ToArray(),
                defaultPath,
                out var file))
            return null;
        return file;
    }
}