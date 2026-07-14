namespace Dg.ProviderOrder.Mockumentation.Viewer.Model;

// Lightweight POCOs mirroring the subset of Dg.ProviderOrder.Mokumentation's serialized
// VerticalSlice model that the viewer needs. Deserialized from the *.verticalslices.json
// output. Kept deliberately decoupled from the (heavy, Roslyn/MSBuild-based) analysis project.

public sealed class FullNameDto
{
    public string Value { get; set; } = string.Empty;
    public string[] Tokens { get; set; } = [];

    public string LastToken => Tokens.Length > 0 ? Tokens[^1] : Value;
}

public sealed class NamedSymbolDto
{
    public string Name { get; set; } = string.Empty;
    public FullNameDto FullName { get; set; } = new();

    /// <summary>Link back to the source that defines this symbol; null when it has no source (e.g. types from a referenced package).</summary>
    public SourceLocationDto? SourceLocation { get; set; }
}

/// <summary>A repo-relative source path + 1-based line range. Combined with the file's SourceRepositoryDto it yields a commit-pinned GitHub blob URL.</summary>
public sealed class SourceLocationDto
{
    public string Path { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int EndLine { get; set; }
}

public sealed class PrimaryAdapterDto
{
    public NamedSymbolDto ClassTypeSymbol { get; set; } = new();
    public NamedSymbolDto EntryMethod { get; set; } = new();

    /// <summary>0 = Web, 1 = Messaging, 2 = Other (matches PrimaryAdapterType enum).</summary>
    public int AdapterType { get; set; }

    /// <summary>The incoming payload type; null for web adapters (they carry no message payload).</summary>
    public NamedSymbolDto? PayloadType { get; set; }
}

public sealed class MethodSignatureDto
{
    public List<NamedSymbolDto> Parameters { get; set; } = [];
}

public sealed class ImplementationMethodDto
{
    public MethodSignatureDto Signature { get; set; } = new();
}

public sealed class PrimaryPortDto
{
    public FullNameDto FullName { get; set; } = new();

    /// <summary>0 = Query, 1 = Command (matches ApplicationSide enum).</summary>
    public int ApplicationSide { get; set; }

    /// <summary>0 = MediatrRequestHandler, 1 = MarinatorRequestHandler, 2 = MonolithApplicationService.</summary>
    public int PrimaryPortType { get; set; }

    /// <summary>The handler method. For MediatR/Marinator handlers its first parameter is the handled request/command.</summary>
    public ImplementationMethodDto ImplementationMethod { get; set; } = new();
}

public sealed class WebApiServiceClientCallDto
{
    public NamedSymbolDto Method { get; set; } = new();
}

public sealed class VerticalSliceDto
{
    public List<PrimaryAdapterDto> PrimaryAdapters { get; set; } = [];
    public PrimaryPortDto PrimaryPort { get; set; } = new();
    public List<NamedSymbolDto> NServiceBusPayloads { get; set; } = [];
    public List<NamedSymbolDto> KafkaPayloads { get; set; } = [];
    public List<WebApiServiceClientCallDto> WebApiServiceClientCalls { get; set; } = [];

    public string Id => PrimaryPort.FullName.Value;
}

/// <summary>The source repository/commit a *.verticalslices.json was extracted from, held once per file.</summary>
public sealed class SourceRepositoryDto
{
    public string RemoteBaseUrl { get; set; } = string.Empty;
    public string CommitSha { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;

    /// <summary>Composes the commit-pinned GitHub blob URL for a source location.</summary>
    public string BlobUrl(SourceLocationDto location) =>
        $"{RemoteBaseUrl}/blob/{CommitSha}/{location.Path}#L{location.StartLine}-L{location.EndLine}";
}

/// <summary>Top-level shape of a *.verticalslices.json file: the repository header plus the extracted slices.</summary>
public sealed class VerticalSliceExtractDto
{
    public SourceRepositoryDto Repository { get; set; } = new();
    public List<VerticalSliceDto> Slices { get; set; } = [];
}
