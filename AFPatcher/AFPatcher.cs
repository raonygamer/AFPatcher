using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AFPatcher.Models;
using Newtonsoft.Json;
using SharpFileDialog;

namespace AFPatcher;

class AFPatcher
{
    #region Singleton
    private static AFPatcher? _instance;
    public static AFPatcher Instance => _instance ??= new AFPatcher();
    
    static void Main(string[] args)
    {
        Instance.Start().GetAwaiter().GetResult();
    }
    #endregion

    public readonly string TemporaryDirectory;
    public readonly string DecompilationDirectory;
    
    public AFPatcher()
    {
        AppDomain.CurrentDomain.ProcessExit += (sender, args) => OnExit();
        TemporaryDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString().Replace("-", ""));
        DecompilationDirectory = Path.Combine(TemporaryDirectory, "decompiled");
        Console.WriteLine("Creating temporary directory...");
        Directory.CreateDirectory(DecompilationDirectory);
        Console.WriteLine($"Created temporary directory: {TemporaryDirectory}");
    }
    
    private async Task Start()
    {
        if (!NativeFileDialog.OpenDialog(
                [new NativeFileDialog.Filter { Extensions = ["swf"], Name = "Shockwave Files" }], null,
                out var swfFile) ||
            !File.Exists(swfFile))
        {
            Console.WriteLine("No swf file found.");
            return;
        }

        var patches = GetPatches();
        await DecompileGame(swfFile);

        var changedScripts = new List<string>();
        foreach (var desc in patches.PatchDescriptors)
        {
            var file = Path.Combine(DecompilationDirectory, "scripts",
                desc.FullyQualifiedName.Replace('.', '\\') + ".as");
            if (!File.Exists(file))
            {
                Console.WriteLine($"Qualified name '{desc.FullyQualifiedName}' does not exist.");
                Console.WriteLine();
                continue;
            }
            
            var text = await File.ReadAllTextAsync(file);
            Console.WriteLine($"Patching '{desc.FullyQualifiedName}'...");
            foreach (var patch in desc.Patches)
            {
                var result = patch.PatchFunction(new PatchContext(patches, text));
                if (result is null)
                {
                    Console.WriteLine($"    Couldn't apply the patch '{patch.Name}'.");
                    continue;
                }

                Console.WriteLine($"    Patch '{patch.Name}' successfully applied to '{desc.FullyQualifiedName}'.");
                text = result.ScriptText;
            }
            
            Console.WriteLine($"Patched '{desc.FullyQualifiedName}'.");
            Console.WriteLine();
            await File.WriteAllTextAsync(file, text);
            changedScripts.Add($"{desc.FullyQualifiedName} {file}");
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd",
            Arguments = $"/c start {DecompilationDirectory}",
            UseShellExecute = true
        });
        
        Directory.CreateDirectory(Path.Combine(TemporaryDirectory, "recompiled"));
        await (Util.InvokeFFDec([string.Join(" ", ["-replace", swfFile, Path.Combine(TemporaryDirectory, "recompiled", Path.GetFileName(swfFile)), ..changedScripts])])?.WaitForExitAsync() ?? Task.CompletedTask);
        Console.WriteLine($"Recompiled '{swfFile}'.");
        if (!NativeFileDialog.SaveDialog(
                [new NativeFileDialog.Filter() { Name = "Shockwave Files", Extensions = ["swf"] }], null,
                out var savingPath))
        {
            return;
        }

        if (!Directory.Exists(Path.GetDirectoryName(savingPath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(savingPath)!);
        }
        
        await File.WriteAllBytesAsync(savingPath, await File.ReadAllBytesAsync(Path.Combine(TemporaryDirectory, "recompiled", Path.GetFileName(swfFile))));
    }

    private async Task DecompileGame(string swfSource)
    {
        await (Util.InvokeFFDec("-export script", DecompilationDirectory, swfSource)?.WaitForExitAsync() ?? Task.CompletedTask);
    }

    private GamePatches GetPatches()
    {
        return new GamePatches(
            new GlobalPatchContext(new Dictionary<string, string>()
            {
                // Sets the zoom identifier name globally
                { "ZOOM_IDENTIFIER", "zoomFactor" },
                // Sets the check zoom identifier name globally
                { "CHECK_ZOOM_IDENTIFIER", "checkZoom" },
            }),
            [
            new PatchDescriptor("core.scene.Game", [
                new Patch("Add zoom variable", (ctx) =>
                {
                    // Gets the zoom identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier("ZOOM_IDENTIFIER", out var zoomIdentifier))
                        return null;
                    
                    // The text to insert
                    var insertingText = $@"public var {zoomIdentifier}:Number = 1;";
                    
                    // Flattens script to remove new lines and carriage returns
                    var flatScript = Util.FlattenString(ctx.ScriptText);
                    
                    // Add ZOOM variable to core.scene.Game
                    {
                        // Tries to match Game class declaration
                        var match = Regex.Match(flatScript, @"public class Game extends SceneBase{");
                        if (match.Success)
                        {
                            // Sets the position to the end of the match
                            var position = match.Index + match.Length;
                            
                            // Splits the first and the last part of the script text to insert text in between
                            var firstPart = flatScript.Substring(0, position);
                            var lastPart = flatScript.Substring(position);
                            
                            // Inserts text in between firstPart and lastPart
                            flatScript = $"{firstPart}{insertingText}{lastPart}";
                        }
                    }
                    
                    return new PatchResult(flatScript);
                })
            ]),
            new PatchDescriptor("core.states.gameStates.PlayState", [
                new Patch("Replace all camera zoomFocus calls for zoom", (ctx) =>
                {
                    // Gets the zoom identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier("ZOOM_IDENTIFIER", out var zoomIdentifier))
                        return null;
                    
                    // Flattens script to remove new lines and carriage returns
                    var flatScript = Util.FlattenString(ctx.ScriptText);
                    
                    // Searches all occurrences of g.camera.zoomFocus
                    var matches = Regex.Matches(flatScript, @"g\.camera\.zoomFocus\(\s*([\d.]+)\s*,\s*([\d.]+)\s*\);");
                    for (var i = 0; i < matches.Count; i++)
                    {
                        var match = matches[i];
                        // Replace occurrence of g.camera.zoomFocus
                        flatScript = flatScript.Replace(match.Value, $"g.camera.zoomFocus({match.Groups[1].Value} * g.{zoomIdentifier}, 100);");
                    }
                    
                    return new PatchResult(flatScript);
                }),
                new Patch("Add checkZoom function", (ctx) =>
                {
                    // Gets the zoom identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier("ZOOM_IDENTIFIER", out var zoomIdentifier))
                        return null;
                    
                    // Gets the check zoom identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier("CHECK_ZOOM_IDENTIFIER", out var checkZoomIdentifier))
                        return null;
                    
                    // Function block to insert
                    var functionToInsert = Util.FlattenString(
                        $"private function {checkZoomIdentifier}(): void {{" +
                        $"    if (input.isKeyDown(74)) {{" +
                        $"        g.{zoomIdentifier} *= 0.98;" +
                        $"        g.camera.zoomFocus(1 * g.{zoomIdentifier}, 4);" +
                        $"    }}" +
                        $"    if (input.isKeyDown(75)) {{" +
                        $"        g.{zoomIdentifier} /= 0.98;" +
                        $"        g.camera.zoomFocus(1 * g.{zoomIdentifier}, 4);" +
                        $"        if (input.isKeyDown(74)) {{" +
                        $"            g.{zoomIdentifier} = 1.0;" +
                        $"            g.camera.zoomFocus(1 * g.{zoomIdentifier}, 4);" +
                        $"        }}" +
                        $"    }}" +
                        $"}}");
                    
                    // Flattens script to remove new lines and carriage returns
                    var flatScript = Util.FlattenString(ctx.ScriptText);

                    // Add checkZoom function to core.states.gameStates.PlayState
                    {
                        // Tries to match PlayState class declaration
                        var match = Regex.Match(flatScript, @"public class PlayState extends GameState{");
                        if (match.Success)
                        {
                            // Sets the position to the end of the match
                            var position = match.Index + match.Length;
                            
                            // Splits the first and the last part of the script text to insert text in between
                            var firstPart = flatScript.Substring(0, position);
                            var lastPart = flatScript.Substring(position);
                            
                            // Inserts text in between firstPart and lastPart
                            flatScript = $"{firstPart}{functionToInsert}{lastPart}";
                        }
                    }
                    
                    return new PatchResult(flatScript);
                }),
                new Patch("Add checkZoom call in updateCommands", (ctx) =>
                {
                    // Gets the check zoom identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier("CHECK_ZOOM_IDENTIFIER", out var checkZoomIdentifier))
                        return null;
                    
                    // Flattens script to remove new lines and carriage returns
                    var flatScript = Util.FlattenString(ctx.ScriptText);
                    
                    // Searches for the function definition
                    var functionDefinitionMatch = Regex.Match(flatScript, @"public\s+function\s+updateCommands\s*\(.*?\)\s*:\s*\w+");
                    if (!functionDefinitionMatch.Success)
                        return null;
                    
                    // Get function scope info
                    var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
                    if (scopeInfo is null)
                        return null;
                    
                    // Get checkAccelerate() index to use as an anchor
                    var checkAccelerateMatch = Regex.Match(scopeInfo.ScopeText, @"checkAccelerate\(\);");
                    if (!checkAccelerateMatch.Success)
                        return null;
                    
                    // Calculate the insertion position on the original text
                    var insertionPosition = checkAccelerateMatch.Index + checkAccelerateMatch.Length + scopeInfo.Index;
                    
                    // Splits the first and the last part of the script text to insert text in between
                    var firstPart = flatScript.Substring(0, insertionPosition);
                    var lastPart = flatScript.Substring(insertionPosition);
                            
                    // Inserts text in between firstPart and lastPart
                    flatScript = $"{firstPart}{checkZoomIdentifier}();{lastPart}";
                    return new PatchResult(flatScript);
                })
            ]),
            new PatchDescriptor("core.text.TextManager", [
                new Patch("Add zoom calculation to createDmgText", (ctx) =>
                {
                    // Gets the zoom identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier("ZOOM_IDENTIFIER", out var zoomIdentifier))
                        return null;
                    
                    // Flattens script to remove new lines and carriage returns
                    var flatScript = Util.FlattenString(ctx.ScriptText);
                    
                    // Local declaration of zoom sqrt to add
                    var localDeclarationOfZoomSqrtName = "zoom";
                    var localDeclarationOfZoomSqrt = $"var {localDeclarationOfZoomSqrtName}:Number = Math.sqrt(g.{zoomIdentifier});";
                    
                    // Searches for the function definition
                    var functionDefinitionMatch = Regex.Match(flatScript, @"public\s+function\s+createDmgText\s*\(.*?\)\s*:\s*\w+");
                    if (!functionDefinitionMatch.Success)
                        return null;
                    
                    // Get function scope info
                    var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
                    if (scopeInfo is null)
                        return null;
                    
                    var scopeText = scopeInfo.ScopeText;
                    
                    // Add the local declaration of zoom sqrt
                    scopeText = $"{{{localDeclarationOfZoomSqrt}{scopeText.Substring(1)}";
                    
                    // Find matches of textHandler.add calls
                    var matches = Regex.Matches(scopeText,
                        @"textHandler\.add\(([^,]+),\s*([^,]+),\s*([^,]+?\([^)]*?\)|[^,]+),\s*([^,]+),\s*([^,]+),\s*([^,]+)\);");
                    if (matches.Count == 0)
                        return null;

                    for (int i = 0; i < matches.Count; i++)
                    {
                        var match = matches[i];
                        scopeText = scopeText.Replace(match.Value, $"textHandler.add({match.Groups[1].Value},{match.Groups[2].Value},{match.Groups[3].Value},{match.Groups[4].Value},{match.Groups[5].Value},{match.Groups[6].Value} / {localDeclarationOfZoomSqrtName});");
                    }
                    
                    // Split original script and re-insert block of function
                    var first = flatScript.Substring(0, scopeInfo.Index);
                    var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
                    flatScript = $"{first}{scopeText}{last}";
                    
                    return new PatchResult(flatScript);
                })
            ]),
            new PatchDescriptor("core.unit.Unit", [
                new Patch("Fix unit render distance for zoom", (ctx) =>
                {
                    // Gets the zoom identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier("ZOOM_IDENTIFIER", out var zoomIdentifier))
                        return null;
                    
                    // Flattens script to remove new lines and carriage returns
                    var flatScript = Util.FlattenString(ctx.ScriptText);
                    
                    // Searches for the function definition
                    var functionDefinitionMatch = Regex.Match(flatScript, @"private\s+function\s+updateIsNear\s*\(.*?\)\s*:\s*\w+");
                    if (!functionDefinitionMatch.Success)
                        return null;
                    
                    // Get function scope info
                    var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
                    if (scopeInfo is null)
                        return null;
                    
                    var scopeText = scopeInfo.ScopeText;
                    
                    // Find 'if(distanceToCamera < _loc(\d*)_)' on the function scope text
                    var match = Regex.Match(scopeText, @"if\(distanceToCamera\s*<\s*_loc(\d*)_\)");
                    if (!match.Success)
                        return null;

                    // Build new condition replacement
                    var newConditionReplacement =
                        $"if(distanceToCamera * g.{zoomIdentifier} < _loc{match.Groups[1].Value}_)";
                    
                    // Replace the old condition with the new one
                    scopeText = scopeText.Replace(match.Value, newConditionReplacement);
                    
                    // Split original script and re-insert block of function
                    var first = flatScript.Substring(0, scopeInfo.Index);
                    var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
                    flatScript = $"{first}{scopeText}{last}";
                    
                    return new PatchResult(flatScript);
                })
            ]),
            new PatchDescriptor("core.boss.Boss", [
                new Patch("Fix boss render distance for zoom", (ctx) =>
                {
                    // Gets the zoom identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier("ZOOM_IDENTIFIER", out var zoomIdentifier))
                        return null;
                    
                    // Flattens script to remove new lines and carriage returns
                    var flatScript = Util.FlattenString(ctx.ScriptText);
                    
                    // Searches for the function definition
                    var functionDefinitionMatch = Regex.Match(flatScript, @"private\s+function\s+updateIsNear\s*\(.*?\)\s*:\s*\w+");
                    if (!functionDefinitionMatch.Success)
                        return null;
                    
                    // Get function scope info
                    var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
                    if (scopeInfo is null)
                        return null;
                    
                    var scopeText = scopeInfo.ScopeText;
                    
                    // Find 'if(distanceToCamera < _loc(\d*)_)' on the function scope text
                    var match = Regex.Match(scopeText, @"if\(distanceToCamera\s*<\s*_loc(\d*)_\)");
                    if (!match.Success)
                        return null;

                    // Build new condition replacement
                    var newConditionReplacement =
                        $"if(distanceToCamera * g.{zoomIdentifier} < _loc{match.Groups[1].Value}_)";
                    
                    // Replace the old condition with the new one
                    scopeText = scopeText.Replace(match.Value, newConditionReplacement);
                    
                    // Split original script and re-insert block of function
                    var first = flatScript.Substring(0, scopeInfo.Index);
                    var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
                    flatScript = $"{first}{scopeText}{last}";
                    
                    return new PatchResult(flatScript);
                })
            ])
        ]);
    }

    private void OnExit()
    {
        Directory.Delete(TemporaryDirectory, true);
    }
}