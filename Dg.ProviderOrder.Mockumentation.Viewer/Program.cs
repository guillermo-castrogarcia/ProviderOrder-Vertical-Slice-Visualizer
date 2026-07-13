using Dg.ProviderOrder.Mockumentation.Viewer;
using Dg.ProviderOrder.Mockumentation.Viewer.Model;
using Dg.ProviderOrder.Mockumentation.Viewer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<GraphBuilder>();
builder.Services.AddSingleton<GraphService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// The processed graph consumed by the SPA. Pass ?refresh=true to re-read the source JSON files.
app.MapGet("/api/graph", (GraphService graph, bool refresh = false) => Results.Json(graph.Get(refresh)));

app.MapFallbackToFile("index.html");

app.Run();

namespace Dg.ProviderOrder.Mockumentation.Viewer
{
    /// <summary>Resolves the configured source files and caches the built graph.</summary>
    public sealed class GraphService(GraphBuilder builder, IConfiguration configuration, IHostEnvironment env)
    {
        private readonly object gate = new();
        private GraphDto? cached;

        public GraphDto Get(bool refresh)
        {
            if (!refresh && cached is not null)
            {
                return cached;
            }
            lock (gate)
            {
                if (refresh || cached is null)
                {
                    cached = builder.Build(ResolveFiles());
                }
                return cached;
            }
        }

        private IEnumerable<string> ResolveFiles()
        {
            var configured = configuration.GetSection("VerticalSlices:Files").Get<string[]>();
            var files = configured is { Length: > 0 }
                ? configured
                :
                [
                    "../Dg.ProviderOrder.Mokumentation/monolith.verticalslices.json",
                    "../Dg.ProviderOrder.Mokumentation/module.verticalslices.json",
                ];

            return files.Select(f => Path.IsPathRooted(f) ? f : Path.GetFullPath(Path.Combine(env.ContentRootPath, f)));
        }
    }
}
