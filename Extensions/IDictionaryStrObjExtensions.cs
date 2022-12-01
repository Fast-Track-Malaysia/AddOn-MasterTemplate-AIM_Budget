using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace FT_ADDON
{
    static public class DictionaryStrObjExtensions
    {
        public static T Clone<T>(this T eo) where T : IDictionary<string, object>
        {
            Type type = eo.GetType();
            T t_map = default(T);
            t_map = t_map != null ? t_map : (T)Activator.CreateInstance(type);

            foreach (var prop in type.GetProperties())
            {
                if (!prop.CanRead || !prop.CanWrite || prop.GetMethod == null || prop.SetMethod == null) continue;

                switch (prop.Name)
                {
                    case nameof(IDictionary<string, object>.Keys):
                    case nameof(IDictionary<string, object>.Values):
                    case "Item":
                        continue;
                }

                object value = prop.GetValue(eo);
                prop.SetValue(t_map, value);
            }

            foreach (var field in type.GetFields())
            {
                field.SetValue(t_map, field.GetValue(eo));
            }

            foreach (var kvp in eo)
            {
                if (kvp.Value != null)
                {
                    type = kvp.Value.GetType();

                    if (type == typeof(T))
                    {
                        T clone = ((T)kvp.Value).Clone();
                        t_map.Add(kvp.Key, clone);
                        continue;
                    }

                    if (typeof(IDictionary<string, object>).IsAssignableFrom(type))
                    {
                        IDictionary<string, object> clone = (kvp.Value as IDictionary<string, object>).Clone();
                        t_map.Add(kvp.Key, clone);
                        continue;
                    }

                    if (typeof(ICollection<object>).IsAssignableFrom(type))
                    {
                        ICollection<object> clone = (kvp.Value as ICollection<object>).Clone();
                        t_map.Add(kvp.Key, clone);
                        continue;
                    }
                }

                t_map.Add(kvp);
            }

            return t_map;
        }
    }
}
