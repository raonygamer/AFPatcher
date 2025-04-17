using Gtk;

namespace AFPatcher.Patching;

public class CyclicDependencyException : Exception
{
    public readonly DependencyAnalyzer Analyzer;
    public readonly List<List<string>> Nodes;

    public override string Message
    {
        get
        {
            var message = Nodes.Count == 0 ? "Cyclic dependencies found." : "Cyclic dependencies found:";
            if (Nodes.Count > 0)
            {
                foreach (var node in Nodes)
                {
                    message += "\n  At:";
                    foreach (var iNode in node)
                    {
                        message += $"\n    {(node.IndexOf(iNode) == 0 ? ".." : "->")} {iNode} {(node.IndexOf(iNode) == node.Count - 1 ? ".." : "->")}";
                    }
                }
            }
            return message;
        }
    }

    public CyclicDependencyException(DependencyAnalyzer analyzer, List<List<string>> nodes)
    {
        Analyzer = analyzer;
        Nodes = nodes;
    }
}