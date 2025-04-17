using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AFPatcher.Patches;
using AFPatcher.Patching;
using AFPatcher.Utility;
using Newtonsoft.Json;
using SharpFileDialog;

class PatcherMain
{
    #region Singleton
    private static PatcherMain? _instance;
    public static PatcherMain Instance => _instance ??= new PatcherMain();
    
    static void Main(string[] args)
    {
        Instance.Start().GetAwaiter().GetResult();
#if DEBUG
        Console.ReadKey();        
#endif
        Thread.Sleep(2000);
    }
    #endregion

    public readonly string TemporaryDirectory;
    public readonly string DecompilationDirectory;
    
    public PatcherMain()
    {
        AppDomain.CurrentDomain.ProcessExit += (sender, args) => OnExit();
        TemporaryDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString().Replace("-", ""));
        DecompilationDirectory = Path.Combine(TemporaryDirectory, "decompiled");
        
        Directory.CreateDirectory(DecompilationDirectory);
        Log.TraceLine("Creating temporary directory...");
        Log.TraceLine($"Created temporary directory: {TemporaryDirectory}");
    }
    
    private async Task Start()
    {
        Log.TraceLine();
        var globalContext = QoLAF.GetGlobalPatchContext();
        var patchDescriptors = QoLAF.GetPatchDescriptors();
        var flattenedPatches = patchDescriptors
            .SelectMany(d => d.Patches.Select(p => (Descriptor: d, Patch: p)))
            .ToDictionary(t => t.Patch.Id);
        var analyzer = new DependencyAnalyzer(flattenedPatches);
        var sortedPatchIds = new List<string>();
        Log.TraceLine($"Created instance of {flattenedPatches.Count} patches.");
        try
        {
            sortedPatchIds = analyzer.TopologicalSort();
            Log.TraceLine($"Sorted {flattenedPatches.Count} patches by dependency and priority.");
            Log.TraceLine($"No cyclic dependencies detected.");
        }
        catch (CyclicDependencyException e)
        {
            Log.ErrorLine(e.Message);
        }
        Log.TraceLine();
        
        if (!NativeFileDialog.OpenDialog(
                [new NativeFileDialog.Filter { Extensions = ["swf"], Name = "Shockwave Files" }], null,
                out var swfFile) ||
            !File.Exists(swfFile))
        {
            Log.ErrorLine("No swf file found.");
            return;
        }
        
        Log.TraceLine("Decompiling game...");
        if (!DecompileClasses(swfFile, patchDescriptors.Select(d => d.ClassName)))
        {
            Log.ErrorLine("Game decompilation failed.");
            return;
        }
        
        var appliedPatches = new List<string>();
        var failedPatches = new List<string>();
        var changedFiles = new List<string>();
        foreach (var id in sortedPatchIds)
        {
            try
            {
                var (desc, patch) = flattenedPatches[id];
                PatchScript(globalContext, desc, patch, appliedPatches, failedPatches, changedFiles);
                Log.SuccessLine($"Patch '{id}' applied successfully on '{desc.ClassName}'.");
            }
            catch (Exception e)
            {
                Log.ErrorLine($"Failed to apply patch '{id}':\n    {e.Message}");
            }
        }
        
        Log.TraceLine();
        
#if DEBUG
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd",
            Arguments = $"/c start {DecompilationDirectory}",
            UseShellExecute = true
        });
#endif

        if (changedFiles.Count == 0)
        {
            Log.ErrorLine("No changed files found.");
            return;
        }
        
        Log.TraceLine("Recompiling game...");
        if (!RecompileClasses(swfFile, changedFiles))
        {
            Log.ErrorLine("Game recompilation failed.");
            return;
        }

        var newFile = Path.Combine(TemporaryDirectory, "recompiled", Path.GetFileName(swfFile));
        if (!NativeFileDialog.SaveDialog(
                [new NativeFileDialog.Filter { Extensions = ["swf"], Name = "Shockwave Files" }], null,
                out var saveSwfFile))
        {
            Log.ErrorLine("Invalid save swf file.");
            return;
        }
        
        File.Copy(newFile, saveSwfFile, true);
    }

    private void PatchScript(GlobalPatchContext gCtx, PatchDescriptor descriptor, PatchBase patch, List<string> appliedPatches, List<string> failedPatches, List<string> changedFiles)
    {
        if (appliedPatches.Contains(patch.Id))
            return;
        foreach (var dependencyId in patch.Dependencies)
        {
            if (failedPatches.Contains(dependencyId))
            {
                throw new PatchFailedException($"The patch dependency '{dependencyId}' failed.");
            }
        }
        
        var scriptFile = Path.Combine(DecompilationDirectory, $"scripts\\{descriptor.ClassName.Replace('.', '\\')}.as");
        var scriptContent = File.ReadAllText(scriptFile).Flatten();
        
        try
        {
            var result = patch.Apply(new PatchContext(gCtx, scriptContent, descriptor));
            appliedPatches.Add(patch.Id);
            File.WriteAllText(scriptFile, result.Text);
            if (!changedFiles.Contains($"{descriptor.ClassName} {scriptFile}"))
                changedFiles.Add($"{descriptor.ClassName} {scriptFile}");
        }
        catch
        {
            failedPatches.Add(patch.Id);
            throw;
        }
    }
    
    private bool DecompileClasses(string swfSource, IEnumerable<string> classes)
    { 
        var p = Utils.StartFlashDecompiler($"-selectclass {string.Join(",", classes)} -export script", DecompilationDirectory, swfSource);
        if (p is null)
            return false;
        p.WaitForExit();
        if (p.ExitCode != 0)
        {
            Log.ErrorLine($"Failed to decompile scripts.");
            Log.ErrorLine(p.StandardOutput.ReadToEnd());
        }
        return p.ExitCode == 0;
    }

    private bool RecompileClasses(string swfSource, IEnumerable<string> changedFiles)
    {
        Directory.CreateDirectory(Path.Combine(TemporaryDirectory, "recompiled"));
        var p = Utils.StartFlashDecompiler(string.Join(" ", ["-replace", swfSource, Path.Combine(TemporaryDirectory, "recompiled", Path.GetFileName(swfSource)), ..changedFiles]));
        if (p is null)
            return false;
        p.WaitForExit();
        if (p.ExitCode != 0)
        {
            Log.ErrorLine($"Failed to recompile scripts.");
            Log.ErrorLine(p.StandardOutput.ReadToEnd());
        }
        return p.ExitCode == 0;
    }

    private void OnExit()
    {
        Directory.Delete(TemporaryDirectory, true);
    }
}