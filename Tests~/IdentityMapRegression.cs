using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TuTienCore;

partial class MotionRegression
{
    static void IdentityMapTests()
    {
        Run("composed names use original gender pool and one to three syllables",()=>{
            foreach(GenderType gender in Enum.GetValues(typeof(GenderType))) {
                var names=(string[])typeof(NameDatabase).GetField(gender==GenderType.Nam?"MaleNames":"FemaleNames",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).GetValue(null);
                var vocabulary=new HashSet<string>();foreach(var name in names)foreach(var word in name.Split(' '))vocabulary.Add(word);
                var lengths=new HashSet<int>();bool novel=false;
                for(int i=0;i<1000;i++) {string name=NameDatabase.GetRandomName(gender);var words=name.Split(' ');lengths.Add(words.Length);
                    Check(words.Length>=1&&words.Length<=3,"bounded syllables");foreach(var word in words)Check(vocabulary.Contains(word),"word from correct gender list");
                    if(Array.IndexOf(names,name)<0)novel=true;
                }
                Check(lengths.Count==3&&novel,"all lengths and novel combinations generated");
            }
        });
        Run("root count follows 60 25 10 5 and mixed race always has two distinct roots",()=>{
            int[] counts=new int[4];for(int roll=0;roll<100;roll++)counts[SynergyMath.RootCount(roll,RaceType.NhanToc)-1]++;
            Check(counts[0]==60&&counts[1]==25&&counts[2]==10&&counts[3]==5,"exact configured distribution");
            var races=new HashSet<RaceType>();
            for(int i=0;i<1000;i++) {var race=SynergyMath.GenerateRandomRace();races.Add(race);var roots=SynergyMath.GenerateRandomRoots(race);
                Check(roots.Count>=1&&roots.Count<=4,"count bounded");Check(new HashSet<ElementType>(roots).Count==roots.Count,"no repeated root");
                foreach(var root in roots)Check((int)root<=4 || root==ElementType.Vo,"only five elements generated");if(race==RaceType.ConLai)Check(roots.Count==2,"mixed race two roots");
            }
            Check(races.Count==5,"all existing races generated");
        });
        Run("all elemental counter pairs work both directions and legacy special roots stay neutral",()=>{
            var cycle=new[]{ElementType.Kim,ElementType.Moc,ElementType.Tho,ElementType.Thuy,ElementType.Hoa};
            for(int i=0;i<5;i++) {var att=cycle[i];var def=cycle[(i+1)%5];Near(SynergyMath.GetElementalMultiplier(att,def),1.30f,"counter");Near(SynergyMath.GetElementalMultiplier(def,att),.70f,"countered");Near(SynergyMath.GetElementalMultiplier(att,att),1,"same");}
            Near(SynergyMath.GetElementalMultiplier(ElementType.Doc,ElementType.Kim),1,"legacy poison");
            Near(SynergyMath.GetElementalMultiplier(new List<ElementType>{ElementType.Kim,ElementType.Hoa},new List<ElementType>{ElementType.Moc}),(1.30f+1f)/2,"multi root pair average");
            var a=HeroAt(1);var b=HeroAt(1);a.baseDamage=100;a.spiritRoots=new List<ElementType>{ElementType.Kim};b.spiritRoots=new List<ElementType>{ElementType.Moc};
            Check(SynergyMath.Damage(a,b,AttackMode.Melee,1)==130,"actual outgoing counter damage");
            a.spiritRoots=b.spiritRoots;b.spiritRoots=new List<ElementType>{ElementType.Kim};Check(SynergyMath.Damage(a,b,AttackMode.Melee,1)==70,"actual incoming countered damage");
        });
        Run("five waves per map and five normal maps then one hard persist through two cycles",()=>{
            var data=HeroAt(1);data.NormalizeMapProgress();
            for(int completed=1;completed<=65;completed++) {data.CompleteWave();Check(data.mapNumber==1+completed/5,"map increments every five waves");Check(data.completedWavesInMap==completed%5,"wave counter");Check(data.difficulty==(data.mapNumber%6==0?1:0),"hard every sixth map");}
        });
        Run("only clearing the last monster advances one wave, duplicate deaths do not advance",()=>{
            var f=new Field();SaveManager save;string dir;var gm=MenuManager(f,out save,out dir);
            try {var data=HeroAt(1);data.mapProgressVersion=1;data.completedWavesInMap=4;Set(gm,"runtimeHeroData",data);Set(gm,"hasDeployed",true);
                var a=new GameObject();var b=new GameObject();gm.activeMonsters.Add(a);gm.activeMonsters.Add(b);
                gm.OnMonsterDied(a);Check(data.mapNumber==1&&data.completedWavesInMap==4,"partial wave does not count");
                gm.OnMonsterDied(b);Check(data.mapNumber==2&&data.completedWavesInMap==0,"cleared wave counts");gm.OnMonsterDied(b);Check(data.mapNumber==2,"duplicate death ignored");
            }finally{Directory.Delete(dir,true);}
        });
        Run("saved map progress survives load and legacy difficulty starts automatic cycle at map one",()=>{
            var f=new Field();SaveManager save;string dir;MenuManager(f,out save,out dir);
            try {var data=HeroAt(10);data.mapProgressVersion=1;data.mapNumber=12;data.completedWavesInMap=3;data.difficulty=1;
                Check(save.SaveGame(data,SaveSlot.ManualSave1),"save");var read=HeroAt(1);Check(save.LoadGame(read,SaveSlot.ManualSave1),"load");read.NormalizeMapProgress();
                Check(read.mapNumber==12&&read.completedWavesInMap==3&&read.difficulty==1,"exact progression restored");
                File.WriteAllText(Path.Combine(dir,"ManualSave1.json"),"{\"currentLevel\":20,\"difficulty\":1}");Check(save.LoadGame(read,SaveSlot.ManualSave1),"legacy load");read.NormalizeMapProgress();
                Check(read.currentLevel==20&&read.mapNumber==1&&read.completedWavesInMap==0&&read.difficulty==0,"level preserved old manual hard flag migrated");
            }finally{Directory.Delete(dir,true);}
        });
        Run("load continues immediately, preserves identity, corrupt slot leaves current battle intact",()=>{
            var f=new Field();SaveManager save;string dir;var gm=MenuManager(f,out save,out dir);
            try {var data=HeroAt(10);data.entityName="Hàn Tuyết Lôi";data.race=RaceType.MaToc;data.spiritRoots=new List<ElementType>{ElementType.Kim,ElementType.Hoa};data.mapProgressVersion=1;data.mapNumber=6;data.completedWavesInMap=2;
                Check(save.SaveGame(data,SaveSlot.ManualSave1),"identity saved");Check(gm.LoadAndContinue(SaveSlot.ManualSave1),"slot load success");
                var loaded=gm.HeroData;Check(Get<bool>(gm,"hasDeployed")&&!gm.preGameUI.activeSelf,"no extra start confirmation after explicit slot load");
                Check(loaded.entityName==data.entityName&&loaded.race==data.race&&loaded.spiritRoots.Count==2,"identity round trip");
                Check(loaded.mapNumber==6&&loaded.completedWavesInMap==2,"load does not skip current wave");
                File.WriteAllText(Path.Combine(dir,"ManualSave2.json"),"broken");Check(!gm.LoadAndContinue(SaveSlot.ManualSave2),"corrupt load rejected");
                Check(gm.HeroData==loaded&&Get<bool>(gm,"hasDeployed"),"current hero and deployment intact");
                gm.OnResetClicked();Check(gm.HasPendingConfirmation,"reset asks first");gm.OnCancelResetClicked();Check(gm.HeroData==loaded&&save.HasSave(SaveSlot.ManualSave1),"cancel does not reset identity or saves");
            }finally{Directory.Delete(dir,true);}
        });
        Run("magic damage is calculated separately for each target's roots",()=>{
            var f=new Field();var hero=HeroAt(1);hero.baseDamage=100;hero.spiritRoots=new List<ElementType>{ElementType.Kim};f.combat.SetupHeroInfo(hero);f.hero.ChangeAttackMode(2);
            var a=f.Monster(-20);var b=f.Monster(-30);f.Battle(a,b);f.Enemies[0].data.spiritRoots=new List<ElementType>{ElementType.Moc};f.Enemies[1].data.spiritRoots=new List<ElementType>{ElementType.Kim};
            foreach(var enemy in f.Enemies){enemy.data.rootTiers.Clear();enemy.data.rootWeights.Clear();}
            f.hero.TickMovement(f.space,0);Time.deltaTime=2;Call(f.combat,"Update");
            var shots=(System.Collections.IList)typeof(CombatManager).GetField("projectiles",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(f.combat);
            Check(shots.Count==1,"one explosion instead of per-target volley");
            foreach(var e in f.Enemies)e.currentHP=e.maxHP=10000;
            CombatStep(f,1.2f);int counter=10000-f.Enemies[0].currentHP,neutral=10000-f.Enemies[1].currentHP;
            Check(neutral>=212&&neutral<=250&&counter==Mathf.RoundToInt(neutral*1.3f),"one cast evaluates counter matrix separately per victim");
        });
        Run("identity tint mixes fifty percent without accumulating over pooled reuse",()=>{
            var f=new Field();var image=f.hero.heroRect.gameObject.AddComponent<Image>();image.color=new Color(.2f,.4f,.6f,.75f);
            var data=HeroAt(1);data.spiritRoots=new List<ElementType>{ElementType.Hoa};f.hero.SetIdentityVisual(data);
            Near(image.color.r,.6f,"half red");Near(image.color.g,.25f,"half green");Near(image.color.a,.75f,"original opacity kept");
            f.hero.SetIdentityVisual(data);Near(image.color.r,.6f,"no cumulative tint");
            var m=f.Monster(-20);var mi=m.gameObject.AddComponent<Image>();mi.color=new Color(1,1,1,1);m.SetIdentityVisual(data);
            data.spiritRoots=new List<ElementType>{ElementType.Thuy};m.SetIdentityVisual(data);Near(mi.color.r,.55f,"new life uses original color");Near(mi.color.b,1,"water blue");
        });
        Run("event log retains hero name race roots map and damage alongside EXP",()=>{
            var f=new Field();SaveManager save;string dir;var gm=MenuManager(f,out save,out dir);
            try {var data=HeroAt(1);data.entityName="Hàn Tuyết Lôi";data.race=RaceType.MaToc;data.spiritRoots=new List<ElementType>{ElementType.Hoa};data.mapNumber=6;data.difficulty=1;data.NormalizeMapProgress();
                Set(gm,"runtimeHeroData",data);gm.eventLogText=new GameObject(true).AddComponent<TextMeshProUGUI>();gm.UpdateEventLog("Nhận 12 EXP");gm.UpdateEventLog("Quái gây 10 sát thương");
                string text=gm.eventLogText.text;Check(text.Contains("Hàn Tuyết Lôi")&&text.Contains("Ma tộc")&&text.Contains("Hỏa")&&text.Contains(data.mapName),"identity and map persistent");
                Check(text.Contains("12 EXP")&&text.Contains("10 sát thương"),"recent events coexist");
            }finally{Directory.Delete(dir,true);}
        });
    }
}
