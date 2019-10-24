using System.Linq;
using System.Reflection;

public static class FieldComparer
{
    public static bool Same<T>(T itemA, T itemB, params string[] exclude)
    {
        var fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public);
        foreach (var field in fields)
        {
            if (exclude.Contains(field.Name)) continue;
            var valueA = field.GetValue(itemA);
            var valueB = field.GetValue(itemB);
            if (!valueA.Equals(valueB)) return false;
        }

        return true;
    }
}
