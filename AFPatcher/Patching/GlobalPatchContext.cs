using System.Collections;
using System.Text.RegularExpressions;
using AFPatcher.Utility;

namespace AFPatcher.Patching;

public class GlobalPatchContext(Dictionary<string, object> tags)
{
    public Dictionary<string, object> Tags { get; } = tags;
    
    public void AddTag(string key, object value) => Tags.Add(key, value);
    public bool RemoveTag(string key) => Tags.Remove(key);
    public object? GetTag(string key) => Tags.GetValueOrDefault(key);
    public bool TryGetTag(string key, out object? value) => Tags.TryGetValue(key, out value);

    public Dictionary<string, object> Flatten()
    {
        Dictionary<string, object> FlattenDict(object obj, string prefix = "")
        {
            var result = new Dictionary<string, object>();

            if (obj is IDictionary dict && obj.GetType().IsGenericType &&
                obj.GetType().GetGenericArguments()[0] == typeof(string))
            {
                foreach (DictionaryEntry entry in dict)
                {
                    string key = string.IsNullOrEmpty(prefix) ? entry.Key.ToString() ?? "" : $"{prefix}.{entry.Key}";
                    var value = entry.Value;
                    if (value is null)
                        continue;
                    
                    var nested = FlattenDict(value, key);
                    foreach (var kvp in nested)
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                }
            }
            else
            {
                result[prefix] = obj;
            }

            return result;
        }

        return FlattenDict(Tags);
    }

    public bool ExpandTags(ref string text)
    {
        var matches = Regex.Matches(text, @"\{\[(\s*[a-zA-Z0-9 ._]+\s*)\]\}");
        var successReplacedTags = 0;
        var tags = Flatten();
        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            if (!match.Groups[1].Success || !tags.TryGetValue(match.Groups[1].Value.TrimStart().TrimEnd(), out var tag))
                continue;
            text = text.ReplaceFirst(match.Value, $"{tag}");
            successReplacedTags++;
        }
        return successReplacedTags >= matches.Count;
    }
}