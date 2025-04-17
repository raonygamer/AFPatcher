using AFPatcher.Patches;
using AFPatcher.Patching;
using AFPatcher.Utility;

namespace AFPatcher;

class PatcherMain
{
    #region Singleton
    private static PatcherMain? _instance;
    public static PatcherMain Instance => _instance ??= new PatcherMain();
    
    static void Main(string[] args)
    {
        Instance.Start();
#if DEBUG
        Console.ReadKey();        
#endif
        Thread.Sleep(2000);
    }
    #endregion

    private readonly string TemporaryDirectory;
    private readonly string DecompilationDirectory;
    
    private PatcherMain()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => OnExit();
        TemporaryDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString().Replace("-", ""));
        DecompilationDirectory = Path.Combine(TemporaryDirectory, "decompiled");
        Directory.CreateDirectory(DecompilationDirectory);
        Log.TraceLine("Creating temporary directory...");
        Log.TraceLine($"Created temporary directory: {TemporaryDirectory}");
    }
    
    private void Start()
    {
        // Create context and all the descriptors and patches
        var context = QoLAF.GetGlobalPatchContext();
        Log.TraceLine($"Created instance of {context.FlattenedPatches.Count} patches.");
        
        // Create the dependency analyzer
        var patchIdentifiers = new List<string>();
        var analyzer = new DependencyAnalyzer(context.FlattenedPatches);
        try
        {
            // Check for cyclic dependencies and sort topologically by dependency order and priority
            patchIdentifiers = analyzer.TopologicalSort();
            Log.TraceLine($"Sorted {context.FlattenedPatches.Count} patches by dependency and priority.");
            Log.TraceLine($"No cyclic dependencies detected.");
        }
        catch (CyclicDependencyException e)
        {
            // Print cyclic dependency traces
            Log.ErrorLine(e.Message);
        }
        
        // Print all the scripts to export
        Log.TraceLine($"Scripts to export:\n    {string.Join("\n    ", context.PatchDescriptors.Select(d => d.ClassName))}");
        Log.TraceLine();

        // Open .swf file to export scripts
        var fileToPatch = Utils.OpenFile([(["swf"], "Shockwave Files")], null);
        if (fileToPatch is null || !File.Exists(fileToPatch)) 
        {
            Log.ErrorLine("File to patch could not be found.");
            return;
        }
        
        // Export specified classes from the .swf
        if (!ExportClasses(fileToPatch, context.PatchDescriptors.Select(d => d.ClassName)))
        {
            Log.ErrorLine("Script exporting failed.");
            return;
        }
        
        // Start patching the exported files
        context.StartPatching(patchIdentifiers, DecompilationDirectory);
        if (context.ChangedFiles.Count == 0)
        {
            Log.ErrorLine("No changed scripts found.");
            return;
        }
        
        // Replace modified files
        Log.TraceLine("Replacing scripts...");
        if (!ReplaceClasses(fileToPatch, context.ChangedFiles.Values))
        {
            Log.ErrorLine("Script replacement failed.");
            return;
        }

        // Copy new .swf files
        var newFile = Path.Combine(TemporaryDirectory, "recompiled", Path.GetFileName(fileToPatch));
        var copyFile = Utils.SaveFile([(["swf"], "Shockwave Files")], null);
        if (copyFile is null || !File.Exists(copyFile))
        {
            return;
        }
        
        File.Copy(newFile, copyFile, true);
    }
    
    private bool ExportClasses(string swfSource, IEnumerable<string> classes)
    { 
        var p = Utils.StartFlashDecompiler($"-selectclass {string.Join(",", classes)} -export script", DecompilationDirectory, swfSource);
        if (p is null)
            return false;
        p.WaitForExit();
        if (p.ExitCode != 0)
        {
            Log.ErrorLine($"Failed to export scripts.");
            Log.ErrorLine(p.StandardOutput.ReadToEnd());
        }
        return p.ExitCode == 0;
    }

    private bool ReplaceClasses(string swfSource, IEnumerable<string> changedFiles)
    {
        Directory.CreateDirectory(Path.Combine(TemporaryDirectory, "recompiled"));
        var p = Utils.StartFlashDecompiler(string.Join(" ", ["-replace", swfSource, Path.Combine(TemporaryDirectory, "recompiled", Path.GetFileName(swfSource)), ..changedFiles]));
        if (p is null)
            return false;
        p.WaitForExit();
        if (p.ExitCode != 0)
        {
            Log.ErrorLine($"Failed to replace scripts.");
            Log.ErrorLine(p.StandardOutput.ReadToEnd());
        }
        return p.ExitCode == 0;
    }

    private void OnExit()
    {
        Directory.Delete(TemporaryDirectory, true);
    }
}