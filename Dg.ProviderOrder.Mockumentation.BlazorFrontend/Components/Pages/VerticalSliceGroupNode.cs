using Dg.ProviderOrder.Mokumentation;
using Dg.ProviderOrder.Mokumentation.ArchitectureBricks;

namespace Dg.ProviderOrder.Mockumentation.BlazorFrontend;

public sealed class VerticalSliceGroupNode(string name, int depth)
{
    public string Name { get; } = name;
    public int Depth { get; } = depth;
    private readonly HashSet<VerticalSliceLeafNode> verticalSliceLeafNodes = new ();
    private readonly HashSet<VerticalSliceGroupNode> verticalSliceGroupNodes = new ();
    public IReadOnlySet<VerticalSliceLeafNode> VerticalSliceLeafNodes => verticalSliceLeafNodes;
    public IReadOnlySet<VerticalSliceGroupNode> VerticalSliceGroupNodes => verticalSliceGroupNodes;
    public static VerticalSliceGroupNode CreateRoot() => new(string.Empty, 0);
    public bool IsRoot => string.IsNullOrEmpty(name);

    public string TreeViewNodeText => IsRoot ? "Provider Order" : Name.ToString();

    private bool? selected = true;

    public bool? Selected
    {
        get => selected;
        set
        {
            selected = value;
            OnSelectedChanged();
            SelectedChanged?.Invoke();
        }
    }

    public event Action SelectedChanged;
    public event Action SelectionChanged;

    public IEnumerable<VerticalSlice> GetSelectedVerticalSlicesRecursive()
    {
        var results = verticalSliceLeafNodes.Where(e => e.Selected).Select(e => e.VerticalSlice)
            .Concat(VerticalSliceGroupNodes.SelectMany(e => e.GetSelectedVerticalSlicesRecursive()));

        return results;
    }

    public static VerticalSliceGroupNode BuildTree(IReadOnlySet<VerticalSlice> verticalSlices)
    {
        var root = CreateRoot();
        var queries = new VerticalSliceGroupNode("Queries", 1);
        var commands = new VerticalSliceGroupNode("Commands", 1);
        root.verticalSliceGroupNodes.Add(commands);
        root.verticalSliceGroupNodes.Add(queries);
        BuildTreeRecursively(queries, verticalSlices.Where(e => e.PrimaryPort.ApplicationSide == ApplicationSide.Query));
        BuildTreeRecursively(commands, verticalSlices.Where(e => e.PrimaryPort.ApplicationSide == ApplicationSide.Command));
        queries.SelectedChanged += root.OnChildSelectedChanged;
        queries.SelectionChanged += () => root.SelectionChanged?.Invoke();
        commands.SelectedChanged += root.OnChildSelectedChanged;
        commands.SelectionChanged += () => root.SelectionChanged?.Invoke();
        return root;
    }

    private static void BuildTreeRecursively(VerticalSliceGroupNode currentParent,
        IEnumerable<VerticalSlice> ungroupedChildren)
    {
        var groups = ungroupedChildren.GroupBy(e => e.FindProviderOrderProduct());

        foreach (var group in groups.OrderBy(e => e.Key.GetName()))
        {
            var verticalSliceGroup = new VerticalSliceGroupNode(name: group.Key.GetName(), currentParent.Depth + 1);
            verticalSliceGroup.SelectedChanged += currentParent.OnChildSelectedChanged;
            verticalSliceGroup.SelectionChanged += () => currentParent.SelectionChanged?.Invoke();
            currentParent.verticalSliceGroupNodes.Add(verticalSliceGroup);
            foreach(var verticalSlice in group)
            {
                var child = new VerticalSliceLeafNode(verticalSliceGroup, verticalSlice);
                child.SelectedChanged += verticalSliceGroup.OnChildSelectedChanged;
                verticalSliceGroup.verticalSliceLeafNodes.Add(child);
            }
        }
    }

    private bool ignoreChildSelectedChanges = false;
    private void OnChildSelectedChanged()
    {
        if (ignoreChildSelectedChanges)
        {
            return;
        }
        var selectedState = CalculateSelectedState();
        if (selectedState != selected)
        {
            ignoreSelectedChanges = true;
            Selected = selectedState;
            ignoreSelectedChanges = false;
        }
        SelectionChanged?.Invoke();
    }

    
    private bool ignoreSelectedChanges = false;
    private void OnSelectedChanged()
    {
        if (selected is null || ignoreSelectedChanges)
        {
            return;
        }

        ignoreChildSelectedChanges = true;
        foreach (var child in verticalSliceLeafNodes)
        {
            child.Selected = selected.Value;
        }
        foreach (var child in verticalSliceGroupNodes)
        {
            child.Selected = selected.Value;
        }
        ignoreChildSelectedChanges = false;
    }

    private bool? CalculateSelectedState()
    {
        if (verticalSliceGroupNodes.Any(e => e.Selected == null))
        {
            return null;
        }

        var selectedChildrenCount = verticalSliceGroupNodes.Count(e => e.Selected == true) +
                                    verticalSliceLeafNodes.Count(e => e.Selected);

        var notSelectedChildrenCount = verticalSliceGroupNodes.Count(e => e.Selected == false) +
                                       verticalSliceLeafNodes.Count(e => !e.Selected);

        if (selectedChildrenCount == 0)
        {
            return false;
        }

        if (notSelectedChildrenCount == 0)
        {
            return true;
        }

        return null;
    }
}