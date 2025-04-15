using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace AFPatcher;

public class TextUtil
{
    public static string InsertAtGroupIndex(string text, string textToInsert,
        [StringSyntax("Regex")] string pattern, bool afterGroup = false)
    {
        var match = Regex.Match(text, pattern);
        if (!match.Success || match.Groups.Count < 2)
            return text;
        return InsertAt(text, textToInsert, match.Groups[1].Index + (afterGroup ? match.Groups[1].Length : 0));
    }
    
    public static string InsertAt(string text, string textToInsert, int position)
    {
        if (position < 0 || position >= text.Length)
            return text;
        return text.Insert(position, textToInsert);
    }

    public static string CutAt(string text, int start, int end)
    {
        if (start < 0 || start >= text.Length || end < 0 || end >= text.Length)
            return text;
        var f = text.Substring(0, start);
        var s = text.Substring(end);
        return f + s;
    }
    
    public static bool InsertAt(ref string text, string textToInsert, int position)
    {
        if (position < 0 || position >= text.Length)
            return false;
        text = text.Insert(position, textToInsert);
        return true;
    }

    public static bool CutAt(ref string text, int start, int end)
    {
        if (start < 0 || start >= text.Length || end < 0 || end >= text.Length)
            return false;
        var f = text.Substring(0, start);
        var s = text.Substring(end);
        text = f + s;
        return true;
    }
}