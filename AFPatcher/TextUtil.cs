using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace AFPatcher;

public class TextUtil
{
    public static string InsertTextAtGroupIndex(string text, string textToInsert,
        [StringSyntax("Regex")] string pattern, bool afterGroup = false)
    {
        var match = Regex.Match(text, pattern);
        if (!match.Success || match.Groups.Count < 2)
            return text;
        return InsertTextAt(text, textToInsert, match.Groups[1].Index + (afterGroup ? match.Groups[1].Length : 0));
    }
    
    public static string InsertTextAt(string text, string textToInsert, int position)
    {
        if (position < 0 || position >= text.Length)
            return text;
        return text.Insert(position, textToInsert);
    }
}