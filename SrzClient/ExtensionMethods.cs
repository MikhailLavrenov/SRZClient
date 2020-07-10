using System.Collections;
using System.Text;

namespace SrzClient
{
    public static class ExtensionMethods
    {
        public static string PropertiesToString<T>(this T obj, string separator) where T : class
        {
            var props = typeof(T).GetProperties();

            var sb = new StringBuilder($"[{typeof(T).Name}]{separator}");

            foreach (var property in props)
            {
                sb.Append($"{property.Name}: ");

                var val = property.GetValue(obj, null);

                if (val is IList collection)
                {
                    foreach (var item in collection)
                    {
                        sb.Append(separator);
                        sb.Append(item.ToString());
                    }
                }
                else
                {
                    sb.Append(val?.ToString() ?? "Null");
                    sb.Append(separator);
                }
            }

            return sb.ToString();
        }
    }
}
