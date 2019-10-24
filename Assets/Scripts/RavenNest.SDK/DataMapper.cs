using System;
using System.Linq;
using System.Reflection;

namespace RavenNest.SDK
{
    public class DataMapper
    {
        public static T Map<T, T2>(T2 data) where T : new()
        {
            var output = new T();
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in typeof(T2).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var p = props.FirstOrDefault(x => x.Name == prop.Name);
                if (p == null)
                {
                    continue;
                }

                try
                {
                    var value = prop.GetValue(data);
                    if (prop.PropertyType.IsEnum)
                    {
                        var intValue = Convert.ToInt32(value);
                        p.SetValue(output, intValue);
                    }
                    else
                    {
                        p.SetValue(output, value);
                    }
                }
                catch (Exception exc)
                {
                }
            }
            return output;
        }
    }
}