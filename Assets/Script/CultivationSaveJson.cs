using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TuTienCore
{
    // Structural reader for the component boundary only. JsonUtility remains the serializer.
    // Distinguishes absent/null/partial/duplicate properties before template overwrite.
    public static class CultivationSaveJson
    {
        sealed class Reader
        {
            readonly string s;int i;
            public Reader(string text){s=text??"";}
            void White(){while(i<s.Length && (s[i]==' ' || s[i]=='\r' || s[i]=='\n' || s[i]=='\t'))i++;}
            void Need(char c){White();if(i>=s.Length || s[i++]!=c)throw new FormatException("Invalid JSON boundary.");}
            public string String()
            {
                Need('"');var b=new StringBuilder();
                while(i<s.Length) {
                    char c=s[i++];if(c=='"')return b.ToString();if(c<' ')throw new FormatException();
                    if(c!='\\'){b.Append(c);continue;}if(i>=s.Length)throw new FormatException();c=s[i++];
                    switch(c) {
                        case '"':case '\\':case '/':b.Append(c);break;
                        case 'b':b.Append('\b');break;case 'f':b.Append('\f');break;case 'n':b.Append('\n');break;
                        case 'r':b.Append('\r');break;case 't':b.Append('\t');break;
                        case 'u':if(i+4>s.Length)throw new FormatException();b.Append((char)ushort.Parse(s.Substring(i,4),NumberStyles.HexNumber,CultureInfo.InvariantCulture));i+=4;break;
                        default:throw new FormatException();
                    }
                }
                throw new FormatException();
            }
            public Dictionary<string,string> Object(int depth=0)
            {
                if(depth>32)throw new FormatException("JSON depth limit.");Need('{');var fields=new Dictionary<string,string>(StringComparer.Ordinal);White();
                if(i<s.Length && s[i]=='}'){i++;return fields;}
                while(true) {
                    string key=String();Need(':');White();int start=i;Value(depth+1);
                    if(fields.ContainsKey(key))throw new FormatException("Duplicate JSON field: "+key);
                    fields.Add(key,s.Substring(start,i-start));White();if(i<s.Length && s[i]=='}'){i++;return fields;}Need(',');
                }
            }
            void Value(int depth)
            {
                if(depth>32)throw new FormatException();White();if(i>=s.Length)throw new FormatException();char c=s[i];
                if(c=='{'){Object(depth);return;}if(c=='"'){String();return;}
                if(c=='['){Array(depth,null);return;}
                foreach(string word in new[]{"true","false","null"})if(i+word.Length<=s.Length && s.Substring(i,word.Length)==word){i+=word.Length;return;}
                int begin=i;if(c=='-')i++;if(i>=s.Length)throw new FormatException();
                if(s[i]=='0')i++;else {if(s[i]<'1' || s[i]>'9')throw new FormatException();while(i<s.Length && char.IsDigit(s[i]))i++;}
                if(i<s.Length && s[i]=='.'){i++;int n=i;while(i<s.Length && char.IsDigit(s[i]))i++;if(n==i)throw new FormatException();}
                if(i<s.Length && (s[i]=='e' || s[i]=='E')){i++;if(i<s.Length && (s[i]=='+' || s[i]=='-'))i++;int n=i;while(i<s.Length && char.IsDigit(s[i]))i++;if(n==i)throw new FormatException();}
                if(begin==i)throw new FormatException();
            }
            public void Array(int depth,List<string> values)
            {
                Need('[');White();if(i<s.Length && s[i]==']'){i++;return;}
                while(true)
                {
                    White();int start=i;Value(depth+1);values?.Add(s.Substring(start,i-start));White();
                    if(i<s.Length && s[i]==']'){i++;return;}Need(',');
                }
            }
            public void End(){White();if(i!=s.Length)throw new FormatException("Trailing JSON data.");}
        }
        public static bool TryObject(string json,out Dictionary<string,string> fields,out string reason)
        {
            fields=null;reason=null;
            try {var r=new Reader(json);fields=r.Object();r.End();return true;}
            catch(Exception e) when(e is FormatException || e is OverflowException){reason="Invalid cultivation JSON: "+e.Message;return false;}
        }
        public static bool TryString(string token,out string text)
        {text=null;try{var r=new Reader(token);text=r.String();r.End();return true;}catch(FormatException){return false;}catch(OverflowException){return false;}}
        public static bool TryArray(string token,out List<string> values)
        {
            values=new List<string>();
            try{var reader=new Reader(token);reader.Array(0,values);reader.End();return true;}
            catch(Exception e) when(e is FormatException || e is OverflowException){values=null;return false;}
        }
    }
}
