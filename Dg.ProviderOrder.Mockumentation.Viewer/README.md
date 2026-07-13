# Dg.ProviderOrder.Mockumentation.Viewer

Interactive viewer for the vertical-slice model produced by **Dg.ProviderOrder.Mokumentation**.
Replaces the old Blazor + hand-written-canvas frontend.

Each vertical slice is drawn as an **ellipse** labelled with the last token of its primary-port
full name. Slices that produce a downstream event or web request are connected by an **arrow** to the
slice that consumes it. Input that arrives from outside any known slice is shown as a short
free-standing incoming arrow on the left of the node. **Arrow colour encodes the consuming
`PrimaryAdapterType`** (blue = Web request, amber = Messaging event, violet = Other).

## Stack

- **Host:** ASP.NET Core (`net9.0`) minimal API. Reads the `*.verticalslices.json` output, merges the
  monolith + module datasets (deduped by primary-port id, so cross-boundary events/requests connect),
  builds the edges + external-incoming arrows, and exposes `GET /api/graph`. It also serves the SPA.
- **Client:** React + TypeScript + Vite using [@xyflow/react](https://reactflow.dev) for the canvas —
  left-drag to pan, mouse wheel to zoom, drag a node to move it.

The domain rules (`FindProviderOrderProduct`, application side, product tokens) are ported as small
pure helpers in `Services/ProviderOrderDomain.cs`, so the viewer does not depend on the heavy
Roslyn/MSBuild analysis project.

## Running

```bash
dotnet run
```

`dotnet build`/`dotnet run` automatically builds the React client (via the `BuildClientApp` MSBuild
target — needs Node.js + npm on PATH) into `wwwroot`, then ASP.NET Core serves it. Open the URL
printed in the console (default <http://localhost:5240>).

- Skip the client build (e.g. `wwwroot` already built): `dotnet run -p:BuildClientApp=false`
- Front-end live-reload during development: run `npm run dev` in `ClientApp` (proxies `/api` to the
  running .NET host) alongside `dotnet run`.

## Configuration

Source files are configured in `appsettings.json` (paths are relative to the project directory or
absolute):

```json
"VerticalSlices": {
  "Files": [
    "../Dg.ProviderOrder.Mokumentation/monolith.verticalslices.json",
    "../Dg.ProviderOrder.Mokumentation/module.verticalslices.json"
  ]
}
```

`GET /api/graph?refresh=true` re-reads the files without restarting.

## Layout

Order (= longest dependency path from an entry-point slice) runs left→right in bands: entry points on
the left, their direct consumers next, and so on. A dense band wraps into a compact grid rather than
one endless column. Within a band, **Command** slices come before **Query** ones and slices of the
same **product** are kept together. The **Reset layout** button recomputes this for whatever is
currently visible; the left checkbox tree (ApplicationSide → Product) toggles slices on and off.

## Note on the source data

`module.verticalslices.json` currently ships with two stray leading bytes (`/c`) before the JSON
array — likely a shell-redirect mishap when it was generated. The loader tolerates this (it skips any
junk before the first `[`/`{` and logs a warning), but regenerating the file cleanly is recommended.
