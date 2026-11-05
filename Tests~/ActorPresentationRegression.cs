using UnityEngine;

partial class MotionRegression
{
    static void ActorPresentationTests()
    {
        using(var resources=new SpriteResources())
        {
            foreach(bool enemy in new[]{false,true})
            Run("production body defaults to 200 percent and preserves logical rect / enemy="+enemy,()=>{
                var f=new Field(largeActors:true);RectTransform root;
                if(enemy)
                {
                    var monster=f.Monster(-200);root=monster.Rect;root.gameObject.AddComponent<UnityEngine.UI.Image>();
                    CultivationSpriteBridge.AttachMonster(monster);
                }
                else
                {
                    root=f.hero.heroRect;root.gameObject.AddComponent<UnityEngine.UI.Image>();CultivationSpriteBridge.AttachHero(f.hero);
                }
                var bridge=root.GetComponent<CultivationSpriteBridge>();var body=bridge.BodyGraphic.rectTransform;
                Near(bridge.heroScale,2,"Hero default");Near(bridge.monsterScale,2,"Monster default");
                Near(Mathf.Abs(body.localScale.x),2,"body width doubled");Near(body.localScale.y,2,"body height doubled");
                Near(root.localScale.x,1,"root/hitbox scale unchanged");
            });
        }
        int previousWidth=Screen.width;
        try
        {
            foreach(int width in new[]{600,800,1150})
            Run("battle projection rises exactly 20 screen pixels without changing logic / "+width,()=>{
                var f=new Field();var canvas=new GameObject(true).AddComponent<Canvas>();
                f.ground.SetParent(canvas.transform,false);Set(f.space,"battleCanvas",canvas);Set(f.space,"logicalWidth",1000f);
                Screen.width=width;var before=f.space.Position(f.hero.heroRect);float range=f.space.AttackRange(f.hero.attackMode,true);
                f.space.SyncBattleProjection();Near(f.ground.anchoredPosition.y*canvas.scaleFactor,20,"20px independent of projection");
                Check(f.space.Position(f.hero.heroRect).Equals(before),"logical foot unchanged");Near(f.space.AttackRange(f.hero.attackMode,true),range,"range unchanged");
                Set(f.space,"battleVerticalOffsetPixels",40f);f.space.SyncBattleProjection();
                Near(f.ground.anchoredPosition.y*canvas.scaleFactor,40,"offset change updates without resizing");
            });
        }
        finally{Screen.width=previousWidth;}
    }
}
