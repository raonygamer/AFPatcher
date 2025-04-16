namespace AFPatcher.Patching;

public class PatchAttribute(string id, string name, string[] dependencies, int priority = 0) : Attribute
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public string[] Dependencies { get; } = dependencies;
    public int Priority { get; } = priority;
}