using System.Collections;
using System.Text.RegularExpressions;
using AFPatcher.Utility;
using Patching.Utility;

namespace AFPatcher.Patching;

public class GlobalPatchContext(Dictionary<string, object> tags, PatchDescriptor[] descriptors)
{
    public Dictionary<string, object> Tags { get; } = tags;
    public List<string> AppliedPatches { get; } = [];
    public Dictionary<string, string> FailedPatches { get; } = [];
    public Dictionary<string, string> ChangedFiles { get; } = [];
    public IEnumerable<PatchDescriptor> PatchDescriptors { get; } = descriptors;
    public IDictionary<string, (PatchDescriptor Descriptor, PatchBase Patch)> FlattenedPatches { get; } = descriptors
        .SelectMany(d => d.Patches.Select(p => (Descriptor: d, Patch: p)))
        .ToDictionary(t => t.Patch.Id);
    
    public void AddTag(string key, object value) => Tags.Add(key, value);
    public bool RemoveTag(string key) => Tags.Remove(key);
    public object? GetTag(string key) => Tags.GetValueOrDefault(key);
    public bool TryGetTag(string key, out object? value) => Tags.TryGetValue(key, out value);

    public bool TryGetTag<TValue>(string key, out TValue? value)
    {
        var ret = Tags.TryGetValue(key, out var obj);
        value = (TValue?)obj;
        return ret;
    }

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

    public void StartPatching(IEnumerable<string> sortedIdentifiers, string exportedScriptsDirectory)
    {
        Log.TraceLine($"Patching process started...");
        foreach (var id in sortedIdentifiers)
        {
            var (desc, patch) = FlattenedPatches[id];
            try
            {
                PatchScript(desc, patch, exportedScriptsDirectory);
                Log.SuccessLine($"Patch '{patch.Id}' applied successfully to '{desc.ClassName}'.");
            }
            catch (Exception e)
            {
                Log.ErrorLine(e.Message);
            }
        }
        Log.TraceLine($"Patching finished...");
    }
    
    private void PatchScript(PatchDescriptor descriptor, PatchBase patch, string exportedScriptsDirectory)
    {
        // Check if patch was already applied
        if (AppliedPatches.Contains(patch.Id))
            return;
        
        // Check if any of the dependencies failed and throw in case
        var failedDependencies = FailedPatches.Keys.Intersect(patch.Dependencies).ToArray();
        if (failedDependencies.Length != 0)
        {
            var failedText = $"Failed to apply patch '{patch.Id}':";
            foreach (var failed in failedDependencies)
            {
                failedText += $"\n  Dependency '{failed}' wasn't applied:";
                failedText += $"\n    {FailedPatches[failed]}";
            }
            FailedPatches.Add(patch.Id, failedText);
            throw new PatchFailedException(failedText);
        }
        
        // Get script file contents
        var scriptFile = Path.Combine(exportedScriptsDirectory, $"scripts\\{descriptor.ClassName.Replace('.', '\\')}.as");
        var scriptContent = File.ReadAllText(scriptFile).Flatten();
        
        try
        {
            // Try to apply patch
            var result = patch.Apply(new PatchContext(this, scriptContent, descriptor));
            AppliedPatches.Add(patch.Id);
            File.WriteAllText(scriptFile, result.Text);
            
            // In case of success add the patch to the changed files
            ChangedFiles[descriptor.ClassName] = scriptFile;
        }
        catch (Exception e)
        {
            // In case of fail, register it and throw
            FailedPatches.Add(patch.Id, e.Message);
            throw;
        }
    }
}