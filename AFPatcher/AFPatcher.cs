using System.Diagnostics;
using System.Globalization;
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
        Util.WriteLine("Creating temporary directory...");
        Directory.CreateDirectory(DecompilationDirectory);
        Util.WriteLine($"Created temporary directory: {TemporaryDirectory}");
    }
    
    private async Task Start()
    {
        if (!NativeFileDialog.OpenDialog(
                [new NativeFileDialog.Filter { Extensions = ["swf"], Name = "Shockwave Files" }], null,
                out var swfFile) ||
            !File.Exists(swfFile))
        {
            Util.WriteLine("No swf file found.");
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
                Util.WriteLine($"Qualified name '{desc.FullyQualifiedName}' does not exist.", ConsoleColor.Red);
                Util.WriteLine();
                continue;
            }
            
            var text = Util.FlattenString(await File.ReadAllTextAsync(file));
            Util.WriteLine($"Patching '{desc.FullyQualifiedName}'...", ConsoleColor.DarkCyan);
            int successPatches = 0;
            foreach (var patch in desc.Patches)
            {
                var result = patch.PatchFunction(new PatchContext(patches, text));
                if (result is null)
                {
                    Util.WriteLine($"    Couldn't apply the patch '{patch.Name}'.", ConsoleColor.Red);
                    continue;
                }

                Util.WriteLine($"    Patch '{patch.Name}' successfully applied to '{desc.FullyQualifiedName}'.", ConsoleColor.Green);
                successPatches++;
                text = result.ScriptText;
            }
            
            if (successPatches == desc.Patches.Length)
				Util.WriteLine($"Fully patched '{desc.FullyQualifiedName}'.", ConsoleColor.DarkCyan);
            else if (successPatches > 0)
	            Util.WriteLine($"Partially patched '{desc.FullyQualifiedName}'.", ConsoleColor.DarkYellow);
            else
				Util.WriteLine($"Failed to patch '{desc.FullyQualifiedName}'.", ConsoleColor.Red);
            Util.WriteLine();
            await File.WriteAllTextAsync(file, text);
            changedScripts.Add($"{desc.FullyQualifiedName} {file}");
        }

        #if DEBUG
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd",
            Arguments = $"/c start {DecompilationDirectory}",
            UseShellExecute = true
        });
        #endif
        
        Directory.CreateDirectory(Path.Combine(TemporaryDirectory, "recompiled"));
        await (Util.InvokeFFDec([string.Join(" ", ["-replace", swfFile, Path.Combine(TemporaryDirectory, "recompiled", Path.GetFileName(swfFile)), ..changedScripts])])?.WaitForExitAsync() ?? Task.CompletedTask);
        Util.WriteLine($"Recompiled '{swfFile}'.");
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
            new GlobalPatchContext(new Dictionary<string, object>()
            {
                { "FITNESS_LINE", "fitnessLine" },
                { "CALCULATE_FITNESS_OF_LINE", "calculateFitnessOfLine" },
                { "FITNESS_VALUE", "fitnessValue" },
                { "CALCULATE_FITNESS_VALUE", "calculateFitnessValue" },
                { "PURIFIED_ARTS", "purifiedArts" },
                { "PURIFY_BUTTON", "purifyButton" },
                { "SAVE_STATS_BUTTON", "saveStatsButton" },
                { "FITNESS_INPUT", "fitnessInput" },
                { "LINE_INPUT", "lineInput" },
                { "STRENGTH_INPUT", "strengthInput" },
                { "THIS_STRENGTH", "thisStrength" },
                { "THIS_FITNESS", "thisFitness" },
                { "THIS_LINES", "thisLines" },
                { "SAVE_STATS", "saveStats" },
                { "PURIFY_ARTS", "purifyArts" },
                { "ON_PURIFY_RECYCLE", "onPurifyRecycle" },
                { "ON_PURIFY_MESSAGE", "onPurifyMessage" },
                { "SHARED_OBJ", "sharedObj" },
                { "SAVE_SHARED_OBJ", "saveSharedObj" },
                { "LOAD_SHARED_OBJ", "loadSharedObj" },
                { "SET_PURIFY_STATS", "setPurifyStats" },
                { "OPEN_PORTABLE_RECYCLE", "openPortableRecycle" },
                { "SERVER_VERSION", 1392 },
                { "CLIENT_VERSION", "1.0.51 LS" },
                { 
	                "DEVELOPERS", 
	                new Dictionary<string, string>()
	                {
		                { "ryd3v", "steam76561199032900322" },
		                { "TheRealPancake", "steam76561198188053594" },
		                { "Kaiser/Primiano", "" },
		                { "TheLostOne", "simple1622136353425" },
		                { "mufenz", "" }
	                }
                },
                { "PAINT_MODE", "paintMode" },
                { "SET_EXTENDED", "setExtended" },
                { "SET_EXTREME", "setExtreme" },
                { "SET_ORIGINAL", "setOriginal" },
                { "SET_UNPAINT", "setUnpaint" },
                { "UPDATE_VISUALS", "updateVisuals" },
                { "TEST_DRIVE", "testDrive" },
                { "CUSTOM_TEST_DRIVE", "customTestDrive" },
                { "DELTA_TIME", "deltaTime" },
                { "CLIENT_DEV_HUE_COLOR", "clientDevHueColor" },
                { "ENGINE_COLOR", "engineColor" },
                { "ECHO_COLOR", "#ff4400" },
                { "DETERMINED_COLOR", "determinedColor" },
                { "DETERMINED_HUE", "determinedHue" },
                { "ECHO_FORMAT", "<font color='{0}'>QoLAF (Server {1} / v{2})</font>" }
            }),
            [
            new PatchDescriptor("core.scene.Game", [
                new Patch("Add DELTA_TIME variable", (ctx) =>
                {
	                // Gets the DELTA_TIME identifier name
	                if (!ctx.GetGlobalContext().GetIdentifier<string>("DELTA_TIME", out var deltaTimeIdentifier))
		                return null;
                    
	                // The text to insert
	                var insertingText = $@"public var {deltaTimeIdentifier}:Number = 0;";
                    
	                // Flattens script to remove new lines and carriage returns
	                var flatScript = Util.FlattenString(ctx.ScriptText);
                    
	                // Add DELTA_TIME variable to core.scene.Game
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
                new Patch("Add CLIENT_DEV_HUE_COLOR variable", (ctx) =>
                {
	                // Gets the CLIENT_DEV_HUE_COLOR identifier name
	                if (!ctx.GetGlobalContext().GetIdentifier<string>("CLIENT_DEV_HUE_COLOR", out var clientDevHueColorIdentifier))
		                return null;
                    
	                // The text to insert
	                var insertingText = $@"public var {clientDevHueColorIdentifier}:Number = 0;";
                    
	                // Flattens script to remove new lines and carriage returns
	                var flatScript = Util.FlattenString(ctx.ScriptText);
                    
	                // Add CLIENT_DEV_HUE_COLOR variable to core.scene.Game
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
	                if (!ctx.GetGlobalContext().GetIdentifier<string>("SHARED_OBJ", out var sharedObjIdentifier))
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
                new Patch("Add THIS_STRENGTH, THIS_FITNESS, THIS_LINES variables (Primiano/Kaiser)", (ctx) =>
                {
	                // Gets the THIS_STRENGTH identifier name
	                if (!ctx.GetGlobalContext().GetIdentifier<string>("THIS_STRENGTH", out var thisStrengthIdentifier))
		                return null;
	                
	                // Gets the THIS_FITNESS identifier name
	                if (!ctx.GetGlobalContext().GetIdentifier<string>("THIS_FITNESS", out var thisFitnessIdentifier))
		                return null;
	                
	                // Gets the THIS_LINES identifier name
	                if (!ctx.GetGlobalContext().GetIdentifier<string>("THIS_LINES", out var thisLinesIdentifier))
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
	                if (!ctx.GetGlobalContext().GetIdentifier<string>("SHARED_OBJ", out var sharedObjIdentifier))
		                return null;
	                
                    // Gets the SAVE_SHARED_OBJ identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier<string>("SAVE_SHARED_OBJ", out var saveSharedObjIdentifier))
                        return null;
                    
                    // Gets the LOAD_SHARED_OBJ identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier<string>("LOAD_SHARED_OBJ", out var loadSharedObjIdentifier))
	                    return null;
                    
                    // Gets the SET_PURIFY_STATS identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier<string>("SET_PURIFY_STATS", out var setPurifyStatsIdentifier))
                        return null;
                    
                    // Gets the THIS_STRENGTH identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier<string>("THIS_STRENGTH", out var thisStrengthIdentifier))
	                    return null;
	                
                    // Gets the THIS_FITNESS identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier<string>("THIS_FITNESS", out var thisFitnessIdentifier))
	                    return null;
	                
                    // Gets the THIS_LINES identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier<string>("THIS_LINES", out var thisLinesIdentifier))
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
                            flatScript = Regex.Replace(flatScript, @"private\s+function\s+reload", "public function reload");
                            
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
	                if (!ctx.GetGlobalContext().GetIdentifier<string>("SHARED_OBJ", out var sharedObjIdentifier))
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
	                if (!ctx.GetGlobalContext().GetIdentifier<string>("LOAD_SHARED_OBJ", out var loadSharedObjIdentifier))
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
                new Patch("Add DELTA_TIME on update function", (ctx) =>
                {
	                // Gets the DELTA_TIME identifier name
	                if (!ctx.GetGlobalContext().GetIdentifier<string>("DELTA_TIME", out var deltaTimeIdentifier))
		                return null;
	                
	                // Flattens script to remove new lines and carriage returns
	                var flatScript = Util.FlattenString(ctx.ScriptText);
                    
	                // Searches for the function definition
	                var functionDefinitionMatch = Regex.Match(flatScript, @"private\s+function\s+update\s*\(.*?\)\s*:\s*\w+");
	                if (!functionDefinitionMatch.Success)
		                return null;
                    
	                // Get function scope info
	                var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
	                if (scopeInfo is null)
		                return null;
                    
	                var scopeText = scopeInfo.ScopeText;
	                
	                var textToInsert = $@"this.{deltaTimeIdentifier} = param1.passedTime;";

	                {
		                // Insert text
		                var first = "{";
		                var last = scopeText.Substring(1);
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
                new Patch("Add OPEN_PORTABLE_RECYCLE function (TheRealPancake)", (ctx) =>
                {
	                // Gets the OPEN_PORTABLE_RECYCLE identifier name
	                if (!ctx.GetGlobalContext().GetIdentifier<string>("OPEN_PORTABLE_RECYCLE", out var openPortableRecycleIdentifier))
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
                }),
                new Patch("Add PAINT_MODE variable (mufenz)", (ctx) =>
                {
	                // Gets the PAINT_MODE identifier name
	                if (!ctx.GetGlobalContext().GetIdentifier<string>("PAINT_MODE", out var paintModeIdentifier))
		                return null;
                    
	                // The text to insert
	                var insertingText = $@"public var {paintModeIdentifier}:String = ""Original"";";
                    
	                // Flattens script to remove new lines and carriage returns
	                var flatScript = Util.FlattenString(ctx.ScriptText);
                    
	                // Add PAINT_MODE variable to core.scene.Game
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
                new Patch("Add [client_dev] thrust rainbow on tickUpdate function", (ctx) =>
                {
	                // Gets the DELTA_TIME identifier name
	                if (!ctx.GetGlobalContext().GetIdentifier<string>("DELTA_TIME", out var deltaTimeIdentifier))
		                return null;
	                
	                // Gets the CLIENT_DEV_HUE_COLOR identifier name
	                if (!ctx.GetGlobalContext().GetIdentifier<string>("CLIENT_DEV_HUE_COLOR", out var clientDevHueColorIdentifier))
		                return null;
	                
	                if (!ctx.GetGlobalContext().GetIdentifier<Dictionary<string, string>>("DEVELOPERS", out var developers))
		                return null;
	                
	                // Flattens script to remove new lines and carriage returns
	                var flatScript = Util.FlattenString(ctx.ScriptText);
                    
	                // Searches for the function definition
	                var functionDefinitionMatch = Regex.Match(flatScript, @"public\s+function\s+tickUpdate\s*\(.*?\)\s*:\s*\w+");
	                if (!functionDefinitionMatch.Success)
		                return null;
                    
	                // Get function scope info
	                var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
	                if (scopeInfo is null)
		                return null;

	                var scopeText = scopeInfo.ScopeText;
	                var textToInsert = Util.FlattenString($@"
						for each(var player in playerManager.players) {{
							if(!({string.Join(" && ", developers!.Select(kvp => kvp.Value != "" ? @$"player.id != ""{kvp.Value}""" : null).Where(v => v is not null))})) {{
								if(player.ship != null && player.ship.engine != null && player.ship.engine.thrustEmitters != null && player.ship.engine.idleThrustEmitters != null) {{
									for each(var emitter in player.ship.engine.thrustEmitters) {{
										emitter.changeHue({clientDevHueColorIdentifier});
									}}

									for each(emitter in player.ship.engine.idleThrustEmitters) {{
										emitter.changeHue({clientDevHueColorIdentifier});
									}}
								}}
							}}
						}}
						{clientDevHueColorIdentifier} += {deltaTimeIdentifier} * 2;
						if({clientDevHueColorIdentifier} >= Math.PI * 2) {{
							{clientDevHueColorIdentifier} = 0;
						}}
					");

	                {
		                // Insert text in between the strings
		                var first = scopeText.Substring(0, scopeInfo.Length - 1);
		                var second = "}";
		                scopeText = $"{first}{textToInsert}{second}";
	                }
	                
	                {
		                // Split original script and re-insert block of function
		                var first = flatScript.Substring(0, scopeInfo.Index);
		                var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
		                flatScript = $"{first}{scopeText}{last}";
	                }
	                
	                return new PatchResult(flatScript);
                }),
            ]),
            new PatchDescriptor("core.artifact.ArtifactStat", [
                new Patch("Add FITNESS_LINE variable (Primiano/Kaiser)", (ctx) =>
                {
                    // Gets the fitness line identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier<string>("FITNESS_LINE", out var fitnessLineIdentifier))
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
                new Patch("Add CALCULATE_FITNESS_OF_LINE function (Primiano/Kaiser)", (ctx) =>
                {
                    // Gets the CALCULATE_FITNESS_OF_LINE identifier name
                    if (!ctx.GetGlobalContext().GetIdentifier<string>("CALCULATE_FITNESS_OF_LINE", out var calculateFitnessOfLineIdentifier))
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
                new Patch("Initialize FITNESS_LINE with CALCULATE_FITNESS_OF_LINE function on constructor (Primiano/Kaiser)", (ctx) =>
                {
	                // Gets the FITNESS_LINE identifier name
	                if (!ctx.GetGlobalContext().GetIdentifier<string>("FITNESS_LINE", out var fitnessLineIdentifier))
		                return null;
	                
	                // Gets the CALCULATE_FITNESS_OF_LINE identifier name
	                if (!ctx.GetGlobalContext().GetIdentifier<string>("CALCULATE_FITNESS_OF_LINE", out var calculateFitnessOfLineIdentifier))
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
				new Patch("Add FITNESS_VALUE variable (Primiano/Kaiser)", (ctx) =>
				{
					// Gets the fitness value identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("FITNESS_VALUE", out var fitnessValueIdentifier))
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
				new Patch("Change orderStatCountAsc ordering mode to unique artifacts (Primiano/Kaiser)", (ctx) =>
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
				new Patch("Change orderStatCountDesc ordering mode to upgraded artifacts (Primiano/Kaiser)", (ctx) =>
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
				new Patch("Change orderLevelLow ordering mode to fitness (Primiano/Kaiser)", (ctx) =>
				{
					// Gets the fitness value identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("FITNESS_VALUE", out var fitnessValueIdentifier))
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
				new Patch("Add CALCULATE_FITNESS_VALUE function (Primiano/Kaiser)", (ctx) =>
				{
					// Gets the CALCULATE_FITNESS_VALUE identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("CALCULATE_FITNESS_VALUE", out var calculateFitnessValueIdentifier))
						return null;
					
					// Gets the FITNESS_LINE identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("FITNESS_LINE", out var fitnessLineIdentifier))
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
				new Patch("Add FITNESS_VALUE calculation to the update function (Primiano/Kaiser)", (ctx) =>
				{
					// Gets the FITNESS_VALUE identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("FITNESS_VALUE", out var fitnessValueIdentifier))
						return null;
					
					// Gets the CALCULATE_FITNESS_VALUE identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("CALCULATE_FITNESS_VALUE", out var calculateFitnessValueIdentifier))
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
				new Patch("Add PURIFIED_ARTS, PURIFY_BUTTON, SAVE_STATS_BUTTON, FITNESS_INPUT, LINE_INPUT, STRENGTH_INPUT variables (Primiano/Kaiser)", (ctx) =>
				{
					// Gets the PURIFIED_ARTS identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("PURIFIED_ARTS", out var purifiedArtsIdentifier))
						return null;
					
					// Gets the PURIFY_BUTTON identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("PURIFY_BUTTON", out var purifyButtonIdentifier))
						return null;
					
					// Gets the SAVE_STATS_BUTTON identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("SAVE_STATS_BUTTON", out var saveStatsButtonIdentifier))
						return null;
					
					// Gets the FITNESS_INPUT identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("FITNESS_INPUT", out var fitnessInputIdentifier))
						return null;
					
					// Gets the LINE_INPUT identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("LINE_INPUT", out var lineInputIdentifier))
						return null;
					
					// Gets the STRENGTH_INPUT identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("STRENGTH_INPUT", out var strengthInputIdentifier))
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
				new Patch("Add purification components to drawComponents (Primiano/Kaiser)", (ctx) =>
				{
					// Gets the PURIFIED_ARTS identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("PURIFIED_ARTS", out var purifiedArtsIdentifier))
						return null;
					
					// Gets the PURIFY_BUTTON identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("PURIFY_BUTTON", out var purifyButtonIdentifier))
						return null;
					
					// Gets the SAVE_STATS_BUTTON identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("SAVE_STATS_BUTTON", out var saveStatsButtonIdentifier))
						return null;
					
					// Gets the FITNESS_INPUT identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("FITNESS_INPUT", out var fitnessInputIdentifier))
						return null;
					
					// Gets the LINE_INPUT identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("LINE_INPUT", out var lineInputIdentifier))
						return null;
					
					// Gets the STRENGTH_INPUT identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("STRENGTH_INPUT", out var strengthInputIdentifier))
						return null;
					
					// Gets the THIS_STRENGTH identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("THIS_STRENGTH", out var thisStrengthIdentifier))
						return null;
	                
					// Gets the THIS_FITNESS identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("THIS_FITNESS", out var thisFitnessIdentifier))
						return null;
	                
					// Gets the THIS_LINES identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("THIS_LINES", out var thisLinesIdentifier))
						return null;
					
					// Gets the SAVE_STATS identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("SAVE_STATS", out var saveStatsIdentifier))
						return null;
                    
					// Gets the PURIFY_ARTS identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("PURIFY_ARTS", out var purifyArtsIdentifier))
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
				new Patch("Add SAVE_STATS, PURIFY_ARTS, ON_PURIFY_RECYCLE, ON_PURIFY_MESSAGE functions (Primiano/Kaiser)", (ctx) =>
				{
					// Gets the PURIFIED_ARTS identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("PURIFIED_ARTS", out var purifiedArtsIdentifier))
						return null;
					
					// Gets the SAVE_STATS identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("SAVE_STATS", out var saveStatsIdentifier))
						return null;
                    
					// Gets the PURIFY_ARTS identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("PURIFY_ARTS", out var purifyArtsIdentifier))
						return null;
					
					// Gets the ON_PURIFY_RECYCLE identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("ON_PURIFY_RECYCLE", out var onPurifyRecycleIdentifier))
						return null;
					
					// Gets the ON_PURIFY_MESSAGE identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("ON_PURIFY_MESSAGE", out var onPurifyMessageIdentifier))
						return null;
					
					// Gets the PURIFY_BUTTON identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("PURIFY_BUTTON", out var purifyButtonIdentifier))
						return null;
					
					// Gets the SAVE_STATS_BUTTON identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("SAVE_STATS_BUTTON", out var saveStatsButtonIdentifier))
						return null;
					
					// Gets the FITNESS_INPUT identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("FITNESS_INPUT", out var fitnessInputIdentifier))
						return null;
					
					// Gets the LINE_INPUT identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("LINE_INPUT", out var lineInputIdentifier))
						return null;
					
					// Gets the STRENGTH_INPUT identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("STRENGTH_INPUT", out var strengthInputIdentifier))
						return null;
					
					// Gets the THIS_STRENGTH identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("THIS_STRENGTH", out var thisStrengthIdentifier))
						return null;
	                
					// Gets the THIS_FITNESS identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("THIS_FITNESS", out var thisFitnessIdentifier))
						return null;
	                
					// Gets the THIS_LINES identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("THIS_LINES", out var thisLinesIdentifier))
						return null;
					
					// Gets the fitness value identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("FITNESS_VALUE", out var fitnessValueIdentifier))
						return null;
					
					// Gets the SET_PURIFY_STATS identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("SET_PURIFY_STATS", out var setPurifyStatsIdentifier))
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
				new Patch("Add Fitness stat on tooltip (Primiano/Kaiser)", (ctx) =>
				{
					// Gets the FITNESS_VALUE identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("FITNESS_VALUE", out var fitnessValueIdentifier))
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
				new Patch("Add echo. and [client_dev] tag to writeChatMsg (ryd3v)", (ctx) =>
				{
					if (!ctx.GetGlobalContext().GetIdentifier<int>("SERVER_VERSION", out var serverVersion))
						return null;
					if (!ctx.GetGlobalContext().GetIdentifier<string>("CLIENT_VERSION", out var clientVersion))
						return null;
					if (!ctx.GetGlobalContext().GetIdentifier<string>("ECHO_COLOR", out var echoColor))
						return null;
					if (!ctx.GetGlobalContext().GetIdentifier<string>("ECHO_FORMAT", out var echoFormat))
						return null;
					if (!ctx.GetGlobalContext().GetIdentifier<Dictionary<string, string>>("DEVELOPERS", out var developers))
						return null;
					
					var flatScript = Util.FlattenString(ctx.ScriptText);
					var functionDefinitionMatch = Regex.Match(flatScript, @"public\s+static\s+function\s+writeChatMsg\s*\(.*?\)\s*:\s*\w+");
					if (!functionDefinitionMatch.Success)
						return null;
					
					var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
					if (scopeInfo is null)
						return null;
                    
					var scopeText = scopeInfo.ScopeText;

					var echoBlock = Util.FlattenString($@"
						if(param2 == ""echo."" && g.me.id != param3) {{
							if(param1 == ""global"" || param1 == ""private"") {{
								g.sendToServiceRoom(""chatMsg"",""private"",param4,""{string.Format(echoFormat!, echoColor, serverVersion, clientVersion)}"");
							}}
							else {{
								g.sendToServiceRoom(""chatMsg"",""local"",""{string.Format(echoFormat!, echoColor, serverVersion, clientVersion)}"");
							}}
						}}
					");
					
					var clientDevTagBlock = Util.FlattenString($@"
						if({string.Join(" || ", developers!.Select(kvp => kvp.Value != "" ? @$"param3 == ""{kvp.Value}""" : null).Where(v => v is not null))}) {{
							param5 += ""client_dev"";
						}}
					");
					
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
				new Patch("Add [client_dev] tag to colorCoding (ryd3v)", (ctx) =>
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
				new Patch("Add [client_dev] tag to colorRights (ryd3v)", (ctx) =>
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
		            if (!ctx.GetGlobalContext().GetIdentifier<Dictionary<string, string>>("DEVELOPERS", out var developers))
			            return null;
		            
		            var flatScript = Util.FlattenString(ctx.ScriptText);
		            var functionDefinitionMatch = Regex.Match(flatScript, @"public\s+function\s+PlayerListItem\s*\(.*?\)\s*");
		            if (!functionDefinitionMatch.Success)
			            return null;
		            
		            var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
		            if (scopeInfo is null)
			            return null;
                    
		            var scopeText = scopeInfo.ScopeText;

		            var anchorMatch = Regex.Match(scopeText, @"else if\(player\.isModerator\)");
		            if (!anchorMatch.Success)
			            return null;
		            
		            var anchorScope = Util.FindNextScope(scopeText, anchorMatch.Index + anchorMatch.Length);
		            if (anchorScope is null)
			            return null;
		            
		            var insertionPosition = anchorScope.Index + anchorScope.Length;
		            var textToInsert = $@"
						else if({string.Join(" || ", developers!.Select(kvp => kvp.Value != "" ? @$"player.id == ""{kvp.Value}""" : null).Where(v => v is not null))}) {{
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
				new Patch("Add /rec command to open portable recycle (TheRealPancake)", (ctx) =>
				{
					// Gets the OPEN_PORTABLE_RECYCLE identifier name
					if (!ctx.GetGlobalContext().GetIdentifier<string>("OPEN_PORTABLE_RECYCLE", out var openPortableRecycleIdentifier))
						return null;
					if (!ctx.GetGlobalContext().GetIdentifier<int>("SERVER_VERSION", out var serverVersion))
						return null;
					if (!ctx.GetGlobalContext().GetIdentifier<string>("CLIENT_VERSION", out var clientVersion))
						return null;
					if (!ctx.GetGlobalContext().GetIdentifier<string>("ECHO_COLOR", out var echoColor))
						return null;
					if (!ctx.GetGlobalContext().GetIdentifier<string>("ECHO_FORMAT", out var echoFormat))
						return null;
					
					// Flattens script to remove new lines and carriage returns
					var flatScript = Util.FlattenString(ctx.ScriptText);
					
					// Searches for the function definition
					var functionDefinitionMatch = Regex.Match(flatScript, @"private\s+function\s+sendMessage\s*\(.*?\)\s*:\s*\w+");
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
						case ""echo"":
							MessageLog.writeChatMsg(""death"",""Your client is: {string.Format(echoFormat!, echoColor, serverVersion, clientVersion)}"");
							break;
						case ""ref"":
							starling.core.Starling.juggler.delayCall(g.reload, 0.5);
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
            ]),
            new PatchDescriptor("camerafocus.StarlingCameraFocus", [
				new Patch("Remove screen shaking", (ctx) =>
				{
					// Flattens script to remove new lines and carriage returns
					var flatScript = Util.FlattenString(ctx.ScriptText);
					
					// Searches for the function definition
					var functionDefinitionMatch = Regex.Match(flatScript, @"public\s+function\s+shake\s*\(.*?\)\s*:\s*\w+");
					if (!functionDefinitionMatch.Success)
						return null;
                    
					// Get function scope info
					var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
					if (scopeInfo is null)
						return null;
					
					// Set empty scope
					var first = flatScript.Substring(0, scopeInfo.Index);
					var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
					flatScript = $"{first}{{}}{last}";
					
					return new PatchResult(flatScript);
				})
            ]),
            new PatchDescriptor("core.states.gameStates.LandedPaintShop", [
	            new Patch("Add buttons and sliders for special painting (mufenz)", (ctx) =>
	            {
		            // Gets the PAINT_MODE identifier name
		            if (!ctx.GetGlobalContext().GetIdentifier<string>("PAINT_MODE", out var paintModeIdentifier))
			            return null;
		            
		            // Gets the SET_EXTENDED identifier name
		            if (!ctx.GetGlobalContext().GetIdentifier<string>("SET_EXTENDED", out var setExtendedIdentifier))
			            return null;
		            
		            // Gets the SET_EXTREME identifier name
		            if (!ctx.GetGlobalContext().GetIdentifier<string>("SET_EXTREME", out var setExtremeIdentifier))
			            return null;
		            
		            // Gets the SET_ORIGINAL identifier name
		            if (!ctx.GetGlobalContext().GetIdentifier<string>("SET_ORIGINAL", out var setOriginalIdentifier))
			            return null;
		            
		            // Gets the SET_UNPAINT identifier name
		            if (!ctx.GetGlobalContext().GetIdentifier<string>("SET_UNPAINT", out var setUnpaintIdentifier))
			            return null;
		            
		            // Gets the UPDATE_VISUALS identifier name
		            if (!ctx.GetGlobalContext().GetIdentifier<string>("UPDATE_VISUALS", out var updateVisualsIdentifier))
			            return null;
		            
		            // Gets the CUSTOM_TEST_DRIVE identifier name
		            if (!ctx.GetGlobalContext().GetIdentifier<string>("CUSTOM_TEST_DRIVE", out var customTestDriveIdentifier))
			            return null;
		            
		            var flatScript = Util.FlattenString(ctx.ScriptText);
		            var functionDefinitionMatch = Regex.Match(flatScript, @"override\s+public\s+function\s+enter\s*\(.*?\)\s*:\s*\w+");
		            if (!functionDefinitionMatch.Success)
			            return null;
		            
		            var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
		            if (scopeInfo is null)
			            return null;
                    
		            var scopeText = scopeInfo.ScopeText;
		            var newButtonsAndSlidersToInsert = Util.FlattenString($@"
						var currentModeText:Text = new Text();
						currentModeText.text = g.{paintModeIdentifier};
						currentModeText.x = 200;
						currentModeText.y = 100;
						currentModeText.size = 23;
						currentModeText.color = 16515901;
						addChild(currentModeText);
						var buttons:Button = new Button({setExtendedIdentifier},""Extended"",""highlight"");
						buttons.x = 200;
						buttons.y = 390;
						addChild(buttons);
						buttons = new Button({setExtremeIdentifier},""Extreme"",""highlight"");
						buttons.x = 5 * 60;
						buttons.y = 390;
						addChild(buttons);
						buttons = new Button({setUnpaintIdentifier},""Un-Paint"",""highlight"");
						buttons.x = 400;
						buttons.y = 390;
						addChild(buttons);
						buttons = new Button({setOriginalIdentifier},""Original"",""highlight"");
						buttons.x = 500;
						buttons.y = 390;
						addChild(buttons);
						buttons = new Button({customTestDriveIdentifier},""Free Test Drive"",""positive"");
						buttons.x = 270;
						buttons.y = 485;
						addChild(buttons);
					");

		            var ifPaintModeIsUnpaintInsertText = Util.FlattenString($@"
						if(g.{paintModeIdentifier} == ""Un-Paint"") {{
							sliderShipHue.minimum = 0;
							sliderShipHue.maximum = 0;
						}}
					");

		            var sliderShipBrightnessIfBlock = Util.FlattenString($@"
						if(g.{paintModeIdentifier} == ""Extended"") {{
							sliderShipBrightness.minimum = -1.18;
							sliderShipBrightness.maximum = 1.04;
						}}
						if(g.{paintModeIdentifier} == ""Extreme"") {{
							sliderShipBrightness.minimum = -2147483648;
							sliderShipBrightness.maximum = 0x7fffffff;
						}}
						if(g.{paintModeIdentifier} == ""Un-Paint"") {{
							sliderShipBrightness.minimum = 0;
							sliderShipBrightness.maximum = 0;
						}}
					");
		            
		            var sliderShipSaturationIfBlock = Util.FlattenString($@"
						if(g.{paintModeIdentifier} == ""Extended"") {{
							sliderShipSaturation.minimum = -3;
							sliderShipSaturation.maximum = 15;
						}}
						if(g.{paintModeIdentifier} == ""Extreme"") {{
							sliderShipSaturation.minimum = -2147483648;
							sliderShipSaturation.maximum = 0x7fffffff;
						}}
						if(g.{paintModeIdentifier} == ""Un-Paint"") {{
							sliderShipSaturation.minimum = 0;
							sliderShipSaturation.maximum = 0;
						}}
					");
		            
		            var sliderShipContrastIfBlock = Util.FlattenString($@"
						if(g.{paintModeIdentifier} == ""Extended"") {{
							sliderShipContrast.minimum = -4;
							sliderShipContrast.maximum = 8;
						}}
						if(g.{paintModeIdentifier} == ""Extreme"") {{
							sliderShipContrast.minimum = -2147483648;
							sliderShipContrast.maximum = 0x7fffffff;
						}}
						if(g.{paintModeIdentifier} == ""Un-Paint"") {{
							sliderShipContrast.minimum = 0;
							sliderShipContrast.maximum = 0;
						}}
					");

		            var hueUnpaintToInsert = Util.FlattenString($@"
						if(g.{paintModeIdentifier} == ""Un-Paint"") {{
							sliderEngineHue.minimum = 0;
							sliderEngineHue.maximum = 0;
						}}
					");

		            {
			            // Comment cuz this is kinda confusing, I have an empty capture to split the match in 2 parts (it's an anchor to insert text in between)
			            var anchorMatch = Regex.Match(scopeText,
				            @"(?:this.)?addShip\(\);()[^""]*\s+=\s+dataManager\.loadKey\(""Skins"",(?:this.)?g\.me\.activeSkin\);");
		            
			            if (!anchorMatch.Success)
				            return null;

			            {
				            // Insert text in between the strings
				            var first = scopeText.Substring(0, anchorMatch.Groups[1].Index);
				            var second = scopeText.Substring(anchorMatch.Groups[1].Index);
				            scopeText = $"{first}{newButtonsAndSlidersToInsert}{second}";
			            }
		            }

		            {
			            // Comment cuz this is kinda confusing, I have an empty capture to split the match in 2 parts (it's an anchor to insert text in between)
			            var anchorMatch = Regex.Match(scopeText,
				            @"(?:this.)?sliderShipHue\.maximum\s+=\s+1\.8707963267948966;()(?:this.)?sliderShipHue\.width\s+=\s+200;");
		            
			            if (!anchorMatch.Success)
				            return null;

			            {
				            // Insert text in between the strings
				            var first = scopeText.Substring(0, anchorMatch.Groups[1].Index);
				            var second = scopeText.Substring(anchorMatch.Groups[1].Index);
				            scopeText = $"{first}{ifPaintModeIsUnpaintInsertText}{second}";
			            }
		            }
		            
		            {
			            // Comment cuz this is kinda confusing, I have an empty capture to split the match in 2 parts (it's an anchor to insert text in between)
			            var anchorMatch = Regex.Match(scopeText,
				            @"(?:this.)?sliderShipBrightness\.maximum\s+=\s+0\.04;()(?:this.)?sliderShipBrightness\.width\s+=\s+200;");
		            
			            if (!anchorMatch.Success)
				            return null;

			            {
				            // Insert text in between the strings
				            var first = scopeText.Substring(0, anchorMatch.Groups[1].Index);
				            var second = scopeText.Substring(anchorMatch.Groups[1].Index);
				            scopeText = $"{first}{sliderShipBrightnessIfBlock}{second}";
			            }
		            }
		            
		            {
			            // Comment cuz this is kinda confusing, I have an empty capture to split the match in 2 parts (it's an anchor to insert text in between)
			            var anchorMatch = Regex.Match(scopeText,
				            @"(?:this.)?sliderShipSaturation\.maximum\s+=\s+1;()(?:this.)?sliderShipSaturation\.width\s+=\s+200;");
		            
			            if (!anchorMatch.Success)
				            return null;

			            {
				            // Insert text in between the strings
				            var first = scopeText.Substring(0, anchorMatch.Groups[1].Index);
				            var second = scopeText.Substring(anchorMatch.Groups[1].Index);
				            scopeText = $"{first}{sliderShipSaturationIfBlock}{second}";
			            }
		            }
		            
		            {
			            // Comment cuz this is kinda confusing, I have an empty capture to split the match in 2 parts (it's an anchor to insert text in between)
			            var anchorMatch = Regex.Match(scopeText,
				            @"(?:this.)?sliderShipContrast\.maximum\s+=\s+1;()(?:this.)?sliderShipContrast\.width\s+=\s+200;");
		            
			            if (!anchorMatch.Success)
				            return null;

			            {
				            // Insert text in between the strings
				            var first = scopeText.Substring(0, anchorMatch.Groups[1].Index);
				            var second = scopeText.Substring(anchorMatch.Groups[1].Index);
				            scopeText = $"{first}{sliderShipContrastIfBlock}{second}";
			            }
		            }
		            
		            {
			            // Comment cuz this is kinda confusing, I have an empty capture to split the match in 2 parts (it's an anchor to insert text in between)
			            var anchorMatch = Regex.Match(scopeText,
				            @"(?:this.)?sliderEngineHue\.maximum\s+=\s+3\.141592653589793;()(?:this.)?sliderEngineHue\.width\s+=\s+200;");
		            
			            if (!anchorMatch.Success)
				            return null;

			            {
				            // Insert text in between the strings
				            var first = scopeText.Substring(0, anchorMatch.Groups[1].Index);
				            var second = scopeText.Substring(anchorMatch.Groups[1].Index);
				            scopeText = $"{first}{hueUnpaintToInsert}{second}";
			            }
		            }
		            
		            {
			            // Comment cuz this is kinda confusing, I have an empty capture to split the match in 2 parts (it's an anchor to insert text in between)
			            var anchorMatch = Regex.Match(scopeText,
				            @"(?:this.)?loadCompleted\(\);()if\(RymdenRunt\.isBuggedFlashVersion\)");
		            
			            if (!anchorMatch.Success)
				            return null;

			            {
				            // Insert text in between the strings
				            var first = scopeText.Substring(0, anchorMatch.Groups[1].Index);
				            var second = scopeText.Substring(anchorMatch.Groups[1].Index);
				            scopeText = $@"{first}{updateVisualsIdentifier}();{second}";
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
	            new Patch("Add SET_EXTENDED, SET_EXTREME, SET_ORIGINAL, SET_UNPAINT, UPDATE_VISUALS, TEST_DRIVE functions (mufenz)", (ctx) =>
	            {
		            // Gets the PAINT_MODE identifier name
		            if (!ctx.GetGlobalContext().GetIdentifier<string>("PAINT_MODE", out var paintModeIdentifier))
			            return null;
		            
		            // Gets the SET_EXTENDED identifier name
		            if (!ctx.GetGlobalContext().GetIdentifier<string>("SET_EXTENDED", out var setExtendedIdentifier))
			            return null;
		            
		            // Gets the SET_EXTREME identifier name
		            if (!ctx.GetGlobalContext().GetIdentifier<string>("SET_EXTREME", out var setExtremeIdentifier))
			            return null;
		            
		            // Gets the SET_ORIGINAL identifier name
		            if (!ctx.GetGlobalContext().GetIdentifier<string>("SET_ORIGINAL", out var setOriginalIdentifier))
			            return null;
		            
		            // Gets the SET_UNPAINT identifier name
		            if (!ctx.GetGlobalContext().GetIdentifier<string>("SET_UNPAINT", out var setUnpaintIdentifier))
			            return null;
		            
		            // Gets the UPDATE_VISUALS identifier name
		            if (!ctx.GetGlobalContext().GetIdentifier<string>("UPDATE_VISUALS", out var updateVisualsIdentifier))
			            return null;
		            
		            // Gets the TEST_DRIVE identifier name
		            if (!ctx.GetGlobalContext().GetIdentifier<string>("TEST_DRIVE", out var testDriveIdentifier))
			            return null;
		            
		            // Gets the CUSTOM_TEST_DRIVE identifier name
		            if (!ctx.GetGlobalContext().GetIdentifier<string>("CUSTOM_TEST_DRIVE", out var customTestDriveIdentifier))
			            return null;
		            
		            // Gets the fitness line identifier name
		            if (!ctx.GetGlobalContext().GetIdentifier<string>("ENGINE_COLOR", out var engineColorIdentifier))
			            return null;

		            var functionsToInsert = Util.FlattenString($@"
						public function reEnter(): void {{
							leave();
							starling.core.Starling.juggler.delayCall(function():void {{
								g.fadeIntoState(new LandedPaintShop(g,body));
							}},1);
						}}

						public function {setExtendedIdentifier}(e:TouchEvent = null): void {{
							g.{paintModeIdentifier} = ""Extended"";
							reEnter();
						}}
						
						public function {setExtremeIdentifier}(e:TouchEvent = null): void {{
							g.{paintModeIdentifier} = ""Extreme"";
							reEnter();
						}}
						
						public function {setOriginalIdentifier}(e:TouchEvent = null): void {{
							g.{paintModeIdentifier} = ""Original"";
							reEnter();
						}}
						
						public function {setUnpaintIdentifier}(e:TouchEvent = null): void {{
							g.{paintModeIdentifier} = ""Un-Paint"";
							reEnter();
						}}
						
						public function {updateVisualsIdentifier}(): void {{
							var filter:ColorMatrixFilter = new ColorMatrixFilter();
							filter.adjustHue(sliderShipHue.value);
							filter.adjustBrightness(sliderShipBrightness.value);
							filter.adjustSaturation(sliderShipSaturation.value);
							filter.adjustContrast(sliderShipContrast.value);
							preview.movieClip.filter = filter;
							for each(emitter in emitters) {{
								emitter.changeHue(sliderEngineHue.value);
							}}
						}}
						
						public function {testDriveIdentifier}(e:TouchEvent = null) : void {{
							var filter:ColorMatrixFilter = new ColorMatrixFilter();
							filter.adjustHue(sliderShipHue.value);
							filter.adjustBrightness(sliderShipBrightness.value);
							filter.adjustSaturation(sliderShipSaturation.value);
							filter.adjustContrast(sliderShipContrast.value);
							g.me.ship.movieClip.filter = filter;
							g.me.ship.originalFilter = filter;
						}}
						
						public function {customTestDriveIdentifier}(e:TouchEvent = null) : void
						{{
							leave();
							g.enterState(new RoamingState(g));
							starling.core.Starling.juggler.delayCall({testDriveIdentifier},1);
							starling.core.Starling.juggler.delayCall({testDriveIdentifier},2);
							g.me.{engineColorIdentifier} = sliderEngineHue.value;
						}}
					");
                    
		            // Flattens script to remove new lines and carriage returns
		            var flatScript = Util.FlattenString(ctx.ScriptText);

		            // Add SET_EXTENDED, SET_EXTREME, SET_ORIGINAL, SET_UNPAINT, UPDATE_VISUALS, TEST_DRIVE function to core.scene.Game
		            {
			            // Tries to match Game class declaration
			            var match = Regex.Match(flatScript, @"public class LandedPaintShop extends LandedState{");
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
	            })
            ]),
            new PatchDescriptor("core.player.Player", [
	            new Patch("Add ENGINE_COLOR variable (mufenz)", (ctx) =>
	            {
		            // Gets the fitness line identifier name
		            if (!ctx.GetGlobalContext().GetIdentifier<string>("ENGINE_COLOR", out var engineColorIdentifier))
			            return null;
                    
		            // The text to insert
		            var insertingText = $@"public var {engineColorIdentifier}:Number = 0;";
                    
		            // Flattens script to remove new lines and carriage returns
		            var flatScript = Util.FlattenString(ctx.ScriptText);
                    
		            // Add ENGINE_COLOR variable to core.player.Player
		            {
			            // Tries to match Player class declaration
			            var match = Regex.Match(flatScript, @"public class Player{");
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
            new PatchDescriptor("core.ship.ShipFactory", [
	            new Patch("Set engine color in createPlayer (mufenz)", (ctx) =>
	            {
		            // Gets the fitness line identifier name
		            if (!ctx.GetGlobalContext().GetIdentifier<string>("ENGINE_COLOR", out var engineColorIdentifier))
			            return null;
		            
		            var flatScript = Util.FlattenString(ctx.ScriptText);
		            var functionDefinitionMatch = Regex.Match(flatScript, @"public\s+static\s+function\s+createPlayer\s*\(.*?\)\s*:\s*\w+");
		            if (!functionDefinitionMatch.Success)
			            return null;
		            
		            var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
		            if (scopeInfo is null)
			            return null;
                    
		            var scopeText = scopeInfo.ScopeText;
		            var setEngineColorToInsert = Util.FlattenString($@"
						if(param2.{engineColorIdentifier} != 0) {{
							param3.engine.colorHue = param2.{engineColorIdentifier};
							param2.{engineColorIdentifier} = 0;
						}}
					");
		            
		            {
			            // Comment cuz this is kinda confusing, I have an empty capture to split the match in 2 parts (it's an anchor to insert text in between)
			            var anchorMatch = Regex.Match(scopeText,
				            @"param\d+\.engine\.colorHue\s+=\s+[^""]*;()CreatePlayerShipWeapon\(.*?\);");
		            
			            if (!anchorMatch.Success)
				            return null;

			            {
				            // Insert text in between the strings
				            var first = scopeText.Substring(0, anchorMatch.Groups[1].Index);
				            var second = scopeText.Substring(anchorMatch.Groups[1].Index);
				            scopeText = $"{first}{setEngineColorToInsert}{second}";
			            }
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
            new PatchDescriptor("Login", [
				new Patch("Add version and title client text in init (ryd3v)", (ctx) =>
				{
					if (!ctx.GetGlobalContext().GetIdentifier<string>("CLIENT_VERSION", out var clientVersion))
						return null;
					
					if (!ctx.GetGlobalContext().GetIdentifier<Dictionary<string, string>>("DEVELOPERS", out var developers))
						return null;
					
					var flatScript = Util.FlattenString(ctx.ScriptText);
					var functionDefinitionMatch = Regex.Match(flatScript, @"private\s+function\s+init\s*\(.*?\)\s*:\s*\w+");
					if (!functionDefinitionMatch.Success)
						return null;
					
					var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
					if (scopeInfo is null)
						return null;
                    
					var scopeText = scopeInfo.ScopeText;
					var textToInsert = Util.FlattenString($@"
						var infoText:TextField = new TextField(0, 0, """", new TextFormat(""DAIDRR"", 12, 0xffffff));
				        infoText.x = 10;
				        infoText.y = 10;
				        infoText.autoSize = starling.text.TextFieldAutoSize.BOTH_DIRECTIONS;
				        infoText.format.horizontalAlign = starling.utils.Align.LEFT;
				        infoText.text = 
								""QoLAF - v{clientVersion} (Auto-Patched)\n"" +
				                ""Contains modifications from:\n"" + 
								{string.Join("\n + ", developers!.Select(kvp => @$"""  - {kvp.Key}\n"""))};
				        addChild(infoText);
					");
					
					{
						var anchorMatch = Regex.Match(scopeText,
							@"addChild\((?:this.)?logoContainer\);()if\(!RymdenRunt\.isDesktop\)");
		            
						if (!anchorMatch.Success)
							return null;

						var first = scopeText.Substring(0, anchorMatch.Groups[1].Index);
						var second = scopeText.Substring(anchorMatch.Groups[1].Index);
						scopeText = $"{first}{textToInsert}{second}";
					}
					{
						var first = flatScript.Substring(0, scopeInfo.Index);
						var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
						flatScript = $"{first}{scopeText}{last}";
					}
					
					return new PatchResult(flatScript);
				})
            ]),
            new PatchDescriptor("core.drops.Drop", [
				new Patch("Multiply Unique Artifact emitters size and max particles by 1.5 (TheLostOne)", (ctx) =>
				{
					var flatScript = Util.FlattenString(ctx.ScriptText);
					var functionDefinitionMatch = Regex.Match(flatScript, @"public\s+function\s+addToCanvasForReal\s*\(.*?\)\s*:\s*\w+");
					if (!functionDefinitionMatch.Success)
						return null;
					
					var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
					if (scopeInfo is null)
						return null;
                    
					var scopeText = scopeInfo.ScopeText;
					var textToInsert = Util.FlattenString($@"
						for each (var e:core.particle.Emitter in effect) {{
							e.startSize *= 1.5;
							e.finishSize *= 1.5;
							e.maxParticles *= 1.5;
						}}
					");
					
					{
						var anchorMatch = Regex.Match(scopeText,
							@"effect\[\d+\]\.startColor\s+=\s+[^""]+;()effect\[\d+\]\.finishColor\s+=\s+[^""]+;");
		            
						if (!anchorMatch.Success)
							return null;

						var first = scopeText.Substring(0, anchorMatch.Groups[1].Index);
						var second = scopeText.Substring(anchorMatch.Groups[1].Index);
						scopeText = $"{first}{textToInsert}{second}";
					}
					{
						var first = flatScript.Substring(0, scopeInfo.Index);
						var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
						flatScript = $"{first}{scopeText}{last}";
					}
					
					return new PatchResult(flatScript);
				})
            ]),
            new PatchDescriptor("core.weapon.Beam", [
	            new Patch("Add [client_dev] beam rainbow on draw function", (ctx) =>
	            {
		            // Gets the CLIENT_DEV_HUE_COLOR identifier name
	                if (!ctx.GetGlobalContext().GetIdentifier<string>("CLIENT_DEV_HUE_COLOR", out var clientDevHueColorIdentifier))
		                return null;
	                
	                if (!ctx.GetGlobalContext().GetIdentifier<Dictionary<string, string>>("DEVELOPERS", out var developers))
		                return null;
	                
	                // Flattens script to remove new lines and carriage returns
	                var flatScript = Util.FlattenString(ctx.ScriptText);
                    
	                // Searches for the function definition
	                var functionDefinitionMatch = Regex.Match(flatScript, @"override\s+public\s+function\s+draw\s*\(.*?\)\s*:\s*\w+");
	                if (!functionDefinitionMatch.Success)
		                return null;
                    
	                // Get function scope info
	                var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
	                if (scopeInfo is null)
		                return null;

	                var scopeText = scopeInfo.ScopeText;
	                var textToInsert = Util.FlattenString($@"
						if(unit is core.ship.PlayerShip) {{
							var player:core.player.Player = (unit as core.ship.PlayerShip).player;
							if (!({string.Join(" && ", developers!.Select(kvp => kvp.Value != "" ? @$"player.id != ""{kvp.Value}""" : null).Where(v => v is not null))})) {{
								for (var i:int = 0; i < lines.length; i++) {{
									var line:BeamLine = lines[i];
									line.color = generics.Color.HEXHue(beamColor,g.{clientDevHueColorIdentifier});
								}}

								for each(var emitter:Emitter in startEffect) {{
									emitter.changeHue(g.{clientDevHueColorIdentifier});
								}}

								for each(var emitter:Emitter in startEffect2) {{
									emitter.changeHue(g.{clientDevHueColorIdentifier});
								}}

								for each(var emitter:Emitter in endEffect) {{
									emitter.changeHue(g.{clientDevHueColorIdentifier});
								}}
							}}
						}}
					");

	                {
		                var indexToInsert = 0;
		                {
			                var anchorMatch = Regex.Match(scopeText, @"if\s*\(_loc\d+_\s*<\s*0\.3\)()");
			                if (!anchorMatch.Success)
				                return null;
			                var anchorScope = Util.FindNextScope(scopeText, anchorMatch.Index + anchorMatch.Length);
			                if (anchorScope is null)
				                return null;
			                indexToInsert = anchorScope.Index + anchorScope.Length;
		                }
		                
		                // Insert text in between the strings
		                var first = scopeText.Substring(0, indexToInsert);
		                var second = scopeText.Substring(indexToInsert);
		                scopeText = $"{first}{textToInsert}{second}";
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
            new PatchDescriptor("core.projectile.Projectile", [
	            new Patch("Add DETERMINED_COLOR, DETERMINED_HUE variables", (ctx) =>
	            {
		            if (!ctx.GetGlobalContext().GetIdentifier<string>("DETERMINED_COLOR", out var determinedColorIdentifier))
			            return null;
		            if (!ctx.GetGlobalContext().GetIdentifier<string>("DETERMINED_HUE", out var determinedHueIdentifier))
			            return null;
		            
		            var insertingText = Util.FlattenString($@"
						public var {determinedColorIdentifier}:Number = NaN;
						public var {determinedHueIdentifier}:Number = NaN;
					");
		            
		            var flatScript = Util.FlattenString(ctx.ScriptText);
		            {
			            var match = Regex.Match(flatScript, @"public class Projectile extends GameObject{");
			            if (match.Success)
			            {
				            var position = match.Index + match.Length;
				            var firstPart = flatScript.Substring(0, position);
				            var lastPart = flatScript.Substring(position);
				            flatScript = $"{firstPart}{insertingText}{lastPart}";
			            }
		            }
                    
		            return new PatchResult(flatScript);
	            }),
	            new Patch("Add [client_dev] chroma to projectiles", (ctx) =>
	            {
	                if (!ctx.GetGlobalContext().GetIdentifier<string>("DETERMINED_COLOR", out var determinedColorIdentifier))
		                return null;
	                if (!ctx.GetGlobalContext().GetIdentifier<string>("DETERMINED_HUE", out var determinedHueIdentifier))
		                return null;

		            var flatScript = Util.FlattenString(ctx.ScriptText);
	                {
		                var functionDefinitionMatch = Regex.Match(flatScript, @"override\s+public\s+function\s+update\s*\(.*?\)\s*:\s*\w+");
		                if (!functionDefinitionMatch.Success)
			                return null;
		                
		                var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
		                if (scopeInfo is null)
			                return null;

		                var scopeText = scopeInfo.ScopeText;
		                var updateTextToInsert = Util.FlattenString($@"
							if(!isNaN({determinedColorIdentifier}) && !isNaN({determinedHueIdentifier})) {{
								movieClip.color = {determinedColorIdentifier};
								for each(var e:Emitter in thrustEmitters) {{
									e.changeHue({determinedHueIdentifier});
								}}
							}}
						");

		                {
			                var indexToInsert = 0;
			                {
				                var anchorMatch = Regex.Match(scopeText, @"if\s*\(alive\)()");
				                if (!anchorMatch.Success)
					                return null;
				                var anchorScope = Util.FindNextScope(scopeText, anchorMatch.Index + anchorMatch.Length);
				                if (anchorScope is null)
					                return null;
				                indexToInsert = anchorScope.Index + 1;
			                }
			                
			                // Insert text in between the strings
			                var first = scopeText.Substring(0, indexToInsert);
			                var second = scopeText.Substring(indexToInsert);
			                scopeText = $"{first}{updateTextToInsert}{second}";
		                }
		                
		                {
			                // Split original script and re-insert block of function
			                var first = flatScript.Substring(0, scopeInfo.Index);
			                var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
			                flatScript = $"{first}{scopeText}{last}";
		                }
	                }
	                
	                {
		                var functionDefinitionMatch = Regex.Match(flatScript, @"public\s+function\s+explode\s*\(.*?\)\s*:\s*\w+");
		                if (!functionDefinitionMatch.Success)
			                return null;
		                
		                var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
		                if (scopeInfo is null)
			                return null;

		                var scopeText = scopeInfo.ScopeText;
		                

		                {
			                var indexToInsert = 0;
			                var localIndexString = "0";
			                {
				                var anchorMatch = Regex.Match(scopeText, @"_loc(\d+)_\s*=\s*EmitterFactory\.create\(explosionEffect,g,pos\.x,pos\.y,param\d+,true\);()if\s*\(param\d+\)");
				                if (!anchorMatch.Success)
					                return null;
				                localIndexString = anchorMatch.Groups[1].Value;
				                indexToInsert = anchorMatch.Groups[2].Index + anchorMatch.Groups[2].Length;
			                }
			                
			                var explodeTextToInsert = Util.FlattenString($@"
								if (!isNaN({determinedColorIdentifier}) && !isNaN({determinedHueIdentifier})) {{
									for each(var e:Emitter in _loc{localIndexString}_) {{
										e.changeHue({determinedHueIdentifier});
									}}
								}}
							");
			                
			                // Insert text in between the strings
			                var first = scopeText.Substring(0, indexToInsert);
			                var second = scopeText.Substring(indexToInsert);
			                scopeText = $"{first}{explodeTextToInsert}{second}";
		                }
		                
		                {
			                // Split original script and re-insert block of function
			                var first = flatScript.Substring(0, scopeInfo.Index);
			                var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
			                flatScript = $"{first}{scopeText}{last}";
		                }
	                }
	                
	                return new PatchResult(flatScript);
	            }),
	            new Patch("Reset DETERMINED_COLOR, DETERMINED_HUE variables on reset function", (ctx) =>
	            {
	                if (!ctx.GetGlobalContext().GetIdentifier<string>("DETERMINED_COLOR", out var determinedColorIdentifier))
		                return null;
	                if (!ctx.GetGlobalContext().GetIdentifier<string>("DETERMINED_HUE", out var determinedHueIdentifier))
		                return null;

		            var flatScript = Util.FlattenString(ctx.ScriptText);
		            
		            var functionDefinitionMatch = Regex.Match(flatScript, @"override\s+public\s+function\s+reset\s*\(.*?\)\s*:\s*\w+");
		            if (!functionDefinitionMatch.Success)
			            return null;
		                
		            var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
		            if (scopeInfo is null)
			            return null;

		            var scopeText = scopeInfo.ScopeText;
		            var textToInsert = Util.FlattenString($@"
						{determinedColorIdentifier} = NaN;
						{determinedHueIdentifier} = NaN;
					");
		            
		            {
			            // Insert text in between the strings
			            var first = "{";
			            var second = scopeText.Substring(1);
			            scopeText = $"{first}{textToInsert}{second}";
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
            new PatchDescriptor("core.projectile.ProjectileFactory", [
	            new Patch("Set projectiles DETERMINED_COLOR, DETERMINED_HUE variables", (ctx) =>
	            {
		            if (!ctx.GetGlobalContext().GetIdentifier<Dictionary<string, string>>("DEVELOPERS", out var developers))
		                return null;
	                if (!ctx.GetGlobalContext().GetIdentifier<string>("DETERMINED_COLOR", out var determinedColorIdentifier))
		                return null;
	                if (!ctx.GetGlobalContext().GetIdentifier<string>("DETERMINED_HUE", out var determinedHueIdentifier))
		                return null;
	                if (!ctx.GetGlobalContext().GetIdentifier<string>("CLIENT_DEV_HUE_COLOR", out var clientDevHueColorIdentifier))
		                return null;

		            var flatScript = Util.FlattenString(ctx.ScriptText);
	                {
		                var functionDefinitionMatch = Regex.Match(flatScript, @"public\s+static\s+function\s+create\s*\(.*?\)\s*:\s*\w+");
		                if (!functionDefinitionMatch.Success)
			                return null;
		                
		                var scopeInfo = Util.FindNextScope(flatScript, functionDefinitionMatch.Index + functionDefinitionMatch.Length);
		                if (scopeInfo is null)
			                return null;

		                var scopeText = scopeInfo.ScopeText;
		                

		                {
			                var indexToInsert = 0;
			                var projectileDbObjLocalIndex = "0";
			                var projectileObjLocalIndex = "0";
			                {
				                var anchorMatch = Regex.Match(scopeText, @"_loc(\d+)_\.blendMode\s*=\s*_loc(\d+)_\.blendMode;()if\s*\(_loc12_\.hasOwnProperty\(""aiAlwaysExplode""\)\)");
				                if (!anchorMatch.Success)
					                return null;
				                projectileObjLocalIndex = anchorMatch.Groups[1].Value;
				                projectileDbObjLocalIndex = anchorMatch.Groups[2].Value;
				                var anchorScope = Util.FindNextScope(scopeText, anchorMatch.Index + anchorMatch.Length);
				                if (anchorScope is null)
					                return null;
				                indexToInsert = anchorMatch.Groups[3].Index + anchorMatch.Groups[3].Length;
			                }
			                
			                var textToInsert = Util.FlattenString($@"
								_loc{projectileObjLocalIndex}_.{determinedHueIdentifier} = NaN;
								_loc{projectileObjLocalIndex}_.{determinedColorIdentifier} = NaN;
								_loc{projectileObjLocalIndex}_.movieClip.color = 0xffffff;
								if(param3 is core.ship.PlayerShip) {{
									var player:core.player.Player = (param3 as core.ship.PlayerShip).player;
									if (!({string.Join(" && ", developers!.Select(kvp => kvp.Value != "" ? @$"player.id != ""{kvp.Value}""" : null).Where(v => v is not null))})) {{
										_loc{projectileObjLocalIndex}_.{determinedHueIdentifier} = param2.{clientDevHueColorIdentifier};
										_loc{projectileObjLocalIndex}_.{determinedColorIdentifier} = generics.Color.HEXHue(0xff00,_loc{projectileObjLocalIndex}_.{determinedHueIdentifier});
										if (""ribbonColor"" in _loc{projectileDbObjLocalIndex}_) {{
											_loc{projectileDbObjLocalIndex}_.ribbonColor = _loc{projectileObjLocalIndex}_.{determinedColorIdentifier};
										}}
									}}
								}}
							");
			                
			                // Insert text in between the strings
			                var first = scopeText.Substring(0, indexToInsert);
			                var second = scopeText.Substring(indexToInsert);
			                scopeText = $"{first}{textToInsert}{second}";
		                }
		                
		                {
			                // Split original script and re-insert block of function
			                var first = flatScript.Substring(0, scopeInfo.Index);
			                var last = flatScript.Substring(scopeInfo.Index + scopeInfo.Length);
			                flatScript = $"{first}{scopeText}{last}";
		                }
	                }
	                
	                return new PatchResult(flatScript);
	            })
            ]),
            new PatchDescriptor("core.hud.components.techTree.TechTree", [
				new Patch("Reduce weapon upgrade animation time by 85%", (ctx) =>
				{
					var text = ctx.ScriptText;
					var matches = Regex.Matches(text, @"TweenMax\.from\(\s*([^,]*?)\s*,\s*([0-9.+\-eE]+)\s*,");
					for (var i = 0; i < matches.Count; i++)
					{
						var target = matches[i].Groups[1].Value;
						var time = (double.Parse(matches[i].Groups[2].Value) * 0.15f).ToString(CultureInfo.InvariantCulture);
						text = text.Replace(matches[i].Value, $@"TweenMax.from({target},{time},");
					}
					return new PatchResult(text);
				})
            ])
        ]);
    }

    private void OnExit()
    {
        Directory.Delete(TemporaryDirectory, true);
    }
}