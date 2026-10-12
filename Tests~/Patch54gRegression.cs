using System;
using UnityEngine;
using TMPro;
using TuTienCore;

partial class MotionRegression
{
    static void Patch54gTests()
    {
        Run("54g all diagnostic attributes stay visible in the HUD data",()=>{
            var f=new Field();
            f.hero.atkStatusText=new GameObject(true).AddComponent<TextMeshProUGUI>();
            var data=HeroAt(12);
            data.spiritRoots=new System.Collections.Generic.List<ElementType>{ElementType.Hoa,ElementType.Moc};
            data.rootTiers=new System.Collections.Generic.List<int>{3,4};
            data.rootWeights=new System.Collections.Generic.List<float>{.4f,.6f};
            f.hero.ChangeAttackMode(1);
            f.hero.UpdateStats(data,190,350);
            var text=f.hero.atkStatusText.text;
            Check(text==f.hero.FullStatDetails,"full stats not replaced with compact 3-line summary");
            foreach(var part in new[]{"Cấp:","Cung","HP: 190/350","EXP:","ATK cơ bản:",
                "ATK Hỏa:","ATK Mộc:","Di chuyển:","Tốc đánh:","Nhịp đánh:","Tầm đánh:"})
                Check(text.Contains(part),"missing "+part);
        });
        Run("54g HUD grid places full stats to right of combat log",()=>{
            int savedW=Screen.width,savedH=Screen.height;
            try {
                foreach(int width in new[]{600,800,1150})
                {
                    Screen.width=width;Screen.height=Mathf.RoundToInt(width*9f/16f);
                    var f=new Field();var ui=ReadableFixture(f);
                    f.hero.atkStatusText.text=f.hero.FullStatDetails;
                    Call(ui,"LateUpdate");
                    var stats=Get<RectTransform>(ui,"statsPanel");
                    var gm=UnityEngine.Object.FindFirstObjectByType<GameManager>();
                    var log=(RectTransform)gm.eventLog.transform;
                    Check(stats.anchorMin.x==1&&log.anchorMin.x==0,"right stats, left log");
                    Check(stats.rect.width + log.rect.width + 18f <= width + .1f,"no column overlap");
                    Check(stats.rect.height<=Screen.height-106f,"stats inside gameplay height");
                    UIManager.Instance=null;
                }
            } finally { Screen.width=savedW;Screen.height=savedH;Time.timeScale=1; }
        });
        Run("54g map name holds center 3s then smoothly shrinks and travels",()=>{
            Near(MapTitleMotion.Progress(0),0,"center start");
            Near(MapTitleMotion.Progress(2.99f),0,"hold three seconds");
            Near(MapTitleMotion.Progress(3f),0,"movement begins only after 3s");
            Near(MapTitleMotion.Progress(3.425f),.5f,"smooth midpoint");
            Near(MapTitleMotion.Progress(3.85f),1,"reaches compact position");
            Near(MapTitleMotion.Progress(10),1,"does not overshoot");
        });
        Run("54g UI map title moves center to compact left-column label",()=>{
            var f=new Field();
            var ui=ReadableFixture(f);
            float oldDt=Time.deltaTime;
            try {
                Time.deltaTime=0f;
                ui.ShowMapTitle("Vùng Thử Nghiệm");
                var rect=Get<RectTransform>(ui,"mapTitleRect");
                Check(rect.gameObject.activeSelf,"title becomes visible");
                Near(rect.anchorMin.x,.5f,"starts center X");
                Near(rect.anchorMin.y,.5f,"starts center Y");
                var animation=Get<Coroutine>(ui,"mapTitleRoutine");
                Time.deltaTime=MapTitleMotion.HoldSeconds;
                animation.routine.MoveNext();
                Near(rect.anchorMin.y,.5f,"still centered at 3s");
                Time.deltaTime=MapTitleMotion.TravelSeconds;
                animation.routine.MoveNext();
                Near(rect.anchorMin.x,0f,"ends in left HUD column");
                Near(rect.anchorMin.y,1f,"ends under top edge");
                ui.HideMapTitle();
                Check(!rect.gameObject.activeSelf,"death/start hides title");
            } finally {Time.deltaTime=oldDt;UIManager.Instance=null;}
        });
        Run("54g damage text flies away from source and fades exactly one second",()=>{
            Near(DamagePopupMotion.AwaySign(200,100,-1),1,"victim right of attack flies right");
            Near(DamagePopupMotion.AwaySign(100,200,1),-1,"victim left of attack flies left");
            Check(DamagePopupMotion.X(.25f,1)>0&&DamagePopupMotion.X(.25f,-1)<0,"direction reflected");
            Check(DamagePopupMotion.Y(.25f)>0&&DamagePopupMotion.Y(.75f)>0,"arc rises above hit");
            Near(DamagePopupMotion.Alpha(0),1,"start opaque");
            Near(DamagePopupMotion.Alpha(.5f),.5f,"half faded");
            Near(DamagePopupMotion.Alpha(1),0,"fully transparent after 1s");
            Near(DamagePopupMotion.Alpha(2),0,"never negative opacity");
        });
    }
}
