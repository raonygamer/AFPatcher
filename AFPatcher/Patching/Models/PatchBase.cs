using AFPatcher.Utility;

namespace AFPatcher.Patching;

public abstract class PatchBase
{
    public string Id { get; }
    public string Name { get; }
    public string[] Dependencies { get; }
    public int Priority { get; }

    protected PatchBase(string id, string name, string[] dependencies, int priority)
    {
        this.Id = id;
        this.Name = name;
        this.Dependencies = dependencies;
        this.Priority = priority;
        Log.SuccessLine($"Created instance of patch '{id}'.");
    }
    
    public abstract PatchResult Apply(PatchContext patchContext);
}