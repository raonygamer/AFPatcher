﻿using System.Diagnostics;
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
                // Sets the ZOOM_FACTOR name globally
                { "ZOOM_FACTOR", "zoomFactor" },
                // Sets the CHECK_ZOOM_FACTOR name globally
                { "CHECK_ZOOM_FACTOR", "checkZoom" },
                // Sets the FITNESS_LINE identifier name globally
                { "FITNESS_LINE", "fitnessLine" },
                // Sets the CALCULATE_FITNESS_OF_LINE identifier name globally
                { "CALCULATE_FITNESS_OF_LINE", "calculateFitnessOfLine" },
                // Sets FITNESS_VALUE identifier name globally
                { "FITNESS_VALUE", "fitnessValue" },
                // Sets CALCULATE_FITNESS_VALUE identifier name globally
                { "CALCULATE_FITNESS_VALUE", "calculateFitnessValue" },
                // Sets PURIFIED_ARTS identifier name globally
                { "PURIFIED_ARTS", "purifiedArts" },
                // Sets PURIFY_BUTTON identifier name globally
                { "PURIFY_BUTTON", "purifyButton" },
                // Sets SAVE_STATS_BUTTON identifier name globally
                { "SAVE_STATS_BUTTON", "saveStatsButton" },
                // Sets FITNESS_INPUT identifier name globally
                { "FITNESS_INPUT", "fitnessInput" },
                // Sets LINE_INPUT identifier name globally
                { "LINE_INPUT", "lineInput" },
                // Sets STRENGTH_INPUT identifier name globally
                { "STRENGTH_INPUT", "strengthInput" },
                // Sets THIS_STRENGTH identifier name globally
                { "THIS_STRENGTH", "thisStrength" },
                // Sets THIS_FITNESS identifier name globally
                { "THIS_FITNESS", "thisFitness" },
                // Sets THIS_LINES identifier name globally
                { "THIS_LINES", "thisLines" },
                // Sets SAVE_STATS identifier name globally
                { "SAVE_STATS", "saveStats" },
                // Sets PURIFY_ARTS identifier name globally
                { "PURIFY_ARTS", "purifyArts" },
                // Sets ON_PURIFY_RECYCLE identifier name globally
                { "ON_PURIFY_RECYCLE", "onPurifyRecycle" },
                // Sets ON_PURIFY_MESSAGE identifier name globally
                { "ON_PURIFY_MESSAGE", "onPurifyMessage" },
                // Sets SHARED_OBJ identifier name globally
                { "SHARED_OBJ", "sharedObj" },
                // Sets SAVE_SHARED_OBJ identifier name globally
                { "SAVE_SHARED_OBJ", "saveSharedObj" },
                // Sets LOAD_SHARED_OBJ identifier name globally
                { "LOAD_SHARED_OBJ", "loadSharedObj" },
                // Sets SET_PURIFY_STATS identifier name globally
                { "SET_PURIFY_STATS", "setPurifyStats" },
                // Sets OPEN_PORTABLE_RECYCLE identifier name globally
                { "OPEN_PORTABLE_RECYCLE", "openPortableRecycle" },
                // Sets ECHO_VERSION identifier name globally
                { "ECHO_VERSION", "1718" },
            }),
            [
            new PatchDescriptor("core.scene.Game", [
                new Patch("Add ZOOM_FACTOR variable", (ctx) =>
                {
                    // Gets the zoom identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier("ZOOM_FACTOR", out var zoomIdentifier))
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
                }),
                new Patch("Add SHARED_OBJ variable", (ctx) =>
                {
	                // Gets the SHARED_OBJ identifier name
	                if (!ctx.GetGlobalContext().GetIdentifier("SHARED_OBJ", out var sharedObjIdentifier))
		                return null;
                    
	                // The text to insert
	                var insertingText = $@"public var {sharedObjIdentifier}:flash.net.SharedObject;";
                    
	                // Flattens script to remove new lines and carriage returns
	                var flatScript = Util.FlattenString(ctx.ScriptText);
                    
	                // Add SHARED_OBJ variable to core.scene.Game
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
                }),
                new Patch("Add THIS_STRENGTH, THIS_FITNESS, THIS_LINES variables", (ctx) =>
                {
	                // Gets the THIS_STRENGTH identifier name
	                if (!ctx.GetGlobalContext().GetIdentifier("THIS_STRENGTH", out var thisStrengthIdentifier))
		                return null;
	                
	                // Gets the THIS_FITNESS identifier name
	                if (!ctx.GetGlobalContext().GetIdentifier("THIS_FITNESS", out var thisFitnessIdentifier))
		                return null;
	                
	                // Gets the THIS_LINES identifier name
	                if (!ctx.GetGlobalContext().GetIdentifier("THIS_LINES", out var thisLinesIdentifier))
		                return null;
                    
	                // The text to insert
	                var insertingText = Util.FlattenString($@"
						public var {thisFitnessIdentifier}:int = 110;
						public var {thisLinesIdentifier}:int = 0;
						public var {thisStrengthIdentifier}:int = 90;
					");
                    
	                // Flattens script to remove new lines and carriage returns
	                var flatScript = Util.FlattenString(ctx.ScriptText);
                    
	                // Add THIS_STRENGTH, THIS_FITNESS, THIS_LINES variables to core.scene.Game
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
                }),
                new Patch("Add SAVE_SHARED_OBJ, LOAD_SHARED_OBJ, SAVE_PURIFY_STATS functions", (ctx) =>
                {
	                // Gets the SHARED_OBJ identifier name
	                if (!ctx.GetGlobalContext().GetIdentifier("SHARED_OBJ", out var sharedObjIdentifier))
		                return null;
	                
                    // Gets the SAVE_SHARED_OBJ identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier("SAVE_SHARED_OBJ", out var saveSharedObjIdentifier))
                        return null;
                    
                    // Gets the LOAD_SHARED_OBJ identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier("LOAD_SHARED_OBJ", out var loadSharedObjIdentifier))
	                    return null;
                    
                    // Gets the SET_PURIFY_STATS identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier("SET_PURIFY_STATS", out var setPurifyStatsIdentifier))
                        return null;
                    
                    // Gets the THIS_STRENGTH identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier("THIS_STRENGTH", out var thisStrengthIdentifier))
	                    return null;
	                
                    // Gets the THIS_FITNESS identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier("THIS_FITNESS", out var thisFitnessIdentifier))
	                    return null;
	                
                    // Gets the THIS_LINES identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier("THIS_LINES", out var thisLinesIdentifier))
	                    return null;
                    
                    // SAVE_SHARED_OBJ block to insert
                    var saveSharedObjFunction = Util.FlattenString($@"
						public function {saveSharedObjIdentifier}(): void {{
							{sharedObjIdentifier}.data.{thisFitnessIdentifier} = {thisFitnessIdentifier};
							{sharedObjIdentifier}.data.{thisLinesIdentifier} = {thisLinesIdentifier};
							{sharedObjIdentifier}.data.{thisStrengthIdentifier} = {thisStrengthIdentifier};
							{sharedObjIdentifier}.flush();
						}}
					");
                    
                    // LOAD_SHARED_OBJ block to insert
                    var loadSharedObjFunction = Util.FlattenString($@"
						private function {loadSharedObjIdentifier}(): void {{
							this.{thisFitnessIdentifier} = {sharedObjIdentifier}.data.{thisFitnessIdentifier} == null ? 110 : {sharedObjIdentifier}.data.{thisFitnessIdentifier};
							this.{thisLinesIdentifier} = {sharedObjIdentifier}.data.{thisLinesIdentifier} == null ? 0 : {sharedObjIdentifier}.data.{thisLinesIdentifier};
							this.{thisStrengthIdentifier} = {sharedObjIdentifier}.data.{thisStrengthIdentifier} == null ? 90 : {sharedObjIdentifier}.data.{thisStrengthIdentifier};
						}}
					");
                    
                    // SET_PURIFY_STATS block to insert
                    var setPurifyStatsFunction = Util.FlattenString($@"
						public function {setPurifyStatsIdentifier}(data:Array): void {{
							this.{thisFitnessIdentifier} = data[0];
							this.{thisStrengthIdentifier} = data[1];
							this.{thisLinesIdentifier} = data[2];
							{saveSharedObjIdentifier}();
						}}
					");

                    var functionsToInsert = $"{saveSharedObjFunction}{loadSharedObjFunction}{setPurifyStatsFunction}";
                    
                    // Flattens script to remove new lines and carriage returns
                    var flatScript = Util.FlattenString(ctx.ScriptText);

                    // Add SAVE_SHARED_OBJ, LOAD_SHARED_OBJ, SAVE_PURIFY_STATS function to core.scene.Game
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
                            flatScript = $"{firstPart}{functionsToInsert}{lastPart}";
                        }
                    }
                    
                    return new PatchResult(flatScript);
                }),
                new Patch("Initialize SHARED_OBJ on constructor", (ctx) =>
                {
	                // Gets the SHARED_OBJ identifier name
	                if (!ctx.GetGlobalContext().GetIdentifier("SHARED_OBJ", out var sharedObjIdentifier))
		                return null;
	                
	                // Flattens script to remove new lines and carriage returns
	                var flatScript = Util.FlattenString(ctx.ScriptText);
                    
	                // Searches for the function definition
	                var functionDefinitionMatch = Regex.Match(flatScript, @"public\s+function\s+Game\s*\(.*?\)\s*");
	                if (!functionDefinitionMatch.Success)
		                return null;
                    
	                // Get function scope info
	                var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
	                if (scopeInfo is null)
		                return null;
                    
	                var scopeText = scopeInfo.ScopeText;
	                var textToInsert = $@"this.{sharedObjIdentifier} = flash.net.SharedObject.getLocal(""customStorage"");";

	                {
		                // Insert text
		                var first = scopeText.Substring(0, scopeText.Length - 1);
		                var last = scopeText.Substring(scopeInfo.Length - 1);
		                scopeText = $"{first}{textToInsert}{last}";
	                }

	                {
		                // Split original script and re-insert block of function
		                var first = flatScript.Substring(0, scopeInfo.Index);
		                var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
		                flatScript = $"{first}{scopeText}{last}";
	                }
	                
	                return new PatchResult(flatScript);
                }),
                new Patch("Add LOAD_SHARED_OBJ on init function", (ctx) =>
                {
	                // Gets the LOAD_SHARED_OBJ identifier name
	                if (!ctx.GetGlobalContext().GetIdentifier("LOAD_SHARED_OBJ", out var loadSharedObjIdentifier))
		                return null;
	                
	                // Flattens script to remove new lines and carriage returns
	                var flatScript = Util.FlattenString(ctx.ScriptText);
                    
	                // Searches for the function definition
	                var functionDefinitionMatch = Regex.Match(flatScript, @"override\s+protected\s+function\s+init\s*\(.*?\)\s*:\s*\w+");
	                if (!functionDefinitionMatch.Success)
		                return null;
                    
	                // Get function scope info
	                var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
	                if (scopeInfo is null)
		                return null;
                    
	                var scopeText = scopeInfo.ScopeText;
	                
	                var textToInsert = $@"{loadSharedObjIdentifier}();";

	                {
		                // Insert text
		                var first = scopeText.Substring(0, scopeText.Length - 1);
		                var last = scopeText.Substring(scopeInfo.Length - 1);
		                scopeText = $"{first}{textToInsert}{last}";
	                }

	                {
		                // Split original script and re-insert block of function
		                var first = flatScript.Substring(0, scopeInfo.Index);
		                var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
		                flatScript = $"{first}{scopeText}{last}";
	                }
	                
	                return new PatchResult(flatScript);
                }),
                new Patch("Add OPEN_PORTABLE_RECYCLE function", (ctx) =>
                {
	                // Gets the OPEN_PORTABLE_RECYCLE identifier name
	                if (!ctx.GetGlobalContext().GetIdentifier("OPEN_PORTABLE_RECYCLE", out var openPortableRecycleIdentifier))
		                return null;
                    
	                // OPEN_PORTABLE_RECYCLE block to insert
	                var openPortableRecycleFunction = Util.FlattenString($@"
						public function {openPortableRecycleIdentifier}(): void {{
							var root:Body = bodyManager.getRoot();
							root.name = ""Portable recycle"";
							fadeIntoState(new LandedRecycle(this,root));
						}}
					");
	                
	                // Flattens script to remove new lines and carriage returns
	                var flatScript = Util.FlattenString(ctx.ScriptText);

	                // Add OPEN_PORTABLE_RECYCLE function to core.scene.Game
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
			                flatScript = $"{firstPart}{openPortableRecycleFunction}{lastPart}";
		                }
	                }
	                
	                return new PatchResult(flatScript);
                })
            ]),
            new PatchDescriptor("core.states.gameStates.PlayState", [
                new Patch("Replace all camera zoomFocus calls for zoom", (ctx) =>
                {
                    // Gets the zoom identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier("ZOOM_FACTOR", out var zoomIdentifier))
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
                new Patch("Add CHECK_ZOOM_FACTOR function", (ctx) =>
                {
                    // Gets the zoom identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier("ZOOM_FACTOR", out var zoomIdentifier))
                        return null;
                    
                    // Gets the check zoom identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier("CHECK_ZOOM_FACTOR", out var checkZoomIdentifier))
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
                new Patch("Add CHECK_ZOOM_FACTOR call in updateCommands", (ctx) =>
                {
                    // Gets the check zoom identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier("CHECK_ZOOM_FACTOR", out var checkZoomIdentifier))
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
                    if (!ctx.GetGlobalContext().GetIdentifier("ZOOM_FACTOR", out var zoomIdentifier))
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
                    if (!ctx.GetGlobalContext().GetIdentifier("ZOOM_FACTOR", out var zoomIdentifier))
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
                    if (!ctx.GetGlobalContext().GetIdentifier("ZOOM_FACTOR", out var zoomIdentifier))
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
            new PatchDescriptor("core.artifact.ArtifactStat", [
                new Patch("Add FITNESS_LINE variable", (ctx) =>
                {
                    // Gets the fitness line identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier("FITNESS_LINE", out var fitnessLineIdentifier))
                        return null;
                    
                    // The text to insert
                    var insertingText = $@"public var {fitnessLineIdentifier}:Number = 0;";
                    
                    // Flattens script to remove new lines and carriage returns
                    var flatScript = Util.FlattenString(ctx.ScriptText);
                    
                    // Add FITNESS_LINE variable to core.artifact.ArtifactStat
                    {
                        // Tries to match ArtifactStat class declaration
                        var match = Regex.Match(flatScript, @"public class ArtifactStat{");
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
                }),
                new Patch("Add CALCULATE_FITNESS_OF_LINE function", (ctx) =>
                {
                    // Gets the CALCULATE_FITNESS_OF_LINE identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier("CALCULATE_FITNESS_OF_LINE", out var calculateFitnessOfLineIdentifier))
                        return null;
                    
                    // The text to insert
                    var functionToInsert = Util.FlattenString(
	                $@"public function {calculateFitnessOfLineIdentifier}():Number {{
						var lineValue:Number = this.value;
						switch(this.type) {{
							case ""healthAdd"":
							case ""healthAdd2"":
							case ""healthAdd3"":
								return 0.002264 * lineValue;
							case ""healthMulti"":
								return 0.5832 * lineValue;
							case ""armorAdd"":
							case ""armorAdd2"":
							case ""armorAdd3"":
								return 0.15375 * lineValue;
							case ""armorMulti"":
								return 0.333 * lineValue;
							case ""corrosiveAdd"":
							case ""corrosiveAdd2"":
							case ""corrosiveAdd3"":
								return 0.3388 * lineValue;
							case ""corrosiveMulti"":
								return 0.83325 * lineValue;
							case ""energyAdd"":
							case ""energyAdd2"":
							case ""energyAdd3"":
								return 0.324 * lineValue;
							case ""energyMulti"":
								return 0.83325 * lineValue;
							case ""kineticAdd"":
							case ""kineticAdd2"":
							case ""kineticAdd3"":
								return 0.3132 * lineValue;
							case ""kineticMulti"":
								return 0.79992 * lineValue;
							case ""shieldAdd"":
							case ""shieldAdd2"":
							case ""shieldAdd3"":
								return 0.001925 * lineValue;
							case ""shieldMulti"":
								return 0.54675 * lineValue;
							case ""shieldRegen"":
								return 0.48 * lineValue;
							case ""corrosiveResist"":
							case ""energyResist"":
							case ""kineticResist"":
								return 0.425 * lineValue;
							case ""allResist"":
								return 1.1 * lineValue;
							case ""allAdd"":
							case ""allAdd2"":
							case ""allAdd3"":
								return 0.23904 * lineValue;
							case ""allMulti"":
								return 2.36925 * lineValue;
							case ""dotDamage"":
								return 5 * lineValue;
							case ""dotDuration"":
								return 2.5 * lineValue;
							case ""directDamage"":
								return 0.5 * lineValue;
							case ""speed"":
							case ""speed2"":
							case ""speed3"":
								return 0.88 * lineValue;
							case ""refire"":
							case ""refire2"":
							case ""refire3"":
								return 0.529584 * lineValue;
							case ""convHp"":
							case ""convShield"":
								if(1000 <= lineValue) {{
									return 43;
								}}
								return 0.0425 * lineValue;
								break;
							case ""powerReg"":
							case ""powerReg2"":
							case ""powerReg3"":
								return 0.24 * lineValue;
							case ""powerMax"":
								return 0.3333 * lineValue;
							case ""cooldown"":
							case ""cooldown2"":
							case ""cooldown3"":
								return 0.6666 * lineValue;
							case ""increaseRecyleRate"":
								return 1.1 * lineValue;
							case ""damageReduction"":
								return 1.5 * lineValue;
							case ""damageReductionWithLowHealth"":
							case ""damageReductionWithLowShield"":
								return 1.4 * lineValue;
							case ""healthRegenAdd"":
								return 0.1 * lineValue;
							case ""shieldVamp"":
							case ""healthVamp"":
								return lineValue;
							case ""kineticChanceToPenetrateShield"":
								return 0.1 * lineValue * 0.5;
							case ""energyChanceToShieldOverload"":
							case ""corrosiveChanceToIgnite"":
								return 0.5 * lineValue;
							case ""beamAndMissileDoesBonusDamage"":
								return 0.5 * lineValue;
							case ""recycleCatalyst"":
								return lineValue;
							case ""velocityCore"":
								return 50;
							case ""slowDown"":
								return 80;
							case ""damageReductionUnique"":
								return 50;
							case ""damageReductionWithLowHealthUnique"":
							case ""damageReductionWithLowShieldUnique"":
								return 35;
							case ""overmind"":
								return 30;
							case ""upgrade"":
								return 20;
							case ""lucaniteCore"":
								return 1.1 * lineValue;
							case ""mantisCore"":
								return 0.8 * lineValue;
							case ""thermofangCore"":
								return lineValue;
							case ""reduceKineticResistance"":
							case ""reduceCorrosiveResistance"":
							case ""reduceEnergyResistance"":
								return lineValue;
							case ""crownOfXhersix"":
								return 10 * lineValue;
							case ""veilOfYhgvis"":
								return 50;
							case ""fistOfZharix"":
								return 10 * lineValue;
							case ""bloodlineSurge"":
								return 50;
							case ""dotDamageUnique"":
								return 1.1 * lineValue;
							case ""directDamageUnique"":
								return 0.5 * lineValue;
							case ""reflectDamageUnique"":
								return 40;
							default:
								return 0;
						}}
					}}");
                    
                    // Flattens script to remove new lines and carriage returns
                    var flatScript = Util.FlattenString(ctx.ScriptText);
                    
                    // Add CALCULATE_FITNESS_OF_LINE function to core.artifact.ArtifactStat
                    {
                        // Tries to match ArtifactStat class declaration
                        var match = Regex.Match(flatScript, @"public class ArtifactStat{");
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
                new Patch("Initialize FITNESS_LINE with CALCULATE_FITNESS_OF_LINE function on constructor", (ctx) =>
                {
	                // Gets the FITNESS_LINE identifier name
	                if (!ctx.GetGlobalContext().GetIdentifier("FITNESS_LINE", out var fitnessLineIdentifier))
		                return null;
	                
	                // Gets the CALCULATE_FITNESS_OF_LINE identifier name
	                if (!ctx.GetGlobalContext().GetIdentifier("CALCULATE_FITNESS_OF_LINE", out var calculateFitnessOfLineIdentifier))
		                return null;
                    
	                // Flattens script to remove new lines and carriage returns
	                var flatScript = Util.FlattenString(ctx.ScriptText);
                    
	                // Searches for the function definition
	                var functionDefinitionMatch = Regex.Match(flatScript, @"public\s+function\s+ArtifactStat\s*\(.*?\)\s*");
	                if (!functionDefinitionMatch.Success)
		                return null;
                    
	                // Get function scope info
	                var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
	                if (scopeInfo is null)
		                return null;
                    
	                var scopeText = scopeInfo.ScopeText;
	                var textToInsert = $"this.{fitnessLineIdentifier} = {calculateFitnessOfLineIdentifier}();";

	                {
		                // Insert text
		                var first = scopeText.Substring(0, scopeText.Length - 1);
		                var last = scopeText.Substring(scopeInfo.Length - 1);
		                scopeText = $"{first}{textToInsert}{last}";
	                }

	                {
		                // Split original script and re-insert block of function
		                var first = flatScript.Substring(0, scopeInfo.Index);
		                var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
		                flatScript = $"{first}{scopeText}{last}";
	                }
	                
	                return new PatchResult(flatScript);
                })
            ]),
            new PatchDescriptor("core.artifact.Artifact", [
				new Patch("Add FITNESS_VALUE variable", (ctx) =>
				{
					// Gets the fitness value identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("FITNESS_VALUE", out var fitnessValueIdentifier))
						return null;
                    
					// The text to insert
					var insertingText = $@"public var {fitnessValueIdentifier}:Number = 0;";
                    
					// Flattens script to remove new lines and carriage returns
					var flatScript = Util.FlattenString(ctx.ScriptText);
                    
					// Add FITNESS_VALUE variable to core.artifact.Artifact
					{
						// Tries to match Artifact class declaration
						var match = Regex.Match(flatScript, @"public class Artifact{");
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
				}),
				new Patch("Change orderStatCountAsc ordering mode to unique artifacts", (ctx) =>
				{
					// Flattens script to remove new lines and carriage returns
					var flatScript = Util.FlattenString(ctx.ScriptText);

					// Searches for the function definition
					var functionDefinitionMatch = Regex.Match(flatScript, @"public\s+static\s+function\s+orderStatCountAsc\s*\(.*?\)\s*:\s*\w+");
					if (!functionDefinitionMatch.Success)
						return null;
                    
					// Get function scope info
					var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
					if (scopeInfo is null)
						return null;
                    
					var scopeText = scopeInfo.ScopeText;

					// Remove var _loc(\d*)_:Number = param(\d*).stats.length;
					scopeText = Regex.Replace(scopeText, @"var\s+_loc(\d*)_:Number\s+=\s+param(\d*)\.stats\.length;", "");
					
					// Try to patch if(_loc(\d*)_ > _loc(\d*)_)"
					{
						var match = Regex.Match(scopeText, @"if\(_loc(\d*)_\s+>\s+_loc(\d*)_\)");
						if (match.Success)
						{
							var textToReplace = @"if(!param1.isUnique && param2.isUnique)";
							scopeText = scopeText.Replace(match.Value, textToReplace);
						}
					}
					
					// Try to patch if(_loc(\d*)_ < _loc(\d*)_)"
					{
						var match = Regex.Match(scopeText, @"if\(_loc(\d*)_\s+<\s+_loc(\d*)_\)");
						if (match.Success)
						{
							var textToReplace = @"if(param1.isUnique && !param2.isUnique)";
							scopeText = scopeText.Replace(match.Value, textToReplace);
						}
					}
					
					{
						// Split original script and re-insert block of function
						var first = flatScript.Substring(0, scopeInfo.Index);
						var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
						flatScript = $"{first}{scopeText}{last}";
					}
					
					return new PatchResult(flatScript);
				}),
				new Patch("Change orderStatCountDesc ordering mode to upgraded artifacts", (ctx) =>
				{
					// Flattens script to remove new lines and carriage returns
					var flatScript = Util.FlattenString(ctx.ScriptText);

					// Searches for the function definition
					var functionDefinitionMatch = Regex.Match(flatScript, @"public\s+static\s+function\s+orderStatCountDesc\s*\(.*?\)\s*:\s*\w+");
					if (!functionDefinitionMatch.Success)
						return null;
                    
					// Get function scope info
					var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
					if (scopeInfo is null)
						return null;
                    
					var scopeText = scopeInfo.ScopeText;

					// Replace var _loc(\d*)_:Number = param(\d*).stats.length; with var _loc$1_:Number = param$2.upgraded;
					scopeText = Regex.Replace(scopeText, @"var\s+_loc(\d*)_:Number\s+=\s+param(\d*)\.stats\.length;", "var _loc$1_:Number = param$2.upgraded;");
					
					{
						// Split original script and re-insert block of function
						var first = flatScript.Substring(0, scopeInfo.Index);
						var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
						flatScript = $"{first}{scopeText}{last}";
					}
					
					return new PatchResult(flatScript);
				}),
				new Patch("Change orderLevelLow ordering mode to fitness", (ctx) =>
				{
					// Gets the fitness value identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("FITNESS_VALUE", out var fitnessValueIdentifier))
						return null;
					
					// Flattens script to remove new lines and carriage returns
					var flatScript = Util.FlattenString(ctx.ScriptText);

					// Searches for the function definition
					var functionDefinitionMatch = Regex.Match(flatScript, @"public\s+static\s+function\s+orderLevelLow\s*\(.*?\)\s*:\s*\w+");
					if (!functionDefinitionMatch.Success)
						return null;
                    
					// Get function scope info
					var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
					if (scopeInfo is null)
						return null;
                    
					var scopeText = scopeInfo.ScopeText;

					// Replace var _loc(\d*)_:Number = param(\d*).level; with var _loc$1_:Number = param$2.FITNESS_VALUE;
					scopeText = Regex.Replace(scopeText, @"var\s+_loc(\d*)_:Number\s+=\s+param(\d*)\.level;", $"var _loc$1_:Number = param$2.{fitnessValueIdentifier};");
					
					// Try to replace if(_loc(\d*)_ > _loc(\d*)_) with if(_loc(\d*)_ >_ _loc(\d*)_)
					// This >_ is not a mistake, its just so we can filter it to patch later to not generate conflicts
					scopeText = Regex.Replace(scopeText, @"if\(_loc(\d*)_\s+>\s+_loc(\d*)_\)", @"if(_loc$1_ >_ _loc$2_)");
					
					// Try to replace if(_loc(\d*)_ < _loc(\d*)_) with if(_loc(\d*)_ > _loc(\d*)_)
					scopeText = Regex.Replace(scopeText, @"if\(_loc(\d*)_\s+<\s+_loc(\d*)_\)", @"if(_loc$1_ > _loc$2_)");
					
					// Try to replace if(_loc(\d*)_ >_ _loc(\d*)_) with if(_loc(\d*)_ < _loc(\d*)_)
					// Now we really replace it
					scopeText = Regex.Replace(scopeText, @"if\(_loc(\d*)_\s+>_\s+_loc(\d*)_\)", @"if(_loc$1_ < _loc$2_)");

					{
						// Split original script and re-insert block of function
						var first = flatScript.Substring(0, scopeInfo.Index);
						var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
						flatScript = $"{first}{scopeText}{last}";
					}
					
					return new PatchResult(flatScript);
				}),
				new Patch("Add CALCULATE_FITNESS_VALUE function", (ctx) =>
				{
					// Gets the CALCULATE_FITNESS_VALUE identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("CALCULATE_FITNESS_VALUE", out var calculateFitnessValueIdentifier))
						return null;
					
					// Gets the FITNESS_LINE identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("FITNESS_LINE", out var fitnessLineIdentifier))
						return null;
					
					// Flattens script to remove new lines and carriage returns
					var flatScript = Util.FlattenString(ctx.ScriptText);

					var functionToInsert = Util.FlattenString($@"
						public function {calculateFitnessValueIdentifier}() : Number {{
							var fitness:Number = 0;
							var i:int = 0;
							while(i < stats.length)
							{{
								fitness += stats[i].{fitnessLineIdentifier};
								i++;
							}}
							return fitness;
						}}
					");
					
					// Add CALCULATE_FITNESS_VALUE function to core.artifact.Artifact
					{
						// Tries to match Artifact class declaration
						var match = Regex.Match(flatScript, @"public class Artifact{");
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
				new Patch("Add FITNESS_VALUE calculation to the update function", (ctx) =>
				{
					// Gets the FITNESS_VALUE identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("FITNESS_VALUE", out var fitnessValueIdentifier))
						return null;
					
					// Gets the CALCULATE_FITNESS_VALUE identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("CALCULATE_FITNESS_VALUE", out var calculateFitnessValueIdentifier))
						return null;
					
					// Flattens script to remove new lines and carriage returns
					var flatScript = Util.FlattenString(ctx.ScriptText);

					// Searches for the function definition
					var functionDefinitionMatch = Regex.Match(flatScript, @"public\s+function\s+update\s*\(.*?\)\s*:\s*\w+");
					if (!functionDefinitionMatch.Success)
						return null;
                    
					// Get function scope info
					var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
					if (scopeInfo is null)
						return null;
                    
					var scopeText = scopeInfo.ScopeText;
					var textToInsert = $"this.{fitnessValueIdentifier} = {calculateFitnessValueIdentifier}();";

					{
						// Insert text
						var first = scopeText.Substring(0, scopeText.Length - 1);
						var last = scopeText.Substring(scopeInfo.Length - 1);
						scopeText = $"{first}{textToInsert}{last}";
					}

					{
						// Split original script and re-insert block of function
						var first = flatScript.Substring(0, scopeInfo.Index);
						var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
						flatScript = $"{first}{scopeText}{last}";
					}
					
					return new PatchResult(flatScript);
				})
            ]),
            new PatchDescriptor("core.artifact.ArtifactOverview", [
				new Patch("Add PURIFIED_ARTS, PURIFY_BUTTON, SAVE_STATS_BUTTON, FITNESS_INPUT, LINE_INPUT, STRENGTH_INPUT variables", (ctx) =>
				{
					// Gets the PURIFIED_ARTS identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("PURIFIED_ARTS", out var purifiedArtsIdentifier))
						return null;
					
					// Gets the PURIFY_BUTTON identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("PURIFY_BUTTON", out var purifyButtonIdentifier))
						return null;
					
					// Gets the SAVE_STATS_BUTTON identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("SAVE_STATS_BUTTON", out var saveStatsButtonIdentifier))
						return null;
					
					// Gets the FITNESS_INPUT identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("FITNESS_INPUT", out var fitnessInputIdentifier))
						return null;
					
					// Gets the LINE_INPUT identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("LINE_INPUT", out var lineInputIdentifier))
						return null;
					
					// Gets the STRENGTH_INPUT identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("STRENGTH_INPUT", out var strengthInputIdentifier))
						return null;
                    
					// The text to insert
					var insertingText = Util.FlattenString($@"
						private var {purifiedArtsIdentifier}:Vector.<Artifact> = new Vector.<Artifact>();
						private var {purifyButtonIdentifier}:Button;
						private var {saveStatsButtonIdentifier}:Button;
						private var {fitnessInputIdentifier}:InputText;
						private var {lineInputIdentifier}:InputText;
						private var {strengthInputIdentifier}:InputText;
					");
                    
					// Flattens script to remove new lines and carriage returns
					var flatScript = Util.FlattenString(ctx.ScriptText);
                    
					// Add PURIFIED_ARTS, PURIFY_BUTTON, SAVE_STATS_BUTTON, FITNESS_INPUT, LINE_INPUT, STRENGTH_INPUT variables to core.artifact.ArtifactOverview
					{
						// Tries to match ArtifactOverview class declaration
						var match = Regex.Match(flatScript, @"public class ArtifactOverview extends Sprite{");
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
				}),
				new Patch("Add purification components to drawComponents", (ctx) =>
				{
					// Gets the PURIFIED_ARTS identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("PURIFIED_ARTS", out var purifiedArtsIdentifier))
						return null;
					
					// Gets the PURIFY_BUTTON identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("PURIFY_BUTTON", out var purifyButtonIdentifier))
						return null;
					
					// Gets the SAVE_STATS_BUTTON identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("SAVE_STATS_BUTTON", out var saveStatsButtonIdentifier))
						return null;
					
					// Gets the FITNESS_INPUT identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("FITNESS_INPUT", out var fitnessInputIdentifier))
						return null;
					
					// Gets the LINE_INPUT identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("LINE_INPUT", out var lineInputIdentifier))
						return null;
					
					// Gets the STRENGTH_INPUT identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("STRENGTH_INPUT", out var strengthInputIdentifier))
						return null;
					
					// Gets the THIS_STRENGTH identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("THIS_STRENGTH", out var thisStrengthIdentifier))
						return null;
	                
					// Gets the THIS_FITNESS identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("THIS_FITNESS", out var thisFitnessIdentifier))
						return null;
	                
					// Gets the THIS_LINES identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("THIS_LINES", out var thisLinesIdentifier))
						return null;
					
					// Gets the SAVE_STATS identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("SAVE_STATS", out var saveStatsIdentifier))
						return null;
                    
					// Gets the PURIFY_ARTS identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("PURIFY_ARTS", out var purifyArtsIdentifier))
						return null;
					
					// Flattens script to remove new lines and carriage returns
					var flatScript = Util.FlattenString(ctx.ScriptText);
					
					// Searches for the function definition
					var functionDefinitionMatch = Regex.Match(flatScript, @"public\s+function\s+drawComponents\s*\(.*?\)\s*:\s*\w+");
					if (!functionDefinitionMatch.Success)
						return null;
                    
					// Get function scope info
					var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
					if (scopeInfo is null)
						return null;
                    
					var scopeText = scopeInfo.ScopeText;

					var textToInsert = Util.FlattenString($@"
						{strengthInputIdentifier} = new InputText(458,8 * 60,40,25);
						{strengthInputIdentifier}.restrict = ""0-9"";
						{strengthInputIdentifier}.maxChars = 3;
						{strengthInputIdentifier}.text = g.{thisStrengthIdentifier};
						{strengthInputIdentifier}.visible = true;
						{strengthInputIdentifier}.isEnabled = true;
						addChild({strengthInputIdentifier});
						{fitnessInputIdentifier} = new InputText(458,453,40,25);
						{fitnessInputIdentifier}.restrict = ""0-9"";
						{fitnessInputIdentifier}.maxChars = 3;
						{fitnessInputIdentifier}.text = g.{thisFitnessIdentifier};
						{fitnessInputIdentifier}.visible = true;
						{fitnessInputIdentifier}.isEnabled = true;
						addChild({fitnessInputIdentifier});
						{lineInputIdentifier} = new InputText(458,426,40,25);
						{lineInputIdentifier}.restrict = ""0-9"";
						{lineInputIdentifier}.maxChars = 3;
						{lineInputIdentifier}.text = g.{thisLinesIdentifier};
						{lineInputIdentifier}.visible = true;
						{lineInputIdentifier}.isEnabled = true;
						addChild({lineInputIdentifier});
						addChild(new TextBitmap(500,430,Localize.t(""Lines""),12));
						addChild(new TextBitmap(500,457,Localize.t(""Fitness""),12));
						addChild(new TextBitmap(500,484,Localize.t(""Level""),12));
						{purifyButtonIdentifier} = new Button({purifyArtsIdentifier},""Purify!"",""positive"");
						{purifyButtonIdentifier}.x = 380;
						{purifyButtonIdentifier}.y = 8 * 60;
						{saveStatsButtonIdentifier} = new Button({saveStatsIdentifier},""Save!"",""positive"");
						{saveStatsButtonIdentifier}.x = {purifyButtonIdentifier}.x;
						{saveStatsButtonIdentifier}.y = {purifyButtonIdentifier}.y - {purifyButtonIdentifier}.height - 10;
						addChild({saveStatsButtonIdentifier});
						addChild({purifyButtonIdentifier});
					");
					
					{
						// Insert text
						var first = scopeText.Substring(0, scopeText.Length - 1);
						var last = scopeText.Substring(scopeInfo.Length - 1);
						scopeText = $"{first}{textToInsert}{last}";
					}
					
					{
						// Split original script and re-insert block of function
						var first = flatScript.Substring(0, scopeInfo.Index);
						var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
						flatScript = $"{first}{scopeText}{last}";
					}

					return new PatchResult(flatScript);
				}),
				new Patch("Add SAVE_STATS, PURIFY_ARTS, ON_PURIFY_RECYCLE, ON_PURIFY_MESSAGE functions", (ctx) =>
				{
					// Gets the PURIFIED_ARTS identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("PURIFIED_ARTS", out var purifiedArtsIdentifier))
						return null;
					
					// Gets the SAVE_STATS identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("SAVE_STATS", out var saveStatsIdentifier))
						return null;
                    
					// Gets the PURIFY_ARTS identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("PURIFY_ARTS", out var purifyArtsIdentifier))
						return null;
					
					// Gets the ON_PURIFY_RECYCLE identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("ON_PURIFY_RECYCLE", out var onPurifyRecycleIdentifier))
						return null;
					
					// Gets the ON_PURIFY_MESSAGE identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("ON_PURIFY_MESSAGE", out var onPurifyMessageIdentifier))
						return null;
					
					// Gets the PURIFY_BUTTON identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("PURIFY_BUTTON", out var purifyButtonIdentifier))
						return null;
					
					// Gets the SAVE_STATS_BUTTON identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("SAVE_STATS_BUTTON", out var saveStatsButtonIdentifier))
						return null;
					
					// Gets the FITNESS_INPUT identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("FITNESS_INPUT", out var fitnessInputIdentifier))
						return null;
					
					// Gets the LINE_INPUT identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("LINE_INPUT", out var lineInputIdentifier))
						return null;
					
					// Gets the STRENGTH_INPUT identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("STRENGTH_INPUT", out var strengthInputIdentifier))
						return null;
					
					// Gets the THIS_STRENGTH identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("THIS_STRENGTH", out var thisStrengthIdentifier))
						return null;
	                
					// Gets the THIS_FITNESS identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("THIS_FITNESS", out var thisFitnessIdentifier))
						return null;
	                
					// Gets the THIS_LINES identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("THIS_LINES", out var thisLinesIdentifier))
						return null;
					
					// Gets the fitness value identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("FITNESS_VALUE", out var fitnessValueIdentifier))
						return null;
					
					// Gets the SET_PURIFY_STATS identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("SET_PURIFY_STATS", out var setPurifyStatsIdentifier))
						return null;
                    
					// SAVE_STATS block to insert
					var saveStatsFunction = Util.FlattenString($@"
						private function {saveStatsIdentifier}(e:TouchEvent = null): void {{
							var fitness:int = int({fitnessInputIdentifier}.text);
							var strength:int = int({strengthInputIdentifier}.text);
							var lines:int = int({lineInputIdentifier}.text);
							g.{setPurifyStatsIdentifier}([fitness,strength,lines]);
							{saveStatsButtonIdentifier}.enabled = true;
						}}
					");
					
					// PURIFY_ARTS block to insert
					var purifyArtsFunction = Util.FlattenString($@"
						private function {purifyArtsIdentifier}(e:TouchEvent = null): void {{
							{purifiedArtsIdentifier}.splice(0,{purifiedArtsIdentifier}.length);
							var count:int = 0;
							for each(var cargoBox in cargoBoxes)
							{{
								if(count == 40)
								{{
									break;
								}}
								if(cargoBox.a != null)
								{{
									if(!cargoBox.a.revealed)
									{{
										if(cargoBox.a.stats.length < g.{thisLinesIdentifier} || cargoBox.a.{fitnessValueIdentifier} < g.{thisFitnessIdentifier} || cargoBox.a.level < g.{thisStrengthIdentifier})
										{{
											if(cargoBox.a.name != ""Recycle Generator"")
											{{
												cargoBox.setSelectedForRecycle();
												{purifiedArtsIdentifier}.push(cargoBox.a);
												count++;
											}}
										}}
									}}
								}}
							}}
							{onPurifyRecycleIdentifier}();
						}}
					");
					
					// ON_PURIFY_RECYCLE block to insert
					var onPurifyRecycleFunction = Util.FlattenString($@"
						private function {onPurifyRecycleIdentifier}(): void {{
							if({purifiedArtsIdentifier}.length == 0)
							{{
								g.showErrorDialog(""No artifacts to recycle."");
								{purifyButtonIdentifier}.enabled = true;
								return;
							}}
							if(g.myCargo.isFull)
							{{
								g.showErrorDialog(Localize.t(""Your cargo compressor is overloaded!""));
								{purifyButtonIdentifier}.enabled = true;
								return;
							}}
							var recycleMessage:Message = g.createMessage(""bulkRecycle"");
							for each(var art in purifiedArts)
							{{
								recycleMessage.add(art.id);
							}}
							g.rpcMessage(recycleMessage,{onPurifyMessageIdentifier});
						}}
					");
					
					// ON_PURIFY_MESSAGE block to insert
					var onPurifyMessageFunction = Util.FlattenString($@"
						private function {onPurifyMessageIdentifier}(message:Message): void {{
							var i:int = 0;
							var success:Boolean = message.getBoolean(0);
							if(!success)
							{{
								g.showErrorDialog(message.getString(1));
								{purifyButtonIdentifier}.enabled = true;
								return;
							}}
							while(i < {purifiedArtsIdentifier}.length)
							{{
								var art:Artifact = {purifiedArtsIdentifier}[i];
								p.artifactCount -= 1;
								for each(var cargoBox in cargoBoxes)
								{{
									if(cargoBox.a == art)
									{{
										cargoBox.setEmpty();
										break;
									}}
								}}
								j = 0;
								while(j < p.artifacts.length)
								{{
									if(art == p.artifacts[j])
									{{
										p.artifacts.splice(j,1);
										break;
									}}
									j++;
								}}
								i++;
							}}
							if(p.artifactCount < p.artifactLimit)
							{{
								g.hud.hideArtifactLimitText();
							}}
							i = 1;
							while(i < message.length)
							{{
								var key:String = message.getString(i);
								var count:int = message.getInt(i + 1);
								g.myCargo.addItem(""Commodities"",key,count);
								i += 2;
							}}
							{purifyButtonIdentifier}.enabled = true;
						}}
					");

					var allFunctions = $"{saveStatsFunction}{purifyArtsFunction}{onPurifyRecycleFunction}{onPurifyMessageFunction}";
                    
					// Flattens script to remove new lines and carriage returns
					var flatScript = Util.FlattenString(ctx.ScriptText);
					
					// Add SAVE_STATS, PURIFY_ARTS, ON_PURIFY_RECYCLE, ON_PURIFY_MESSAGE functions to core.artifact.ArtifactOverview
					{
						// Tries to match ArtifactOverview class declaration
						var match = Regex.Match(flatScript, @"public class ArtifactOverview extends Sprite{");
						if (match.Success)
						{
							// Sets the position to the end of the match
							var position = match.Index + match.Length;
                            
							// Splits the first and the last part of the script text to insert text in between
							var firstPart = flatScript.Substring(0, position);
							var lastPart = flatScript.Substring(position);
                            
							// Inserts text in between firstPart and lastPart
							flatScript = $"{firstPart}{allFunctions}{lastPart}";
						}
					}
					
					return new PatchResult(flatScript);
				})
            ]),
            new PatchDescriptor("core.artifact.ArtifactCargoBox", [
				new Patch("Add Fitness stat on tooltip", (ctx) =>
				{
					// Gets the FITNESS_VALUE identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("FITNESS_VALUE", out var fitnessValueIdentifier))
						return null;
					
					// Flattens script to remove new lines and carriage returns
					var flatScript = Util.FlattenString(ctx.ScriptText);

					// Searches for the function definition
					var functionDefinitionMatch = Regex.Match(flatScript, @"private\s+function\s+addToolTip\s*\(.*?\)\s*:\s*\w+");
					if (!functionDefinitionMatch.Success)
						return null;
                    
					// Get function scope info
					var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
					if (scopeInfo is null)
						return null;
                    
					var scopeText = scopeInfo.ScopeText;
					
					// Find tooltip variable name
					var match = Regex.Match(scopeText, @"var\s+_loc(\d+)_:String\s+=\s+a\.name\s+\+\s+""<br>"";");
					if (!match.Success)
						return null;

					var tooltipTextVariableName = $"_loc{match.Groups[1].Value}_";
					
					// Find position to insert fitness stat in tooltip
					{
						var anchorForFitnessStatMatch = Regex.Match(scopeText, $@"{tooltipTextVariableName}\s*\+=\s*Localize\.t\(""([^""]*)""\)\.replace\(""\[level\]"",\s*a\.level\)\.replace\(""\[potential\]"",\s*a\.levelPotential\)\s*\+\s*""<br>"";");
						if (!match.Success)
							return null;
						
						int positionToInsert = anchorForFitnessStatMatch.Index + anchorForFitnessStatMatch.Length;
						var first = scopeText.Substring(0, positionToInsert);
						var last = scopeText.Substring(positionToInsert);

						var textToInsert = $@"{tooltipTextVariableName} += ""Fitness: "" + int(a.{fitnessValueIdentifier}) + ""<br>""";
						scopeText = $"{first}{textToInsert}{last}";
					}
					
					{
						// Split original script and re-insert block of function
						var first = flatScript.Substring(0, scopeInfo.Index);
						var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
						flatScript = $"{first}{scopeText}{last}";
					}

					return new PatchResult(flatScript);
				})
            ]),
            new PatchDescriptor("core.hud.components.chat.MessageLog", [
				new Patch("Add echo. and [client_dev] tag to writeChatMsg", (ctx) =>
				{
					// Gets the ECHO_VERSION identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("ECHO_VERSION", out var echoVersion))
						return null;
					
					// Flattens script to remove new lines and carriage returns
					var flatScript = Util.FlattenString(ctx.ScriptText);

					// Searches for the function definition
					var functionDefinitionMatch = Regex.Match(flatScript, @"public\s+static\s+function\s+writeChatMsg\s*\(.*?\)\s*:\s*\w+");
					if (!functionDefinitionMatch.Success)
						return null;
                    
					// Get function scope info
					var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
					if (scopeInfo is null)
						return null;
                    
					var scopeText = scopeInfo.ScopeText;

					var echoBlock = Util.FlattenString($@"
						if(param2 == ""echo."" && g.me.id != param3) {{
							if(param1 == ""global"" || param1 == ""private"") {{
								g.sendToServiceRoom(""chatMsg"",""private"",param4,""<b><font color=\'#34cceb\'>QoLAF v{echoVersion} (Auto-Patched) - ryd3v, Kaiser, Pancake</font></b>"");
							}}
							else {{
								g.sendToServiceRoom(""chatMsg"",""local"",""<b><font color=\'#34cceb\'>QoLAF v{echoVersion} (Auto-Patched) - ryd3v, Kaiser, Pancake</font></b>"");
							}}
						}}
					");
					
					var clientDevTagBlock = Util.FlattenString($@"
						if(param3 == ""steam76561199032900322"" || param3 == ""steam76561198188053594"") {{
							param5 = ""client_dev"";
						}}
					");
					
					// Get anchor to add echo.
					var anchorMatch = Regex.Match(scopeText,
						@"if\(g\.solarSystem\.type == ""pvp dom"" && param1 == ""local""\)");
					if (!anchorMatch.Success)
						return null;

					var blocksToInsert = $"{echoBlock}{clientDevTagBlock}";
					
					{
						// Insert text
						var first = scopeText.Substring(0, anchorMatch.Index);
						var last = scopeText.Substring(anchorMatch.Index);
						scopeText = $@"{first}{blocksToInsert}{last}";
					}
					
					{
						// Split original script and re-insert block of function
						var first = flatScript.Substring(0, scopeInfo.Index);
						var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
						flatScript = $"{first}{scopeText}{last}";
					}
					
					return new PatchResult(flatScript);
				}),
				new Patch("Add [client_dev] tag to colorCoding", (ctx) =>
				{
					// Flattens script to remove new lines and carriage returns
					var flatScript = Util.FlattenString(ctx.ScriptText);
                    
					// Searches for the function definition
					var functionDefinitionMatch = Regex.Match(flatScript, @"public\s+static\s+function\s+colorCoding\s*\(.*?\)\s*:\s*\w+");
					if (!functionDefinitionMatch.Success)
						return null;
                    
					// Get function scope info
					var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
					if (scopeInfo is null)
						return null;
                    
					var scopeText = scopeInfo.ScopeText;
					var textToInsert =
						@"param1 = param1.replace(""[client_dev]"",""<FONT COLOR=\'#c2fc03\'>[client_dev]</FONT>"");";

					{
						// Insert text
						var first = "{";
						var last = scopeText.Substring(1);
						scopeText = $@"{first}{textToInsert}{last}";
					}

					{
						// Split original script and re-insert block of function
						var first = flatScript.Substring(0, scopeInfo.Index);
						var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
						flatScript = $"{first}{scopeText}{last}";
					}
					
					return new PatchResult(flatScript);
				}),
				new Patch("Add [client_dev] tag to colorRights", (ctx) =>
				{
					// Flattens script to remove new lines and carriage returns
					var flatScript = Util.FlattenString(ctx.ScriptText);
                    
					// Searches for the function definition
					var functionDefinitionMatch = Regex.Match(flatScript, @"public\s+static\s+function\s+colorRights\s*\(.*?\)\s*:\s*\w+");
					if (!functionDefinitionMatch.Success)
						return null;
                    
					// Get function scope info
					var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
					if (scopeInfo is null)
						return null;
                    
					var scopeText = scopeInfo.ScopeText;
					var textToInsert = Util.FlattenString($@"
						if(param1 == ""client_dev"")
						{{
							return ""<FONT COLOR=\'#c2fc03\'>[client_dev]</FONT> "" + param2;
						}}
					");

					{
						// Insert text
						var first = "{";
						var last = scopeText.Substring(1);
						scopeText = $@"{first}{textToInsert}{last}";
					}

					{
						// Split original script and re-insert block of function
						var first = flatScript.Substring(0, scopeInfo.Index);
						var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
						flatScript = $"{first}{scopeText}{last}";
					}
					
					return new PatchResult(flatScript);
				})
            ]),
            new PatchDescriptor("core.hud.components.playerList.PlayerListItem", [
	            new Patch("Add [client_dev] tag to constructor", (ctx) =>
	            {
		            // Flattens script to remove new lines and carriage returns
		            var flatScript = Util.FlattenString(ctx.ScriptText);
                    
		            // Searches for the function definition
		            var functionDefinitionMatch = Regex.Match(flatScript, @"public\s+function\s+PlayerListItem\s*\(.*?\)\s*");
		            if (!functionDefinitionMatch.Success)
			            return null;
                    
		            // Get function scope info
		            var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
		            if (scopeInfo is null)
			            return null;
                    
		            var scopeText = scopeInfo.ScopeText;
		            
		            // Try to anchor to 'else if(player.isModerator)'
		            var anchorMatch = Regex.Match(scopeText, @"else if\(player\.isModerator\)");
		            if (!anchorMatch.Success)
			            return null;

		            // Try to find scope of the anchor
		            var anchorScope = Util.FindNextScope(scopeText, anchorMatch.Index + anchorMatch.Length);
		            if (anchorScope is null)
			            return null;
		            
		            var insertionPosition = anchorScope.Index + anchorScope.Length;
		            var textToInsert = $@"
						else if(player.id == ""steam76561199032900322"" || player.id == ""steam76561198188053594"") {{
							level.text = ""client dev"" + "" "" + level.text;
							level.format.color = 0xC2FC03;
						}}
					";

		            {
			            // Insert text
			            var first = scopeText.Substring(0, insertionPosition);
			            var last = scopeText.Substring(insertionPosition);
			            scopeText = $@"{first}{textToInsert}{last}";
		            }

		            {
			            // Split original script and re-insert block of function
			            var first = flatScript.Substring(0, scopeInfo.Index);
			            var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
			            flatScript = $"{first}{scopeText}{last}";
		            }
		            
		            return new PatchResult(flatScript);
	            })
            ]),
            new PatchDescriptor("core.ship.EnemyShip", [
	            new Patch("Remove enemy cloak on cloakStart and cloakEnd", (ctx) =>
	            {
		            // Flattens script to remove new lines and carriage returns
		            var flatScript = Util.FlattenString(ctx.ScriptText);

		            // cloakStart
		            {
			            // Searches for the function definition
			            var functionDefinitionMatch = Regex.Match(flatScript, @"public\s+function\s+cloakStart\s*\(.*?\)\s*:\s*\w+");
			            if (!functionDefinitionMatch.Success)
				            return null;
                    
			            // Get function scope info
			            var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
			            if (scopeInfo is null)
				            return null;
                    
			            {
				            // Split original script and re-insert block of function
				            var first = flatScript.Substring(0, scopeInfo.Index);
				            var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
				            flatScript = $"{first}{{}}{last}";
			            }
		            }
		            
					// cloakEnd
                    {
                        // Searches for the function definition
                        var functionDefinitionMatch = Regex.Match(flatScript, @"public\s+function\s+cloakEnd\s*\(.*?\)\s*:\s*\w+");
                        if (!functionDefinitionMatch.Success)
                            return null;
                    
                        // Get function scope info
                        var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
                        if (scopeInfo is null)
                            return null;
                    
                        {
                            // Split original script and re-insert block of function
                            var first = flatScript.Substring(0, scopeInfo.Index);
                            var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
                            flatScript = $"{first}{{addToCanvasForReal();}}{last}";
                        }
                    }
                    
                    return new PatchResult(flatScript);
	            })
            ]),
            new PatchDescriptor("core.hud.components.chat.ChatInputText", [
				new Patch("Add /rec command to open portable recycle", (ctx) =>
				{
					// Gets the OPEN_PORTABLE_RECYCLE identifier name
					if (!ctx.GetGlobalContext().GetIdentifier("OPEN_PORTABLE_RECYCLE", out var openPortableRecycleIdentifier))
						return null;
					
					// Flattens script to remove new lines and carriage returns
					var flatScript = Util.FlattenString(ctx.ScriptText);
					
					// Searches for the function definition
					var functionDefinitionMatch = Regex.Match(flatScript, @"public\s+function\s+cloakStart\s*\(.*?\)\s*:\s*\w+");
					if (!functionDefinitionMatch.Success)
						return null;
                    
					// Get function scope info
					var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
					if (scopeInfo is null)
						return null;
					
					var scopeText = scopeInfo.ScopeText;
					
					// Match anchor 'switch(output[0])'
					var anchorMatch = Regex.Match(scopeText, @"switch\(output\[0\]\)");
					if (!anchorMatch.Success)
						return null;
					
					// Get anchor scope
					var anchorScopeInfo = Util.FindNextScope(scopeText, anchorMatch.Index + anchorMatch.Length);
					if (anchorScopeInfo is null)
						return null;
					
					var anchorScopeText = anchorScopeInfo.ScopeText;
					var caseToInsert = Util.FlattenString(@$"
						case ""rec"":
							g.{openPortableRecycleIdentifier}();
							break;
					");

					{
						// Insert text on anchor block
						var first = "{";
						var last = anchorScopeText.Substring(1);
						anchorScopeText = $"{first}{caseToInsert}{last}";
					}

					{
						// Replace anchor scope on function scope
						var first = scopeText.Substring(0, anchorScopeInfo.Index);
						var last = scopeText.Substring(anchorScopeInfo.Index + anchorScopeInfo.Length);
						scopeText = $"{first}{anchorScopeText}{last}";
					}

					{
						// Split original script and re-insert block of function
						var first = flatScript.Substring(0, scopeInfo.Index);
						var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
						flatScript = $"{first}{scopeText}{last}";
					}
					
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