namespace AFPatcher.Patching;

public class PatchContext(GlobalPatchContext gCtx, string text, PatchDescriptor descriptor)
{
    public GlobalPatchContext GlobalPatchContext => gCtx;
    public string Text => text;
    public PatchDescriptor PatchDescriptor => descriptor;
}