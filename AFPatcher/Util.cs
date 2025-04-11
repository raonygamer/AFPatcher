using System.Diagnostics;
using AFPatcher.Models;

namespace AFPatcher;

public class ScopeInfo(string originalText, int index, int length, string scopeText)
{
    public string OriginalText => originalText;
    public int Index => index;
    public int Length => length;
    public string ScopeText => scopeText;
}

public class Util
{
    public static string FlattenString(string str)
    {
        return str.Replace("\r\n", "").Replace("\r", "").Replace("\t", "");
    }

    public static Process? InvokeFFDec(params string[] arguments)
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
                Console.WriteLine(await (process?.StandardOutput.ReadLineAsync() ?? Task.FromResult<string>("")!));
            }
        });
        
        return process;
    }

    public static ScopeInfo? FindNextScope(string text, int startIndex)
    {
        if (startIndex < 0 || startIndex >= text.Length)
        {
            return null;
        }

        int braceLevel = 0;
        bool insideScope = false;
        int scopeStart = -1;

        for (int i = startIndex; i < text.Length; i++)
        {
            if (text[i] == '{')
            {
                if (!insideScope)
                {
                    insideScope = true;
                }

                if (braceLevel == 0)
                {
                    scopeStart = i;
                }
                braceLevel++;
            }
            else if (text[i] == '}')
            {
                braceLevel--;
                if (insideScope && braceLevel == 0)
                {
                    return new ScopeInfo(text, scopeStart, i - scopeStart + 1, text.Substring(scopeStart, i - scopeStart + 1));
                }
            }
        }

        return null;
    }
}