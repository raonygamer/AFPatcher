using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AFPatcher.Patches;
using AFPatcher.Patching;
using AFPatcher.Utility;
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
#if DEBUG
        Console.ReadKey();        
#endif
        Thread.Sleep(2000);
    }
    #endregion

    public readonly string TemporaryDirectory;
    public readonly string DecompilationDirectory;
    
    public AFPatcher()
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
        if (!NativeFileDialog.OpenDialog(
                [new NativeFileDialog.Filter { Extensions = ["swf"], Name = "Shockwave Files" }], null,
                out var swfFile) ||
            !File.Exists(swfFile))
        {
            Log.ErrorLine("No swf file found.");
            return;
        }

        Log.TraceLine();
        var globalContext = QoLAF.GetGlobalPatchContext();
        var patchDescriptors = QoLAF.GetPatchDescriptors();
        Log.TraceLine();
        
        Log.TraceLine($"Found {patchDescriptors.Sum(d => d.Patches.Count())} patches.");
        Log.TraceLine("Decompiling game...");
        if (!DecompileClasses(swfFile, patchDescriptors.Select(d => d.ClassName)))
        {
            Log.ErrorLine("Game decompilation failed.");
            return;
        }
        
        var appliedPatches = new List<string>();
        var changedFiles = new List<string>();
        var allPatches = (from patchDesc in patchDescriptors from patch in patchDesc.Patches select (patch, patchDesc, patch.Priority)).OrderByDescending(patch => patch.Priority);
        foreach (var patch in allPatches)
        {
            try
            {
                if (!PatchScript(globalContext, patchDescriptors, patch.patchDesc, patch.patch, appliedPatches, changedFiles))
                    continue;
                Log.SuccessLine($"Patch '{patch.patch.Id}' applied successfully to '{patch.patchDesc.ClassName}'.");
            }
            catch (PatchFailedException e)
            {
                Log.ErrorLine($"Failed to apply patch '{patch.patch.Id}' to '{patch.patchDesc.ClassName}':\n    {e.Message}");
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

    private bool PatchScript(GlobalPatchContext gCtx, IEnumerable<PatchDescriptor> descriptors, PatchDescriptor descriptor, PatchBase patchBase, List<string> appliedPatches, List<string> changedFiles)
    {
        if (appliedPatches.Contains(patchBase.Id))
            return false;
        
        var scriptFile = Path.Combine(DecompilationDirectory, $"scripts\\{descriptor.ClassName.Replace('.', '\\')}.as");
        var patchDescriptors = descriptors as PatchDescriptor[] ?? descriptors.ToArray();
        foreach (var dependency in patchBase.Dependencies)
        {
            if (appliedPatches.Contains(dependency))
                continue;
            
            var dependencyDesc =
                patchDescriptors.FirstOrDefault(d => d.Patches.FirstOrDefault(p => p.Id == dependency) != null);
            if (dependencyDesc is null)
                throw new PatchFailedException($"Patch dependency '{dependency}' not found.");
            var dependencyPatch = dependencyDesc.Patches.FirstOrDefault(p => p.Id == dependency);
            if (dependencyPatch is null)
                throw new PatchFailedException($"Patch dependency '{dependency}' not found.");
            
            try
            {
                PatchScript(gCtx, patchDescriptors, dependencyDesc, dependencyPatch, appliedPatches, changedFiles);
                Log.SuccessLine($"Patch '{dependencyPatch.Id}' applied successfully to '{dependencyDesc.ClassName}' as dependency for '{patchBase.Id}'.");
                appliedPatches.Add(dependency);
            }
            catch (PatchFailedException e)
            {
                throw new PatchFailedException($"Could not patch dependency '{dependency}':\n    {e.Message}");
            }
        }
        var scriptContent = File.ReadAllText(scriptFile).Flatten();
        var result = patchBase.Apply(new PatchContext(gCtx, scriptContent, patchDescriptors, descriptor));
        appliedPatches.Add(patchBase.Id);
        File.WriteAllText(scriptFile, result.Text);
        if (!changedFiles.Contains($"{descriptor.ClassName} {scriptFile}"))
            changedFiles.Add($"{descriptor.ClassName} {scriptFile}");
        return true;
    }
    
    private bool DecompileClasses(string swfSource, IEnumerable<string> classes)
    { 
        var p = Utils.StartFlashDecompiler($"-selectclass {string.Join(",", classes)} -export script", DecompilationDirectory, swfSource);
        if (p is null)
            return false;
        p.WaitForExit();
        return p.ExitCode == 0;
    }

    private bool RecompileClasses(string swfSource, IEnumerable<string> changedFiles)
    {
        Directory.CreateDirectory(Path.Combine(TemporaryDirectory, "recompiled"));
        var p = Utils.StartFlashDecompiler(string.Join(" ", ["-replace", swfSource, Path.Combine(TemporaryDirectory, "recompiled", Path.GetFileName(swfSource)), ..changedFiles]));
        if (p is null)
            return false;
        p.WaitForExit();
        return p.ExitCode == 0;
    }

    private void OnExit()
    {
        Directory.Delete(TemporaryDirectory, true);
    }
}