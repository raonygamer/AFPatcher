using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace AFPatcher.Utility;

public class ScopeInfo(string originalText, int index, int length, string scopeText)
{
    public string OriginalText => originalText;
    public int Index => index;
    public int Length => length;
    public string ScopeText => scopeText;
}

public class Scope
{
    public delegate string ScopeModificationDelegate(ScopeInfo scopeInfo);
    public delegate void ScopeCompleteDelegate(ScopeInfo oldScopeInfo, ScopeInfo newScopeInfo);

    public static ScopeInfo Modify(string text, [StringSyntax("Regex")] string scopeFindingRegex, int groupIndex,
        ScopeModificationDelegate scopeModificationDelegate, ScopeCompleteDelegate? scopeCompleteDelegate)
    {
        var regex = new Regex(scopeFindingRegex);
        if (!regex.GetGroupNumbers().Contains(groupIndex))
        {
            throw new Exception($"The scope finding pattern '{scopeFindingRegex}' lacks the first group capture.");
        }
        var match = regex.Match(text);
        if (!match.Success)
        {
            throw new Exception($"Could not find scope with pattern '{scopeFindingRegex}'.");
        }

        return Modify(text, match.Groups[groupIndex].Index + match.Groups[groupIndex].Length, scopeModificationDelegate, scopeCompleteDelegate);
    }
    
    public static ScopeInfo Modify(string text, [StringSyntax("Regex")] string scopeFindingRegex,
        ScopeModificationDelegate scopeModificationDelegate, ScopeCompleteDelegate? scopeCompleteDelegate)
    {
        var regex = new Regex(scopeFindingRegex);
        if (!regex.GetGroupNumbers().Contains(1))
        {
            throw new Exception($"The scope finding pattern '{scopeFindingRegex}' lacks the first group capture.");
        }
        var match = regex.Match(text);
        if (!match.Success)
        {
            throw new Exception($"Could not find scope with pattern '{scopeFindingRegex}'.");
        }

        return Modify(text, match.Groups[1].Index + match.Groups[1].Length, scopeModificationDelegate, scopeCompleteDelegate);
    }
    
    public static ScopeInfo Modify(string text, int index, ScopeModificationDelegate scopeModificationDelegate, ScopeCompleteDelegate? scopeCompleteDelegate)
    {
        if (index < 0 || index >= text.Length)
        {
            throw new Exception($"Index {index} is out of range.");
        }

        var braceLevel = 0;
        var insideScope = false;
        var scopeStart = -1;

        for (var i = index; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '{':
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
                    break;
                }
                case '}':
                {
                    braceLevel--;
                    if (insideScope && braceLevel == 0)
                    {
                        var scopeText = text.Substring(scopeStart, i - scopeStart + 1);
                        var scopeInfo = new ScopeInfo(text, scopeStart, i - scopeStart + 1, scopeText);
                        scopeText = scopeModificationDelegate(scopeInfo);
                        var newScopeInfo = new ScopeInfo(text, scopeStart, scopeText.Length, scopeText);
                        scopeCompleteDelegate?.Invoke(scopeInfo, newScopeInfo);
                        return newScopeInfo;
                    }
                    break;
                }
            }
        }

        throw new Exception($"Scope was not found.");
    }
}