namespace AFPatcher.Patching;

public class PatchFailedException(string reason) : Exception(reason);