// Headless-only JsonUtility helper for the new nested, serializable save
// fields. Unity itself uses its native JsonUtility in Play Mode.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

internal static class NestedEquipmentSaveStub
{
    public static bool Supports(Type t)
    {
        return t==typeof(long) ||
            t==typeof(List<EquipmentInstance>) ||
            t==typeof(List<ProfessionProgress>) ||
            t==typeof(List<LearnedProfessionRecipe>) ||
            t==typeof(List<LearnedSupportSkill>);
    }

    public static string Encode(object value)
    {
        if(value==null)return "null";
        if(value is string)
        {
            return "\"" + ((string)value).Replace("\\","\\\\")
                .Replace("\"","\\\"").Replace("\n","\\n") + "\"";
        }
        if(value is bool)return (bool)value?"true":"false";
        var type=value.GetType();
        if(type.IsEnum)return Convert.ToInt32(value).ToString(CultureInfo.InvariantCulture);
        if(type==typeof(int)||type==typeof(long)||type==typeof(float)||type==typeof(double))
            return Convert.ToString(value,CultureInfo.InvariantCulture);
        var enumerable=value as IEnumerable;
        if(enumerable!=null)
        {
            var entries=new List<string>();
            foreach(var item in enumerable)entries.Add(Encode(item));
            return "["+string.Join(",",entries.ToArray())+"]";
        }
        var fields=new List<string>();
        foreach(var field in type.GetFields(BindingFlags.Instance|BindingFlags.Public))
            fields.Add(Encode(field.Name)+":"+Encode(field.GetValue(value)));
        return "{"+string.Join(",",fields.ToArray())+"}";
    }

    public static void ReadInto(string json,object target,FieldInfo field)
    {
        var decoded=new MiniParser(json).Parse();
        var root=decoded as Dictionary<string,object>;
        if(root==null)return;
        object value;
        if(!root.TryGetValue(field.Name,out value))return;
        field.SetValue(target,ConvertValue(value,field.FieldType));
    }

    private static object ConvertValue(object node,Type type)
    {
        if(node==null)return null;
        if(type==typeof(string))return node.ToString();
        if(type==typeof(bool))return (bool)node;
        if(type==typeof(int))return Convert.ToInt32(node,CultureInfo.InvariantCulture);
        if(type==typeof(long))return Convert.ToInt64(node,CultureInfo.InvariantCulture);
        if(type==typeof(float))return Convert.ToSingle(node,CultureInfo.InvariantCulture);
        if(type==typeof(double))return Convert.ToDouble(node,CultureInfo.InvariantCulture);
        if(type.IsEnum)return Enum.ToObject(type,Convert.ToInt32(node,CultureInfo.InvariantCulture));
        if(type.IsGenericType && type.GetGenericTypeDefinition()==typeof(List<>))
        {
            var result=(IList)Activator.CreateInstance(type);
            var elements=node as List<object>;
            if(elements!=null)foreach(var entry in elements)
                result.Add(ConvertValue(entry,type.GetGenericArguments()[0]));
            return result;
        }
        var fields=node as Dictionary<string,object>;
        if(fields==null)return null;
        var instance=Activator.CreateInstance(type);
        foreach(var field in type.GetFields(BindingFlags.Instance|BindingFlags.Public))
        {
            object value;
            if(fields.TryGetValue(field.Name,out value))
                field.SetValue(instance,ConvertValue(value,field.FieldType));
        }
        return instance;
    }

    private sealed class MiniParser
    {
        private readonly string json;
        private int offset;
        public MiniParser(string source){json=source;}
        public object Parse() { Skip();var value=Next();Skip();return value; }
        private void Skip(){while(offset<json.Length && char.IsWhiteSpace(json[offset]))offset++;}
        private char Current {get{return offset<json.Length?json[offset]:'\0';}}
        private void Eat(char expected)
        {Skip();if(Current!=expected)throw new FormatException("Expected "+expected);offset++;}
        private object Next()
        {
            Skip();
            if(Current=='{')
            {
                Eat('{');
                var result=new Dictionary<string,object>();
                Skip();
                if(Current=='}'){Eat('}');return result;}
                while(true)
                {
                    string key=StringValue();
                    Eat(':');result[key]=Next();Skip();
                    if(Current=='}'){Eat('}');break;}
                    Eat(',');
                }
                return result;
            }
            if(Current=='[')
            {
                Eat('[');
                var result=new List<object>();
                Skip();
                if(Current==']'){Eat(']');return result;}
                while(true)
                {
                    result.Add(Next());Skip();
                    if(Current==']'){Eat(']');break;}
                    Eat(',');
                }
                return result;
            }
            if(Current=='"')return StringValue();
            if(json.Substring(offset).StartsWith("true",StringComparison.Ordinal))
            {offset+=4;return true;}
            if(json.Substring(offset).StartsWith("false",StringComparison.Ordinal))
            {offset+=5;return false;}
            if(json.Substring(offset).StartsWith("null",StringComparison.Ordinal))
            {offset+=4;return null;}
            int start=offset;
            while(offset<json.Length &&
                (char.IsDigit(json[offset])||json[offset]=='-'||json[offset]=='+'||
                 json[offset]=='.'||json[offset]=='e'||json[offset]=='E'))offset++;
            if(start==offset)throw new FormatException("Invalid JSON token");
            return double.Parse(json.Substring(start,offset-start),CultureInfo.InvariantCulture);
        }
        private string StringValue()
        {
            Eat('"');
            var result=new StringBuilder();
            while(offset<json.Length)
            {
                char c=json[offset++];
                if(c=='"')return result.ToString();
                if(c=='\\')
                {
                    if(offset>=json.Length)throw new FormatException("Escape truncated");
                    char escaped=json[offset++];
                    if(escaped=='n')result.Append('\n');
                    else if(escaped=='r')result.Append('\r');
                    else if(escaped=='t')result.Append('\t');
                    else result.Append(escaped);
                }
                else result.Append(c);
            }
            throw new FormatException("String truncated");
        }
    }
}
