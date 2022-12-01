using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace FT_ADDON.Dynamic
{
    using Callback = Func<IDictionary<string, object>, object, string, object>;

    public class DynamicExecution
    {
        public static readonly JsonSerializerSettings jsonsettings = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };

        private enum ParamType
        {
            Array,
            Listing
        }

        private class MethodParams
        {
            public object obj;
            public ICollection<object> paramlist;
            public ParamType type;

            public MethodParams(object _obj, ICollection<object> _paramlist, ParamType _type)
            {
                obj = _obj;
                paramlist = _paramlist;
                type = _type;
            }
        }

        private static object ExpandoInvokeMethod(MethodInfo method, object value, string parent, object field, Callback subfunc)
        {
            if (value == null) return method.Invoke(field, null);

            Type type = value.GetType();

            if (typeof(ICollection<object>).IsAssignableFrom(type)) return ListInvoke(value as ICollection<object>, method.GetParameters().Length, parent, method, field, subfunc);
            
            if (type == typeof(object[])) return method.Invoke(field, value as object[]);
            
            if (type == typeof(string)) return method.Invoke(field, new object[] { value.ToString() });
            
            if (type == typeof(Int16) || type == typeof(int) || type == typeof(Int64)) return method.Invoke(field, new object[] { Convert.ToInt32(value) });
            
            if (type == typeof(double)) return method.Invoke(field, new object[] { Convert.ToDouble(value) });
            
            if (type == typeof(DateTime)) return method.Invoke(field, new object[] { DateTime.Parse(value.ToString()) });
            
            if (type == typeof(object)) return method.Invoke(field, new object[] { value });
            
            if (type == typeof(bool)) return method.Invoke(field, new object[] { Convert.ToBoolean(value) });
            
            if (typeof(IDictionary<string, object>).IsAssignableFrom(type)) return ExpandoInvoke(value as IDictionary<string, object>, method, field, parent, subfunc);

            throw new Exception($"Method type parameter mismatch with SAP detected - { parent }");
        }

        private static IDictionary<string, object> ExpandoInvoke(IDictionary<string, object> eo, MethodInfo method, object field, string parent, Callback subfunc)
        {
            Console.WriteLine($"{ nameof(SetProperty) } : { field }, { parent }");
            IDictionary<string, object> newudflist = new ExpandoObject();
            IDictionary<string, object> udflist = eo;
            object child = null;

            foreach (var udf in udflist)
            {
                child = method.Invoke(field, new object[] { udf.Key });
                child = subfunc(udf.Value as IDictionary<string, object>, child, parent + "." + udf.Key);

                if (child == null) continue;

                newudflist.Add(udf.Key, child);
            }

            return newudflist.Clone();
        }

        private static object ListInvoke(ICollection<object> curparam, int size, string parent, MethodInfo method, object field, Callback subfunc)
        {
            Console.WriteLine($"{ nameof(SetProperty) } : { size }, { parent }, { method.Name }, { field }");

            if (curparam.Count == 0)
            {
                return method.Invoke(field, null);
            }

            ICollection<object> methodlist = new List<object>();
            ICollection<object> tempparam = new List<object>();
            ParamType prmtype = ParamType.Listing;

            foreach (var obj in curparam)
            {
                Type objtype = obj.GetType();

                if (tempparam.Count == size)
                {
                    if (!typeof(IDictionary<string, object>).IsAssignableFrom(objtype)) throw new Exception($"Invalid object type detected - { parent }");

                    methodlist.Add(new MethodParams(obj as IDictionary<string, object>, tempparam, prmtype));
                    tempparam = new List<object>();
                    prmtype = ParamType.Listing;
                }
                else if (typeof(ICollection<object>).IsAssignableFrom(objtype))
                {
                    if (tempparam.Count != 0) throw new Exception($"Invalid parameter detected. Unused parameter(s) detected - { parent }");

                    tempparam = (obj as ICollection<object>);
                    prmtype = ParamType.Array;

                    if (tempparam.Count != size) throw new Exception($"Invalid parameter count detected. Expected paramater count ({ size }) - { parent }");
                }
                else if (typeof(IDictionary<string, object>).IsAssignableFrom(objtype))
                {
                    if (size != 1) throw new Exception($"Method require more than 1 parameter. Use array for parameters input instead - { parent }");

                    methodlist.Add(obj as IDictionary<string, object>);
                }
                else
                {
                    tempparam.Add(obj);
                }
            }

            if (tempparam.Count > 0) throw new Exception($"Incomplete method parameters detected - { parent }");

            object child = null;
            var newlist = new List<object>();
            string tempparent = parent.Remove(parent.Length - 2);

            foreach (var each in methodlist)
            {
                if (typeof(IDictionary<string, object>).IsAssignableFrom(each.GetType()))
                {
                    newlist.Add(ExpandoInvoke((IDictionary<string, object>)each, method, field, parent, subfunc));
                    continue;
                }

                MethodParams cur = each as MethodParams;
                string tempkey = JsonConvert.SerializeObject(cur.paramlist, jsonsettings);
                tempkey = $"{ tempkey.Substring(1, tempkey.Length - 2) })";

                if (cur.type == ParamType.Array)
                {
                    newlist.Add(cur.paramlist);
                }
                else
                {
                    foreach (var prm in cur.paramlist)
                    {
                        newlist.Add(prm);
                    }
                }

                child = method.Invoke(field, cur.paramlist.ToArray());
                child = subfunc(cur.obj as IDictionary<string, object>, child, parent + "." + tempkey);
                newlist.Add(child);
            }

            return newlist.Clone();
        }

        private static ICollection<T> ExpandoSAPDoc<T>(ICollection<object> eo, object field, Func<IDictionary<string, object>, object, string, T> subfunc, string parent = "Document")
        {
            ICollection<T> list = new List<T>();

            for (int i = 0; i < eo.Count; ++i)
            {
                object value = eo.ElementAt(i);

                if (value == null || !typeof(IDictionary<string, object>).IsAssignableFrom(value.GetType()))
                {
                    throw new Exception($"Property type array status mismatch with SAP detected - { parent }");
                }

                list.Add(subfunc(value as IDictionary<string, object>, field, parent));
            }

            return list;
        }

        private static object ExpandoInterface(PropertyInfo prop,
                                               object value,
                                               string parent,
                                               string key,
                                               object field,
                                               Func<ICollection<object>, object, string, object> listfunc,
                                               Callback dictfunc)
        {
            if (value == null) throw new Exception($"Property type interface status mismatch with SAP detected - { parent }.{ key }");

            Type type = value.GetType();

            if (typeof(ICollection<object>).IsAssignableFrom(type))
            {
                value = listfunc(value as ICollection<object>, prop.GetValue(field), $"{ parent }.{ key }");
                return value;
            }
            
            if (typeof(IDictionary<string, object>).IsAssignableFrom(type))
            {
                value = dictfunc(value as IDictionary<string, object>, prop.GetValue(field), $"{ parent }.{ key }");
                return value;
            }

            throw new Exception($"Property type interface status mismatch with SAP detected - { parent }.{ key }");
        }

        private static bool IsFunctionFormat(string key)
        {
            if (key.Length == 0 || key[0] == '(') return false;

            bool open = false;

            for (int i = 1; i < key.Length; ++i)
            {
                switch (key[i])
                {
                    case '(':
                        if (open) return false;

                        open = true;
                        break;
                    case ')':
                        if (i != key.Length - 1) return false;

                        return open;
                }
            }

            return false;
        }

        public static IDictionary<string, object> ExpandoFromSAPDoc(IDictionary<string, object> eo, object field, string parent = "Document")
        {
            GC.Collect();

            try
            {
                Type basetype = field.GetSAPType();
                var keylist = eo.Keys.ToArray();

                foreach (var key in keylist)
                {
                    if (IsUDF(key))
                    {
                        eo[key] = GetUDF(basetype, field, key);
                        continue;
                    }

                    if (IsFunctionFormat(key))
                    {
                        ReadFunctionExpression(eo, field, key, basetype, parent);
                        continue;
                    }

                    GetProperty(eo, field, key, basetype, parent);
                }

                return eo;
            }
            finally
            {
                GC.Collect();
            }
        }

        public static ICollection<object> ExpandoFromSAPDoc(ICollection<object> eo, object field, string parent = "Document")
        {
            GC.Collect();

            try
            {
                if (eo.Count == 1)
                {
                    object value = eo.FirstOrDefault() as IDictionary<string, object>;

                    if (value == null) throw new Exception($"Property type array status mismatch with SAP detected - { parent }");

                    Type type = field.GetType();
                    var method = type.GetMethod("SetCurrentLine");
                    var prop = type.GetProperty("Count");

                    if (method != null && prop != null)
                    {
                        var dict = value as IDictionary<string, object>;

                        if (!dict.ContainsKey("SetCurrentLine()"))
                        {
                            int count = Convert.ToInt32(prop.GetValue(field));
                            eo.Clear();

                            while (eo.Count < count)
                            {
                                var new_dict = dict.Clone();
                                ExpandoInvokeMethod(method, eo.Count, parent, field, ExpandoFromSAPDoc);
                                new_dict = ExpandoFromSAPDoc(new_dict, field, parent);
                                eo.Add(new_dict);
                            }

                            return eo;
                        }
                    }
                }

                foreach (var value in eo)
                {
                    var dict = value as IDictionary<string, object>;

                    if (dict == null) throw new Exception($"Property type array status mismatch with SAP detected - { parent }");

                    ExpandoFromSAPDoc(dict, field, parent);
                }

                return eo;
            }
            finally
            {
                GC.Collect();
            }
        }

        private static void ReadFunctionExpression(IDictionary<string, object> eo, object field, string key, Type basetype, string parent = "Document")
{
            MethodInfo method = basetype.GetMethod(key.Substring(0, key.Length - 2));

            if (method == null) throw new Exception($"Method not found in SAP - { parent }.{ key }");

            object value = ExpandoInvokeMethod(method, eo[key], parent + "." + key, field, ExpandoFromSAPDoc);

            if (value != null && typeof(IDictionary<string, object>).IsAssignableFrom(value.GetType()))
            {
                eo[key] = value;
            }
        }
        
        private static void WriteFunctionExpression(IDictionary<string, object> eo, object field, string key, Type basetype, string parent = "Document")
        {
            var method = basetype.GetMethod(key.Substring(0, key.Length - 2));

            if (method == null) throw new Exception("Method not found in SAP - " + parent + "." + key);

            ExpandoInvokeMethod(method, eo[key], parent + "." + key, field, ExpandoToSAPDoc);
        }

        private static void GetProperty(IDictionary<string, object> eo, object field, string key, Type basetype, string parent = "Document")
        {
            Console.WriteLine($"{ nameof(SetProperty) } : { field }, { key }, { basetype.Name }, { parent }");
            PropertyInfo prop = basetype.GetProperty(key);
            object value;

            if (prop == null) throw new Exception($"Property not found in SAP - { parent }.{ key }");

            if (prop.PropertyType.IsInterface)
            {
                if (!eo.TryGetValue(key, out value)) return;

                eo[key] = ExpandoInterface(prop, value, parent, key, field, ExpandoFromSAPDoc, ExpandoFromSAPDoc);
                return;
            }

            if (!prop.CanRead) return;

            value = prop.GetValue(field);
            eo[key] = prop.PropertyType.IsEnum && typeof(string) == eo[key].GetType() ? Enum.GetName(prop.PropertyType, value) : value;
        }
        
        private static void SetProperty(IDictionary<string, object> eo, object field, string key, Type basetype, string parent = "Document")
        {
            Console.WriteLine($"{ nameof(SetProperty) } : { field }, { key }, { basetype.Name }, { parent }");
            PropertyInfo prop = basetype.GetProperty(key);

            if (prop == null) throw new Exception($"Property not found in SAP - { parent }.{ key }");
            
            if (!eo.TryGetValue(key, out object value)) return;

            if (prop.PropertyType.IsInterface)
            {
                ExpandoInterface(prop, value, parent, key, field, ExpandoToSAPDoc, ExpandoToSAPDoc);
                return;
            }

            if (!prop.CanWrite || value == null) return;

            if (value.GetType() == typeof(Int64))
            {
                value = Convert.ToInt32(eo[key]);
            }

            prop.SetValue(field, SAPStandardType(prop.PropertyType, value));
        }

        public static ICollection<object> ExpandoToSAPDoc(ICollection<object> eo, object field, string parent = "Document")
        {
            GC.Collect();
            return ExpandoSAPDoc(eo, field, ExpandoToSAPDoc, parent);
        }

        public static object ExpandoToSAPDoc(IDictionary<string, object> eo, object field, string parent = "Document")
        {
            GC.Collect();

            try
            {
                Type basetype = field.GetSAPType();
                var keylist = eo.Keys.ToArray();

                foreach (var key in keylist)
                {
                    if (IsUDF(key))
                    {
                        SetUDF(basetype, field, key, eo[key]);
                        continue;
                    }

                    if (IsFunctionFormat(key))
                    {
                        WriteFunctionExpression(eo, field, key, basetype, parent);
                        continue;
                    }

                    SetProperty(eo, field, key, basetype, parent);
                }

                return eo;
            }
            finally
            {
                GC.Collect();
            }
        }

        private static bool IsUDF(string key)
        {
            return key.StartsWith("U_");
        }

        private static void SetUDF(Type basetype, object field, string key, object value)
        {
            Console.WriteLine($"{ nameof(SetUDF) } : { (basetype.IsCOMObject ? ComUtils.ComHelper.GetTypeName(field) : basetype.Name) }, { field }, { key }, { value }");

            if (value == null) return;

            var prop1 = basetype.GetProperty("UserFields");
            SAPbobsCOM.UserFields udf = (SAPbobsCOM.UserFields)prop1.GetValue(field);
            
            if (value.GetType() == typeof(Int64))
            {
                value = Convert.ToInt32(value);
            }

            udf.Fields.Item(key).Value = value;
        }

        private static object GetUDF(Type basetype, object field, string key)
{
            Console.WriteLine($"{ nameof(GetUDF) } : { (basetype.IsCOMObject ? ComUtils.ComHelper.GetTypeName(field) : basetype.Name) }, { field }, { key }");
            var prop1 = basetype.GetProperty("UserFields");
            SAPbobsCOM.UserFields udf = (SAPbobsCOM.UserFields)prop1.GetValue(field);
            return udf.Fields.Item(key).Value;
        }

        private static object SAPStandardType(Type type, object input)
        {
            if (type == input.GetSAPType()) return input;

            if (type == typeof(DateTime)) return DateTime.Parse(input.ToString());

            if (type.IsEnum) return typeof(string) == input.GetType() ? Enum.Parse(type, input.ToString(), true) : input;

            if (type.IsInterface) throw new Exception("Invalid type input - Interface input attempt detected. Please contact Dave");

            return Convert.ChangeType(input, type);
        }
    }
}
