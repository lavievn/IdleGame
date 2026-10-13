using System;
using UnityEngine;
using TMPro;

// Visual regression for the primary and secondary Hero damage labels.
// Tests only the geometry/motion contract; final appearance needs Unity Play Mode.
partial class MotionRegression
{
    static void HeroDamagePopupTests()
    {
        Run("0.0.51 Hero damage begins at visual body centre, not text-box left edge",()=>{
            foreach(float scale in new[]{.5f,1f,2f})
            {
                var f=new Field(scale);
                var labelRoot=new GameObject(true);
                labelRoot.transform.parent=f.ground;
                var labelRect=(RectTransform)labelRoot.transform;
                labelRect.anchoredPosition=new Vector2(270f,15f);
                var label=labelRoot.AddComponent<TextMeshProUGUI>();
                label.alignment=TextAlignmentOptions.TopLeft; // matches old scene.
                f.hero.dmgTextPrototype=label;
                Call(f.hero,"AlignDamageTextToBody");
                Check(label.transform.parent==f.hero.heroRect,
                    "incoming damage must belong to injured Hero");
                Check(label.alignment==TextAlignmentOptions.Center,
                    "TMP glyphs must be centered, not merely the RectTransform");
                Near(labelRect.anchoredPosition.x,0,"body midpoint anchored X");
                Near(labelRect.anchoredPosition.y,0,"body midpoint anchored Y");
                Near(labelRect.pivot.x,.5f,"text pivot X");
                Near(labelRect.pivot.y,.5f,"text pivot Y");
                Vector3 expected=f.hero.heroRect.TransformPoint(
                    new Vector3(f.hero.heroRect.rect.center.x,
                                f.hero.heroRect.rect.center.y,0f));
                Near(labelRect.position.x,expected.x,"visual middle X");
                Near(labelRect.position.y,expected.y,"visual middle Y");
                Time.deltaTime=.001f;
                f.hero.ShowDamage(17,1f);
                Near(labelRect.position.x,expected.x,"damage initial X");
                Check(label.text=="-17","damage text retained");

                var routine=Get<Coroutine>(f.hero,"fadeDmgCoroutine");
                Time.deltaTime=.25f;
                Check(routine.routine.MoveNext(),"float continues");
                Near(labelRect.position.x,expected.x,
                    "received damage only rises, never drifts toward facing side");
                Check(labelRect.position.y>expected.y,"floating text rises");
                f.space.SetPosition(f.hero.heroRect,
                    f.space.Position(f.hero.heroRect)+new Vector2(70f,0f));
                Vector3 expectedAfter=f.hero.heroRect.TransformPoint(
                    new Vector3(f.hero.heroRect.rect.center.x,
                                f.hero.heroRect.rect.center.y,0f));
                Near(labelRect.position.x,expectedAfter.x,
                    "damage label follows actual Hero while moving");
                // Anchored origin must survive changing body dimensions after
                // popup starts; cached localPosition would shift it sideways.
                f.hero.heroRect.sizeDelta=new Vector2(120f,190f);
                Vector3 resized=f.hero.heroRect.TransformPoint(
                    new Vector3(f.hero.heroRect.rect.center.x,
                                f.hero.heroRect.rect.center.y,0f));
                Near(labelRect.position.x,resized.x,
                    "resized Hero keeps popup horizontally centered");
                Near(labelRect.anchoredPosition.x,0,
                    "resize never introduces a horizontal anchor offset");
                f.hero.HideHero();
                Check(label.alpha==0f,"HideHero clears popup");
                Near(labelRect.anchoredPosition.x,0,"hide restores origin X");
                Near(labelRect.anchoredPosition.y,0,"hide restores origin Y");
            }
        });

        Run("0.0.51 multiple Heroes own separate hit popup origins",()=>{
            var f=new Field();
            var firstRoot=new GameObject(true);
            firstRoot.transform.parent=f.ground;
            f.hero.dmgTextPrototype=firstRoot.AddComponent<TextMeshProUGUI>();
            Call(f.hero,"AlignDamageTextToBody");
            f.hero.dmgTextPrototype.alpha=0f;
            var secondary=new GameObject().AddComponent<HeroController>();
            secondary.heroRect=(RectTransform)new GameObject(true).transform;
            secondary.heroRect.parent=f.ground;
            var secondRoot=new GameObject(true);
            secondRoot.transform.parent=f.ground;
            secondary.dmgTextPrototype=secondRoot.AddComponent<TextMeshProUGUI>();
            Call(secondary,"Awake");
            secondary.SpawnHero(false);
            f.space.SetPosition(secondary.heroRect,new Vector2(-140f,0f));
            Time.deltaTime=.001f;
            secondary.ShowDamage(21,-1f);
            Check(secondary.dmgTextPrototype.transform.parent==secondary.heroRect,
                "secondary hit popup is attached to second Hero");
            Check(f.hero.dmgTextPrototype.alpha==0f,
                "secondary popup does not alter primary Hero");
            Near(((RectTransform)secondary.dmgTextPrototype.transform).anchoredPosition.x,0,
                "secondary popup starts at second Hero centre");
        });

        Run("0.0.51 every monster popup originates at exact body centre",()=>{
            foreach(float scale in new[]{.5f,1f,2f})
            {
                var f=new Field(scale);
                var monsterRoot=new GameObject(true);
                monsterRoot.transform.parent=f.ground;
                var monsterRect=(RectTransform)monsterRoot.transform;
                monsterRect.sizeDelta=new Vector2(76f,110f);
                monsterRect.pivot=new Vector2(.5f,0f);
                monsterRect.anchoredPosition=new Vector2(-175f,30f);
                var hpRoot=new GameObject(true);
                hpRoot.transform.parent=monsterRoot.transform;
                var labelRoot=new GameObject(true);
                labelRoot.transform.parent=hpRoot.transform;
                var labelRect=(RectTransform)labelRoot.transform;
                labelRect.anchoredPosition=new Vector2(-90f,250f);
                var label=labelRoot.AddComponent<TextMeshProUGUI>();
                label.alignment=TextAlignmentOptions.TopLeft;
                var monster=monsterRoot.AddComponent<MonsterController>();
                monster.dmgTextPrototype=label;
                Call(monster,"Awake");
                Call(monster,"OnEnable");
                Check(labelRect.parent==monsterRect,
                    "monster label detached from HP bar and attached to body");
                Check(label.alignment==TextAlignmentOptions.Center,
                    "monster TMP glyphs centered rather than top-left");
                Near(labelRect.anchoredPosition.x,0,"monster initial anchored X");
                Near(labelRect.anchoredPosition.y,0,"monster initial anchored Y");
                Near(labelRect.pivot.x,.5f,"monster text pivot X");
                Near(labelRect.pivot.y,.5f,"monster text pivot Y");
                Vector3 centre=monsterRect.TransformPoint(
                    new Vector3(monsterRect.rect.center.x,monsterRect.rect.center.y,0f));
                Near(labelRect.position.x,centre.x,"monster initial world X");
                Near(labelRect.position.y,centre.y,"monster initial world Y");
                Time.deltaTime=.001f;
                monster.ShowDamage(43,-1f);
                Check(label.text=="-43","monster amount preserved");
                Near(labelRect.anchoredPosition.x,0,
                    "monster damage starts horizontally at centre");
                var routine=Get<Coroutine>(monster,"fadeDmgCoroutine");
                Time.deltaTime=.25f;
                Check(routine.routine.MoveNext(),"monster float continues");
                Near(labelRect.position.x,centre.x,
                    "monster damage floats only upward, never left");
                Check(labelRect.position.y>centre.y,"monster hit text rises");

                // Simulate future monster resizing during animation.
                monsterRect.sizeDelta=new Vector2(125f,180f);
                Vector3 resized=monsterRect.TransformPoint(
                    new Vector3(monsterRect.rect.center.x,monsterRect.rect.center.y,0f));
                Near(labelRect.position.x,resized.x,"resized monster stays centered");
                monster.ShowDamage(13,1f);
                Near(labelRect.anchoredPosition.x,0,
                    "new hit does not inherit previous horizontal offset");
                Call(monster,"OnDisable");
                Call(monster,"OnEnable");
                Near(labelRect.anchoredPosition.x,0,"pool reuse clears offset X");
                Near(labelRect.anchoredPosition.y,0,"pool reuse clears offset Y");
                Check(label.alpha==0f,"pool reuse clears previous damage text");
            }
        });
    }
}
