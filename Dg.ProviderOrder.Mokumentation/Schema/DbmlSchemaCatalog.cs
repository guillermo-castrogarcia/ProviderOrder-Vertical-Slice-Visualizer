namespace Dg.ProviderOrder.Mokumentation.Schema;

using System.Xml.Linq;
using Microsoft.CodeAnalysis;

// Authoritative (database -> schema -> table -> column) map, parsed from the checked-in Deblazer `dbml.xml` descriptors.
// Both analyzed solutions generate their persistence layer from these files (the EF Core module and the Deblazer
// monolith alike), so the dbml is the single source of truth for table/schema/database names and the exact column list
// of every entity — the generated C# entity/config lands in obj/ and is not in source control, so we cannot rely on it.
//
// Each dbml `<Database>` carries a `Namespace` attribute equal to the namespace of the entity classes it generates.
// That lets us map an entity *type symbol* back to its database unambiguously (the same short table name can exist in
// more than one database). Within a database, an entity is matched to its table by name: Deblazer entities are named
// exactly like the dbml `<Type Name>` / table short name (e.g. `Order`, `Address`); EF entities add a `DbModel` suffix
// (`OrderDbModel` -> table `Order`). Column == property name by default.
public sealed class DbmlSchemaCatalog
{
    public sealed record TableInfo(string Database, string Schema, string Table, IReadOnlySet<string> Columns)
    {
        public string FullName => $"{Database}.{Schema}.{Table}";
    }

    private sealed record DatabaseInfo(string DatabaseNamespace, IReadOnlyDictionary<string, TableInfo> TablesByKey);

    private static readonly XNamespace Dbml = "http://schemas.digitecgalaxus.ch/deblazer/dbml/2022";

    private readonly IReadOnlyList<DatabaseInfo> _databases;

    // Diagnostics: entity types / columns we saw referenced in code but could not map. Surfaced by ReportUnresolved so
    // gaps are visible without failing the run.
    private readonly HashSet<string> _unresolvedEntities = new();
    private readonly HashSet<string> _unresolvedColumns = new();

    private DbmlSchemaCatalog(IReadOnlyList<DatabaseInfo> databases)
    {
        _databases = databases;
        foreach (var db in databases)
        {
            foreach (var (key, table) in db.TablesByKey)
            {
                _allEntityKeys.Add(key);
                foreach (var column in table.Columns)
                {
                    _allColumnNames.Add(column);
                }
            }
        }

        // Also treat known value-object property names as candidate column names so the pre-filter does not discard
        // them before ResolveColumn gets a chance to map them to the real column.
        foreach (var renameKey in KnownColumnRenames.Keys)
        {
            _allColumnNames.Add(renameKey);
        }
    }

    public int DatabaseCount => _databases.Count;

    public int TableCount => _databases.Sum(d => d.TablesByKey.Values.Distinct().Count());

    // Cheap pre-filters so callers can skip the (expensive) semantic-model resolution for the overwhelming majority of
    // syntax nodes whose member/type name could not possibly be a mapped column or entity.
    public IReadOnlySet<string> AllColumnNames => _allColumnNames;

    public bool IsCandidateEntityTypeName(string typeName) =>
        CandidateKeys(typeName).Any(_allEntityKeys.Contains);

    private HashSet<string> _allColumnNames = new(StringComparer.Ordinal);
    private HashSet<string> _allEntityKeys = new(StringComparer.Ordinal);

