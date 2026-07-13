using System.Text;

namespace Dg.ProviderOrder.Mokumentation;

using ArchitectureBricks;

public class MarkdownDocument
{
    public static string GetProductsMarkDown(
        IReadOnlyList<VerticalSlice> verticalSlices,
        string title)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {title}");
        var verticalSlicesByPrimaryPortProductFullName = verticalSlices.ToLookup(p => p.PrimaryPortProductFullName);
        foreach (var verticalSlicesOfSameProduct in verticalSlicesByPrimaryPortProductFullName)
        {
            builder.AppendLine($"## {verticalSlicesOfSameProduct.Key}");
            foreach (var verticalSlice in verticalSlicesOfSameProduct)
            {
                builder.AppendLine($"```mermaid");
                var flowchart = MermaidFlowChartFactory.CreateVerticalSliceMermaidFlowchart(verticalSlice);
                builder.Append(flowchart.Text);
                builder.AppendLine($"```");
            }
        }

        return builder.ToString();
    }
}