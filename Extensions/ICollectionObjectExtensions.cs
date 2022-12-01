using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace FT_ADDON
{
    static public class CollectionObjectExtensions
    {
        public static T Clone<T>(this T eo) where T : ICollection<object>
        {
            T list = default(T);
            list = list != null ? list : (T)Activator.CreateInstance(eo.GetType());

            foreach (var v in eo)
            {
                if (v != null)
                {
                    Type type = v.GetType();

                    if (type == typeof(T))
                    {
                        T clone = ((T)v).Clone();
                        list.Add(clone);
                        continue;
                    }
                    if (typeof(IDictionary<string, object>).IsAssignableFrom(type))
                    {
                        IDictionary<string, object> clone = (v as IDictionary<string, object>).Clone();
                        list.Add(clone);
                        continue;
                    }
                    else if (typeof(ICollection<object>).IsAssignableFrom(type))
                    {
                        ICollection<object> clone = (v as ICollection<object>).Clone();
                        list.Add(clone);
                        continue;
                    }
                }

                list.Add(v);
            }

            return list;
        }
    }
}
