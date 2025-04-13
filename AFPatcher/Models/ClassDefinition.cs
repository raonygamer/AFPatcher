namespace AFPatcher.Models;

public class ClassDefinition(string accessModifier, string className, string? extends = null, string[]? implements = null)
{
    public string AccessModifier { get; set; } = accessModifier;
    public string ClassName { get; set; } = className;
    public string? Extends { get; set; } = extends;
    public string[]? Implements { get; set; } = implements;

    public string BuildString()
    {
        string str = $@"{AccessModifier} class {ClassName}";
        if (Extends is not null)
        {
            str += $@" extends {Extends}";
        }

        if (Implements is not null)
        {
            str += $@" implements {string.Join(", ", Implements)}";
        }
        
        return str;
    }
    
    public string BuildAnchor()
    {
        string str = $@"{AccessModifier}\s+class\s+{ClassName}";
        if (Extends is not null)
        {
            str += $@"\s+extends\s+{Extends}";
        }

        if (Implements is not null)
        {
            str += $@"\s+implements\s+{string.Join(@",\s*", Implements)}";
        }
        
        if (!str.EndsWith(@"\s*"))
            str += @"\s*";
        return str;
    }
}