    // Discovers every dbml.xml under the given repository roots and loads them into a single catalog.
    public static DbmlSchemaCatalog LoadFromRepositories(params string[] repositoryRoots)
    {
        var files = repositoryRoots
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "dbml.xml", SearchOption.AllDirectories))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return Load(files);
    }

    public static DbmlSchemaCatalog Load(IEnumerable<string> dbmlFilePaths)
    {
        var databases = new List<DatabaseInfo>();
        foreach (var path in dbmlFilePaths)
        {
            var db = TryLoadFile(path);
            if (db is not null)
            {
                databases.Add(db);
            }
        }

        return new DbmlSchemaCatalog(databases);
    }

    private static DatabaseInfo? TryLoadFile(string path)
    {
        XDocument doc;
        try
        {
            doc = XDocument.Load(path);
        }
        catch
        {
            return null;
        }

        var databaseElement = doc.Root;
        if (databaseElement is null || databaseElement.Name != Dbml + "Database")
        {
            return null;
        }

        var databaseIdentifier = (string?)databaseElement.Attribute("DatabaseIdentifier")
            ?? (string?)databaseElement.Attribute("SchemaName")
            ?? Path.GetFileName(Path.GetDirectoryName(path)) ?? "Unknown";
        var databaseNamespace = (string?)databaseElement.Attribute("Namespace") ?? string.Empty;

        var tablesByKey = new Dictionary<string, TableInfo>(StringComparer.Ordinal);
        foreach (var tableElement in databaseElement.Elements(Dbml + "Table"))
        {
            var qualifiedName = (string?)tableElement.Attribute("Name"); // e.g. "ProviderOrder.CancelRequestFollowupCheckConfiguration" or "dbo.Address"
            if (qualifiedName is null)
            {
                continue;
            }

            var lastDot = qualifiedName.LastIndexOf('.');
            var schema = lastDot >= 0 ? qualifiedName[..lastDot] : ((string?)databaseElement.Attribute("SchemaName") ?? string.Empty);
            var tableShortName = lastDot >= 0 ? qualifiedName[(lastDot + 1)..] : qualifiedName;

            var typeElement = tableElement.Element(Dbml + "Type");
            var typeName = (string?)typeElement?.Attribute("Name") ?? tableShortName;

            var columns = (typeElement ?? tableElement)
                .Elements(Dbml + "Column")
                .Select(c => (string?)c.Attribute("Name"))
                .Where(n => n is not null)
                .Select(n => n!)
                .ToHashSet(StringComparer.Ordinal);

            var tableInfo = new TableInfo(databaseIdentifier, schema, tableShortName, columns);

            // Register under both the dbml Type name (Deblazer entity class name) and the bare table short name; the
            // entity resolver also tries the EF `DbModel`-stripped form against these keys.
            tablesByKey[typeName] = tableInfo;
            tablesByKey[tableShortName] = tableInfo;
        }

        return new DatabaseInfo(databaseNamespace, tablesByKey);
    }

    // Maps an entity type symbol referenced in code to its table, or null if it is not a known persistence entity.
    public TableInfo? TryResolveTable(INamedTypeSymbol entityType)
    {
        var entityNamespace = entityType.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        var keys = CandidateKeys(entityType.Name);

        // Prefer the database whose generated-code namespace matches the entity's namespace (disambiguates identical
        // short names living in different databases).
        var namespaceScoped = _databases
            .Where(d => d.DatabaseNamespace.Length > 0
                && (entityNamespace == d.DatabaseNamespace || entityNamespace.StartsWith(d.DatabaseNamespace + ".", StringComparison.Ordinal)))
            .ToList();

        // Require the entity's namespace to match the database that generated it. This is deliberately strict: a plain
        // domain/DTO class that merely shares a name with a table (e.g. a domain `Order` vs the `Order` table) must not
        // be misread as database access. Real EF/Deblazer entities always live under their database's generated-code
        // namespace, so this keeps precision high at the cost of missing entities in unexpected namespaces (which then
        // simply do not appear, rather than appearing wrong).
        foreach (var db in namespaceScoped)
        {
            foreach (var key in keys)
            {
                if (db.TablesByKey.TryGetValue(key, out var table))
                {
                    return table;
                }
            }
        }

        return null;
    }

    // Name-based fallback for when the compiler could not bind the entity type — i.e. its symbol is a Roslyn
    // *Error* type because the generated entity definition is absent from the (design-time) compilation. This is
    // common here: the Deblazer/EF entity classes are produced by a code generator (Deblazer.ArtifactsGenerator)
    // whose output is not present when the solution is opened without a full build, so every reference to an
    // entity binds to an error type and TryResolveTable (which needs a real namespace) fails. We then match by the
    // entity's simple name, disambiguating same-named tables in different databases by the `using`/namespace
    // context of the file the access lives in. Only ever used as a fallback for error types, so a correctly-bound
    // domain class that merely shares a table's name is never affected.
    public TableInfo? TryResolveTableByName(string? typeName, IReadOnlyCollection<string> contextNamespaces)
    {
        if (string.IsNullOrEmpty(typeName) || typeName == "?")
        {
            return null;
        }

        var keys = CandidateKeys(typeName);

        // 1) Prefer a database whose generated-code namespace is in scope where the access appears (a `using` of it,
        //    or the file's own namespace living under it). This is what disambiguates identical short names.
        foreach (var db in _databases)
        {
            if (db.DatabaseNamespace.Length == 0)
            {
                continue;
            }

            var inScope = contextNamespaces.Any(ns =>
                ns == db.DatabaseNamespace
                || ns.StartsWith(db.DatabaseNamespace + ".", StringComparison.Ordinal)
                || db.DatabaseNamespace.StartsWith(ns + ".", StringComparison.Ordinal));
            if (!inScope)
            {
                continue;
            }

            foreach (var key in keys)
            {
                if (db.TablesByKey.TryGetValue(key, out var table))
                {
                    return table;
                }
            }
        }

        // 2) Otherwise accept the name only if it maps to the same table in every database that has it (no ambiguity).
        TableInfo? unique = null;
        foreach (var db in _databases)
        {
            foreach (var key in keys)
            {
                if (db.TablesByKey.TryGetValue(key, out var table))
                {
                    if (unique is not null && unique.FullName != table.FullName)
                    {
                        return null; // same name in multiple databases and nothing in scope to choose — refuse to guess
                    }

                    unique = table;
                    break;
                }
            }
        }

        return unique;
    }

    // Resolves a C# property name to a column name on the given table (identity mapping by default), or null if the
    // member is not a persisted column (e.g. a navigation/association property or a non-mapped helper member).
    public string? ResolveColumn(TableInfo table, string propertyName)
    {
        if (table.Columns.Contains(propertyName))
        {
            return propertyName;
        }

        // Best-effort owned-type / value-object rename fallback (module EF value objects such as IsMandator -> the
        // "MandatorId" column). Extend as concrete renames surface; unknown members are simply skipped.
        if (KnownColumnRenames.TryGetValue(propertyName, out var renamed) && table.Columns.Contains(renamed))
        {
            return renamed;
        }

        return null;
    }

    public void RecordUnresolvedEntity(INamedTypeSymbol entityType) => _unresolvedEntities.Add(entityType.ToDisplayString());

    public void RecordUnresolvedColumn(TableInfo table, string propertyName) => _unresolvedColumns.Add($"{table.FullName}.{propertyName}");

    // Prints how many entity types / member names were seen against a known table but could not be mapped to a column
    // (mostly navigation/association properties and non-mapped helpers — expected — plus any genuine gaps worth a look).
    public void ReportUnresolved(string label)
    {
        Console.WriteLine($"[{label}] dbml catalog unresolved: {_unresolvedEntities.Count} entity types, {_unresolvedColumns.Count} member names");
        foreach (var column in _unresolvedColumns.OrderBy(c => c, StringComparer.Ordinal).Take(40))
        {
            Console.WriteLine($"[{label}]   unmapped member: {column}");
        }
    }

    private static readonly IReadOnlyDictionary<string, string> KnownColumnRenames = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        // Value-object / owned-type properties whose C# name differs from the underlying column. Best-effort; grows as
        // needed. Example: an `IsMandator`-configured `Mandator` property persists to the `MandatorId` column.
        ["Mandator"] = "MandatorId",
    };

    private static IReadOnlyList<string> CandidateKeys(string entityName)
    {
        var keys = new List<string> { entityName };
        const string dbModelSuffix = "DbModel";
        if (entityName.EndsWith(dbModelSuffix, StringComparison.Ordinal) && entityName.Length > dbModelSuffix.Length)
        {
            keys.Add(entityName[..^dbModelSuffix.Length]);
        }

        return keys;
    }
}
