using AFPatcher.Utility;

namespace AFPatcher.Patching;

public abstract class PatchBase
{
    public string Id { get; }
    public string Name { get; }
    public string[] Dependencies { get; }

    protected PatchBase(string id, string name, string[] dependencies)
    {
        this.Id = id;
        this.Name = name;
        this.Dependencies = dependencies;
        Log.SuccessLine($"Instantiated patch '{id}'.");
    }
    
    public abstract PatchResult Apply(PatchContext patchContext);
}