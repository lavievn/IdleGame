using System;
using System.Collections.Generic;
using System.Globalization;
namespace TuTienCore
{
    public static partial class CultivationProductionConfiguration
    {
        public static string IdentityForVersion(int version){return version==1?"production:cultivation-v09":version==2?"production:cultivation-v11-qi-v2":version==3?"production:cultivation-v11-capacity":null;}
        static string ResourceForVersion(int version){return version==1?"CultivationProductionV1":version==2?"CultivationProductionV2":version==3?"CultivationProductionV3":null;}
        public static int SupportedVersion(string id,int version){return IdentityForVersion(version)==id?version:0;}
        public static bool TryResolve(string component,CultivationPlaytestProfile context,out CultivationPlaytestProfile profile,out string reason)
        {
            profile=context;reason=null;if(string.IsNullOrEmpty(component))return true;
            Dictionary<string,string> fields;string id;int version;
            if(context==null || !CultivationSaveJson.TryObject(component,out fields,out reason) || !fields.ContainsKey("configurationId") || !CultivationSaveJson.TryString(fields["configurationId"],out id) || !fields.ContainsKey("configurationVersion") || !int.TryParse(fields["configurationVersion"],NumberStyles.Integer,CultureInfo.InvariantCulture,out version)){reason="Invalid cultivation configuration boundary.";return false;}
            if(id==context.Id && version==context.Version)return true;
            if(!context.IsProduction || SupportedVersion(context.Id,context.Version)==0 || SupportedVersion(id,version)==0 || version>context.Version){reason="Unsupported cultivation configuration identity/version.";return false;}
            WorldQiDistribution qi;CultivationConfigurationReason code;
            if(!TryLoadVersion(version,out profile,out qi,out code,out reason)){reason=code+": "+reason;return false;}return true;
        }
        internal static bool TryClockSource(string id,int version,CultivationPlaytestProfile current,out CultivationPlaytestProfile source)
        {
            source=current;if(current!=null && id==current.Id && version==current.Version)return true;
            if(current==null || SupportedVersion(id,version)==0 || SupportedVersion(current.Id,current.Version)==0 || version>current.Version)return false;
            WorldQiDistribution qi;CultivationConfigurationReason reason;string detail;return TryLoadVersion(version,out source,out qi,out reason,out detail);
        }
    }
}
