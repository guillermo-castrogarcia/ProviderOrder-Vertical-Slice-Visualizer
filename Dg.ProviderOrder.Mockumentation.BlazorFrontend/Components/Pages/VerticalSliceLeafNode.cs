using Dg.ProviderOrder.Mokumentation;
using Dg.ProviderOrder.Mokumentation.ArchitectureBricks;

namespace Dg.ProviderOrder.Mockumentation.BlazorFrontend;

public sealed class VerticalSliceLeafNode(VerticalSliceGroupNode parent, VerticalSlice verticalSlice)
{
    public VerticalSliceGroupNode Parent { get; } = parent;
    public VerticalSlice VerticalSlice { get; } = verticalSlice;

    private bool selected = true;
    public bool Selected
    {
        get => selected;
        set
        {
            selected = value;
            SelectedChanged?.Invoke();
        }
    }

    public event Action SelectedChanged;
    public string TreeViewNodeText => VerticalSlice.PrimaryPort.ImplementationClass.Name;
}