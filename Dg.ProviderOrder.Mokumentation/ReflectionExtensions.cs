using System.Reflection;

namespace Dg.ProviderOrder.Mokumentation;

public static class ReflectionExtensions
{
    const string MediatorRequestHandlerInterfaceName = "IRequestHandler";
    const string MediatorRequestHandlerInterfaceNamespace = "MediatR";

    public static bool IsMediatrRequestHandler(this Type type) => type.GetMediatrHandlerInterfaces().Any();

    public static IEnumerable<Type> GetMediatrHandlerInterfaces(this Type type) => type.GetInterfaces().Where(i =>
        i.Name.StartsWith(MediatorRequestHandlerInterfaceName) &&
        i.Namespace == MediatorRequestHandlerInterfaceNamespace);

    public static Type[] GetTypesSafe(this Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch
        {
            return new Type[] { };
        }
    }
}
