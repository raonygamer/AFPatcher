using System.Reflection;

namespace AFPatcher.Patching;

public class PatchDescriptor(string className, IEnumerable<PatchBase> patches)
{
    public string ClassName { get; set; } = className;
    public IEnumerable<PatchBase> Patches { get; set; } = [..GeneratePatchInstancesFor(Assembly.GetExecutingAssembly(), className), ..patches];

    private static IEnumerable<PatchBase> GeneratePatchInstancesFor(Assembly assembly, string className)
    {
        return assembly.GetTypes().Select(t =>
        {
            if (!t.IsAssignableTo(typeof(PatchBase)) || t.IsAbstract || t.GetCustomAttribute<PatchAttribute>() is null || t.Namespace?.EndsWith(className) == false)
                return null;
            var patchAttr = t.GetCustomAttribute<PatchAttribute>()!;
            return Activator.CreateInstance(t, [patchAttr.Id, patchAttr.Name, patchAttr.Dependencies, patchAttr.Priority]) as PatchBase;
        }).Where(i => i is not null).OrderBy(i => i!.Priority)!;
    }
}