using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AFPatcher.Models;

public class GlobalPatchContext(Dictionary<string, string> identifierNames)
{
    public Dictionary<string, string> IdentifierNames { get; set; } = identifierNames;

    public void AddIdentifier(string identifier, string identifierName)
    {
        if (IdentifierNames.ContainsKey(identifier))
        {
            Console.WriteLine("Identifier already found on GlobalContext Replacing it");
            IdentifierNames[identifier] = identifierName;
        }
        IdentifierNames.Add(identifier, identifierName);
    }

    public void RemoveIdentifier(string identifier)
    {
        if (!IdentifierNames.Remove(identifier, out _))
        {
            Console.WriteLine($"Identifier '{identifier}' not found on GlobalContext.");
        }
    }

    public bool GetIdentifier(string identifier, out string identifierName)
    {
        if (!IdentifierNames.TryGetValue(identifier, out identifierName!))
        {
            Console.WriteLine($"Identifier '{identifier}' not found on GlobalContext.");
            return false;
        }
        
        return true;
    }
}

public class GamePatches(GlobalPatchContext ctx, PatchDescriptor[] descriptors)
{
    public GlobalPatchContext GlobalContext { get; set; } = ctx;
    public PatchDescriptor[] PatchDescriptors { get; set; } = descriptors;
}

public class PatchDescriptor(string fullyQualifiedName, Patch[] patches)
{
    public string FullyQualifiedName { get; set; } = fullyQualifiedName;
    public Patch[] Patches { get; set; } = patches;
}

public class PatchContext(GamePatches patchObject, string scriptText)
{
    public GamePatches PatchObject { get; set; } = patchObject;
    public string ScriptText { get; set; } = scriptText;

    public GlobalPatchContext GetGlobalContext()
    {
        return PatchObject.GlobalContext;
    }
}

public class PatchResult(string scriptText)
{
    public string ScriptText { get; set; } = scriptText;
}

public class Patch(string name, Patch.PatchDelegate function)
{
    public string Name { get; set; } = name;
    public delegate PatchResult? PatchDelegate(PatchContext ctx);
    public PatchDelegate PatchFunction { get; set; } = function;
}