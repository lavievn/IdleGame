using System;
using UnityEngine;
using TMPro;
using TuTienCore;

// .54g completion checks. Mock Unity tests do not validate final rendered pixels.
partial class MotionRegression
{
    static void Patch54hTests()
    {
        Run("54g fix: standing ranged monster stays attached to the moving Ground",()=>{
            var f = new Field();
            f.hero.ChangeAttackMode(0);
            f.hero.moveSpeed = 300f;
            f.space.scrollSpeed = 40f; // Deliberately different from Hero/Ground speed.
            var ranged = f.Monster(-150f,AttackMode.RangedPhysical);
            float before = f.space.Position(ranged.Rect).x;
            f.Step(.1f);
            Check(ranged.currentState == MonsterState.Attacking,"ranged monster should stand and shoot");
            Check(f.space.CurrentBackgroundSpeed > f.space.CurrentCameraSpeed + .01f,
                "reproduce different Ground and camera speeds");
            Near(f.space.Position(ranged.Rect).x - before,
                f.space.CurrentBackgroundSpeed * .1f,
                "standing monster must advance with actual Ground",.05f);
        });
        Run("54g fix: targetless monsters and standing ones get carrier compensation",()=>{
            Near(BattleMotion.MonsterApproach(-150,0,250,150,.1f,18),-132f,
                "in-range monster moves with carrier");
            Near(BattleMotion.MonsterApproach(-500,0,250,150,.1f,18),-467f,
                "approaching monster moves with walking plus carrier");
            Near(BattleMotion.MonsterApproach(-150,0,250,150,.1f,-12),-162f,
                "negative Ground-camera difference remains valid");
        });
        Run("54g fix: Hero and monster damage labels are anchored to damage recipient body",()=>{
            var f=new Field();
            var heroLabel=new GameObject(true).AddComponent<TextMeshProUGUI>();
            heroLabel.transform.parent=f.ground; // scene's former distant sibling
            f.hero.dmgTextPrototype=heroLabel;
            Call(f.hero,"AlignDamageTextToBody");
            Check(heroLabel.transform.parent==f.hero.heroRect,
                "Hero damage text parent changed from Ground to Hero body");
            var monster=f.Monster(-100);
            var bar=new GameObject(true);bar.transform.parent=monster.Rect;
            var monsterLabel=new GameObject(true).AddComponent<TextMeshProUGUI>();
            monsterLabel.transform.parent=bar.transform; // prefab old HP-bar parent
            monster.dmgTextPrototype=monsterLabel;
            Call(monster,"AlignDamageTextToBody");
            Check(monsterLabel.transform.parent==monster.Rect,
                "monster damage text parent changed from HP bar to monster body");
            Near(((RectTransform)heroLabel.transform).anchoredPosition.x,0,"Hero body centred");
            Near(((RectTransform)monsterLabel.transform).anchoredPosition.x,0,"monster body centred");
        });
        Run("54g fix: compact map label keeps current wave and terrain refreshed",()=>{
            var f=new Field();
            var ui=ReadableFixture(f);
            var d=HeroAt(1);
            d.mapNumber=3;d.mapTerrain=TerrainType.DoiNui;d.completedWavesInMap=0;
            ui.UpdateMapDebug(d);
            var detail=Get<TextMeshProUGUI>(ui,"mapDebugText");
            Check(detail.text.Contains("Bản đồ 3")&&detail.text.Contains("Wave 1/5"),
                "current map and first wave visible");
            Check(detail.text.Contains("Đồi núi"),"terrain name is shown");
            d.completedWavesInMap=3;d.mapTerrain=TerrainType.HoNuoc;
            ui.UpdateMapDebug(d);
            Check(detail.text.Contains("Wave 4/5")&&detail.text.Contains("Hồ nước"),
                "wave and changed terrain refresh without replaying title");
            UIManager.Instance=null;
        });
    }
}
