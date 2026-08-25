using System.Reflection;

namespace SampleIdentityApp.Utilities;

public static class ReflectionHelper
{
    public static IEnumerable<string> GetAllRoles()
    {
        return typeof(Roles)
         .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
         .Where(f => f.IsLiteral && !f.IsInitOnly)
         .Select(f => f.GetValue(null) as string)
         .OfType<string>();
    }
}