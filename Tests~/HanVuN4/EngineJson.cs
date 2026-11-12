using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Script.Serialization;

// A fields-only JSON engine service for the boundary runner. The production
// component validators still execute. NOT Unity JsonUtility or native evidence.
namespace UnityEngine
{
    public static class JsonUtility
    {
        static readonly JavaScriptSerializer json=new JavaScriptSerializer{MaxJsonLength=16*1024*1024};
        public static string ToJson(object value,bool pretty=false)=>json.Serialize(Node(value));
        static object Node(object value)
        {
            if(value==null)return null;var type=value.GetType();
            if(type.IsEnum)return Convert.ToInt32(value);
            if(value is string || type.IsPrimitive || value is decimal)return value;
            if(value is IEnumerable items)
            {var result=new List<object>();foreach(var item in items)result.Add(Node(item));return result;}
            var fields=new Dictionary<string,object>();
            foreach(var field in type.GetFields(BindingFlags.Public|BindingFlags.Instance))
                if(!field.IsNotSerialized)fields.Add(field.Name,Node(field.GetValue(value)));
            return fields;
        }
        public static T FromJson<T>(string root) where T:new()=> (T)ConvertNode(json.DeserializeObject(root),typeof(T));
        public static void FromJsonOverwrite(string root,object target)
        {Populate((Dictionary<string,object>)json.DeserializeObject(root),target);}
        static void Populate(Dictionary<string,object> fields,object target)
        {
            foreach(var field in target.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance))
                if(!field.IsNotSerialized && fields.TryGetValue(field.Name,out var node))
                    field.SetValue(target,ConvertNode(node,field.FieldType));
        }
        static object ConvertNode(object node,Type type)
        {
            if(node==null)return null;
            if(type.IsEnum)return Enum.ToObject(type,Convert.ToInt32(node));
            if(type==typeof(string))return (string)node;
            if(type.IsPrimitive || type==typeof(decimal))return Convert.ChangeType(node,type,System.Globalization.CultureInfo.InvariantCulture);
            if(type.IsGenericType && type.GetGenericTypeDefinition()==typeof(List<>))
            {
                var list=(IList)Activator.CreateInstance(type);
                foreach(var item in (IEnumerable)node)list.Add(ConvertNode(item,type.GetGenericArguments()[0]));return list;
            }
            if(type.IsArray)
            {
                var items=(object[])node;var array=Array.CreateInstance(type.GetElementType(),items.Length);
                for(int i=0;i<items.Length;i++)array.SetValue(ConvertNode(items[i],type.GetElementType()),i);return array;
            }
            var result=Activator.CreateInstance(type,true);Populate((Dictionary<string,object>)node,result);return result;
        }
    }
}
