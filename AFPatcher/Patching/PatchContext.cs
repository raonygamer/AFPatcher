namespace AFPatcher.Patching;

public class PatchContext(GlobalPatchContext gCtx, string text, IEnumerable<PatchDescriptor> descriptors, PatchDescriptor descriptor)
{
    public GlobalPatchContext GlobalPatchContext => gCtx;
    public string Text => text;
    public IEnumerable<PatchDescriptor> PatchDescriptors => descriptors;
    public PatchDescriptor PatchDescriptor => descriptor;
}