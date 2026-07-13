using System.Reflection;

namespace Dg.ProviderOrder.Mokumentation;

public static class AssemblyUtils
{
    public static void LoadAllDgChildAssemblies(FileInfo entryFile)
    {
        AppDomain.CurrentDomain.AssemblyResolve +=
            (_, args) => CurrentDomain_AssemblyResolve(args, entryFile.DirectoryName!);

        LoadAllAssemblies(entryFile.DirectoryName!);
    }

    public static Type GetTypeFromLoadedAssemblies(string typeName, string assemblyName)
    {
        var type = TryGetTypeFromLoadedAssembly(typeName, assemblyName);
        if (type == null)
        {
            var assemblies = string.Join(
                Environment.NewLine,
                AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetName()));
            throw new InvalidOperationException($"type {typeName} was not found in currently loaded assemblies:{Environment.NewLine}{assemblies}");
        }

        return type;
    }

    public static Type? TryGetTypeFromLoadedAssembly(string typeName, string assemblyName)
    {
        var type = AppDomain.CurrentDomain
            .GetAssemblies()
            .SingleOrDefault(a => a.GetName().Name == assemblyName)
            ?.GetTypes()
            .FirstOrDefault(t => t.FullName == typeName);

        return type;
    }

    private static void LoadAllAssemblies(string directory)
    {
        var filePaths = Directory.GetFiles(directory, "*.dll");
        var dgAssemblies = filePaths
            .Where(f =>
            {
                var fileName = Path.GetFileName(f);
                return fileName.StartsWith("Dg") || fileName.StartsWith("devinite") || fileName.StartsWith("Chabis");
            });

        foreach (var referencedAssembly in dgAssemblies)
        {
            Assembly.LoadFile(referencedAssembly);
        }
    }

    private static Assembly? CurrentDomain_AssemblyResolve(ResolveEventArgs args, string applicationRoot)
    {
        var tokens = args.Name.Split(",".ToCharArray());
        System.Diagnostics.Debug.WriteLine("Resolving : " + args.Name);
        if (tokens[0].Contains("resources"))
        {
            return null;
        }

        var assemblyPath = GetAssemblyPathOrNull(tokens[0], applicationRoot);
        if (assemblyPath == null)
        {
            return null;
        }

        return Assembly.LoadFile(assemblyPath);
    }

    private static string? GetAssemblyPathOrNull(string assemblyName, string rootPath)
    {
        if (File.Exists(Path.Combine(rootPath, assemblyName + ".dll")))
        {
            return Path.Combine(rootPath, assemblyName + ".dll");
        }

        return null;
    }
}