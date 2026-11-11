// Only the bridge boundary is modeled. These are NOT gameplay controllers or
// native JsonUtility, texture import, rendering, lifecycle or Canvas tests.
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
namespace UnityEngine.Events { public class UnityEvent { } }
namespace UnityEngine
{
    public static class JsonUtility
    {
        public static void FromJsonOverwrite(string json,object value)
        {
            foreach(var f in value.GetType().GetFields())
            {
                var m=Regex.Match(json,"\""+f.Name+"\"\\s*:\\s*(\"[^\"]*\"|[0-9.]+)");
                if(!m.Success)continue;
                string token=m.Groups[1].Value;
                f.SetValue(value,f.FieldType==typeof(string)?token.Trim('"'):
                    Convert.ChangeType(token,f.FieldType,CultureInfo.InvariantCulture));
            }
        }
    }
}
namespace TuTienCore { public enum AttackMode { Melee, RangedPhysical, RangedMagic } }
public enum HeroState { Idle, Returning, Approaching, Combat, Dead }
public sealed class HeroController : MonoBehaviour
{
    public RectTransform heroRect;
    public Image hpFillImage;
    public bool IsDeployed=true;
    public int MovementFrame=-1;
    public bool IsExplorationMovement;
    public float MovementDistanceThisFrame;
    public HeroState CurrentState;
    public TuTienCore.AttackMode attackMode;
}
public sealed class MonsterController : MonoBehaviour
{
    public RectTransform Rect;
    public Image hpFillImage;
    public TuTienCore.AttackMode attackMode;
    public string SpriteIdentityKey;
    public bool FacingRight=true;
    public void RefreshSpriteIdentity() { }
}
