namespace SecurityMaster.Core.Filtering;
public static class FilterFieldResolver
{
    public static HashSet<string> GetAllowedFields(Type type, int maxDepth = 2)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Collect(type, "", 0, maxDepth, result, new HashSet<Type>());
        return result;
    }

    private static void Collect(Type type, string prefix, int depth, int maxDepth,
        HashSet<string> result, HashSet<Type> visited)
    {
        if (depth > maxDepth || !visited.Add(type)) return;

        foreach (var prop in type.GetProperties())
        {
            var path = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
            var propType = prop.PropertyType;

            var isCollection = propType != typeof(string)
                && typeof(System.Collections.IEnumerable).IsAssignableFrom(propType);

            if (isCollection) continue; // preskoči kolekcije (Allocations, itd.) — nema smisla filtrirati po njima ovako

            var isSimple = propType.IsPrimitive || propType.IsEnum
                || propType == typeof(string) || propType == typeof(decimal)
                || propType == typeof(DateTime) || propType == typeof(Guid)
                || Nullable.GetUnderlyingType(propType) != null;

            if (isSimple)
            {
                result.Add(path);
            }
            else
            {
                Collect(propType, path, depth + 1, maxDepth, result, visited);
            }
        }
    }
}