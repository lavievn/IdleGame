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
                Vector3 before=labelRect.position;
                f.space.SetPosition(f.hero.heroRect,
                    f.space.Position(f.hero.heroRect)+new Vector2(70f,0f));
                Vector3 expectedAfter=f.hero.heroRect.TransformPoint(
                    new Vector3(f.hero.heroRect.rect.center.x,
                                f.hero.heroRect.rect.center.y,0f));
                Near(labelRect.position.x,expectedAfter.x,
                    "damage label follows actual Hero while moving");
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
    }
}
