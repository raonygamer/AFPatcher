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
#if DEBUG
                Console.WriteLine(await (process?.StandardOutput.ReadLineAsync() ?? Task.FromResult<string>("")!));
#else
                await (process?.StandardOutput.ReadLineAsync() ?? Task.FromResult<string>("")!);
#endif
            }
        });
        
        return process;
    }
    
    public static string ReplaceFirst(this string input, string search, string replace)
    {
        var pos = input.IndexOf(search, StringComparison.Ordinal);
        if (pos < 0) 
            throw new ArgumentException($"Could not find '{search}' in string");
        return input.Substring(0, pos) + replace + input.Substring(pos + search.Length);
    }
    
    public static string InsertTextAtGroupIndex(this string text, string textToInsert,
        [StringSyntax("Regex")] string pattern, bool afterGroup = false)
    {
        var match = Regex.Match(text, pattern);
        if (!match.Success || match.Groups.Count < 2)
            throw new Exception($"Invalid Regex: {pattern}");
        return InsertTextAt(text, textToInsert, match.Groups[1].Index + (afterGroup ? match.Groups[1].Length : 0));
    }
    
    public static string InsertTextAt(this string text, string textToInsert, int position)
    {
        if (position < 0 || position >= text.Length)
            throw new ArgumentOutOfRangeException(nameof(position));
        return text.Insert(position, textToInsert);
    }

    public static string ExpandTags(this string text, GlobalPatchContext ctx, bool throwIfPartial = true)
    {
        if (!ctx.ExpandTags(ref text) && throwIfPartial)
            throw new Exception("Could not expand all the tags!");
        return text;
    }
}