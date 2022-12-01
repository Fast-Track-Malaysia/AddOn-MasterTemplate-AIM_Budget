using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    class Subscription
    {
        static private Dictionary<string, Action<object>> data_pool = new Dictionary<string, Action<object>>();

        private Subscription() { }

        static public void Subscribe(string key, Action<object> value)
        {
            lock (data_pool)
            {
                if (data_pool.ContainsKey(key))
                {
                    data_pool[key] += value;
                    return;
                }

                data_pool.Add(key, value);
            }
        }

        static public void Broadcast(string key, object value)
        {
            if (!data_pool.TryGetValue(key, out var action)) throw new Exception($"{ key } key does not exist in the Data Pool");

            action(value);
        }

        static public void Unsubsribe(string key)
        {
            lock (data_pool)
            {
                if (!data_pool.ContainsKey(key)) return;

                data_pool.Remove(key);
            }
        }
    }
}
