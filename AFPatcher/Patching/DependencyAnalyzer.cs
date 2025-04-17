namespace AFPatcher.Patching;

public class DependencyAnalyzer
{
    public Dictionary<string, List<string>> Graph { get; private set; }= [];
    public Dictionary<string, int> Priorities { get; private set; } = [];
    
    public DependencyAnalyzer(Dictionary<string, (PatchDescriptor Descriptor, PatchBase Patch)> patches)
    {
        foreach (var (id, (desc, patch)) in patches)
        {
            foreach (var dependency in patch.Dependencies)
            {
                AddDependency(id, dependency);
            }
            SetPriority(id, patch.Priority);
        }
    }

    public void AddDependency(string item, string dependsOn)
    {
        if (!Graph.ContainsKey(item))
            Graph[item] = [];
        if (!Graph.ContainsKey(dependsOn))
            Graph[dependsOn] = [];
        Graph[item].Add(dependsOn);
    }

    public void SetPriority(string item, int priority)
    {
        Priorities[item] = priority;
        if (!Graph.ContainsKey(item))
            Graph[item] = [];
    }

    public void CheckCycle()
    {
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();
        var path = new List<string>();
        var allCycleNodes = new List<List<string>>();
        
        bool Dfs(string node, string? last = null)
        {
            if (recursionStack.Contains(node))
            {
                var cycleStartIndex = path.IndexOf(node);
                var cycleNodes = path.Skip(cycleStartIndex).ToList();
                allCycleNodes.Add(cycleNodes);
            }

            if (!visited.Add(node))
                return false;

            recursionStack.Add(node);
            path.Add(node);

            foreach (var neighbor in Graph[node])
            {
                if (Dfs(neighbor, node))
                {
                    return true;
                }
            }

            recursionStack.Remove(node);
            path.RemoveAt(path.Count - 1);
            return false;
        }

        foreach (var node in Graph.Keys)
        {
            Dfs(node);
        }
        
        if (allCycleNodes.Count != 0)
            throw new CyclicDependencyException(this, allCycleNodes);
    }
    
    public List<string> TopologicalSort()
    {
        CheckCycle();
        var inDegree = new Dictionary<string, int>();
        foreach (var node in Graph.Keys)
        {
            inDegree[node] = 0;
        }

        foreach (var node in Graph.Keys)
        {
            foreach (var neighbor in Graph[node])
            {
                inDegree[neighbor]++;
            }
        }
        
        var zeroInDegree = new SortedSet<string>(Comparer<string>.Create((a, b) =>
        {
            int priorityComparison = Priorities[a].CompareTo(Priorities[b]);
            return priorityComparison != 0 ? priorityComparison : string.Compare(a, b, StringComparison.Ordinal);
        }));

        foreach (var node in inDegree.Keys.Where(node => inDegree[node] == 0))
        {
            zeroInDegree.Add(node);
        }

        var sortedOrder = new List<string>();

        while (zeroInDegree.Count > 0)
        {
            var node = zeroInDegree.First();
            zeroInDegree.Remove(node);
            sortedOrder.Add(node);

            foreach (var neighbor in Graph[node])
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                {
                    zeroInDegree.Add(neighbor);
                }
            }
        }
        
        sortedOrder.Reverse();
        return sortedOrder;
    }
}