using System;
using UnityEngine;
using TMPro;

partial class MotionRegression
{
    static Vector2 BarPoint(AudioVolumeBar bar,float fraction)
    {
        var rect=bar.Track.rect;
        return (Vector2)bar.Track.TransformPoint(new Vector3(rect.xMin+rect.width*fraction,0,0));
    }
    static void DualVolumeTests()
    {
        // DesktopHotfixFixture can query the catalog during New/Continue setup.
        // Supply the real resource before its static loader can cache a miss.
        Resources.Register("WorldMapCatalog",new TextAsset(System.IO.File.ReadAllText("Assets/Resources/WorldMapCatalog.txt")));
        Resources.Register("WorldMapPolygonSeeds",new TextAsset(System.IO.File.ReadAllText("Assets/Resources/WorldMapPolygonSeeds.txt")));
        string bgmKey=AudioVolumeSettings.Key(GameAudioChannel.Bgm),sfxKey=AudioVolumeSettings.Key(GameAudioChannel.Sfx);
        bool hadBgm=PlayerPrefs.HasKey(bgmKey),hadSfx=PlayerPrefs.HasKey(sfxKey);
        float oldBgm=MapBgmController.CurrentVolume,oldSfx=AudioVolumeSettings.Get(GameAudioChannel.Sfx);
        try
        {
            Run("audio channels retain legacy BGM and default/persist SFX independently",()=>{
                PlayerPrefs.SetFloat(bgmKey,.37f);PlayerPrefs.DeleteKey(sfxKey);
                Near(MapBgmController.CurrentVolume,.37f,"legacy key unchanged");
                Near(AudioVolumeSettings.Get(GameAudioChannel.Sfx),1,"new channel default");
                AudioVolumeSettings.Set(GameAudioChannel.Sfx,.24f);
                Near(MapBgmController.CurrentVolume,.37f,"SFX cannot reset BGM");
                MapBgmController.SetVolume(.81f);
                Near(AudioVolumeSettings.Get(GameAudioChannel.Sfx),.24f,"BGM cannot reset SFX");
                Near(PlayerPrefs.GetFloat(bgmKey),.81f,"persistent BGM");Near(PlayerPrefs.GetFloat(sfxKey),.24f,"persistent SFX");
                AudioVolumeSettings.RestoreDefaults();Near(MapBgmController.CurrentVolume,.6f,"legacy BGM default");
                Near(AudioVolumeSettings.Get(GameAudioChannel.Sfx),1,"SFX reset");
            });
            foreach(int width in new[]{600,800,1150})
            Run("Home/gameplay dual sliders share values, geometry and native drag route / "+width,()=>{
                using(var f=new DesktopHotfixFixture(width,true)) {
                    f.manager.preGameUI.SetActive(true);f.menu.RestoreMenu();f.menu.SetOpeningMenuReady(true);Call(f.menu,"OpenSettings");
                    var bgm=Get<AudioVolumeBar>(f.menu,"bgmVolumeBar");var sfx=Get<AudioVolumeBar>(f.menu,"sfxVolumeBar");
                    Check(Mathf.Abs(bgm.Track.anchoredPosition.y-sfx.Track.anchoredPosition.y)>=48,"separate rows");
                    var before=AudioVolumeSettings.Get(GameAudioChannel.Sfx);int saves=PlayerPrefs.SaveCount;
                    Check(f.ui.TryBeginOpeningVolumeDrag(BarPoint(bgm,.22f)),"Home BGM routes");
                    f.ui.DragOpeningVolume(BarPoint(bgm,.35f));Near(MapBgmController.CurrentVolume,.35f,"live BGM");
                    Near(AudioVolumeSettings.Get(GameAudioChannel.Sfx),before,"SFX untouched");Check(PlayerPrefs.SaveCount==saves,"no flush per drag frame");
                    f.ui.EndOpeningVolumeDrag();Check(PlayerPrefs.SaveCount==saves+1,"flush on release");
                    Check(f.ui.TryBeginOpeningVolumeDrag(BarPoint(sfx,.76f)),"Home SFX routes");f.ui.EndOpeningVolumeDrag();
                    Near(AudioVolumeSettings.Get(GameAudioChannel.Sfx),.76f,"Home SFX value");
                    Call(f.ui,"LayoutReadableUI");Near(sfx.Fill.rect.width/sfx.Track.rect.width,.76f,"layout keeps current SFX fill");
                    Near(sfx.Fill.anchoredPosition.x,0,"fill starts at anchored left, no extra half-width shift");
                    f.menu.EnterGameplayImmediately();
                    var panel=Get<MusicVolumePanelUI>(f.ui,"musicVolumePanel");panel.Toggle();
                    var gameBgm=Get<AudioVolumeBar>(panel,"bgmBar");var gameSfx=Get<AudioVolumeBar>(panel,"sfxBar");
                    Near(gameSfx.Fill.rect.width/gameSfx.Track.rect.width,.76f,"Gameplay receives Home setting");
                    Check(f.ui.TryBeginOpeningVolumeDrag(BarPoint(gameSfx,.12f)),"Gameplay/native SFX route");f.ui.EndOpeningVolumeDrag();
                    Near(AudioVolumeSettings.Get(GameAudioChannel.Sfx),.12f,"Gameplay SFX updated");Near(MapBgmController.CurrentVolume,.35f,"independent BGM");
                    Check(f.ui.TryBeginOpeningVolumeDrag(BarPoint(gameBgm,.9f)),"Gameplay BGM route");f.ui.EndOpeningVolumeDrag();
                    Near(MapBgmController.CurrentVolume,.9f,"Gameplay BGM updated");
                    var panelRect=Get<RectTransform>(panel,"root");
                    foreach(var bar in new[]{gameBgm,gameSfx}) {
                        var local=panelRect.InverseTransformPoint(bar.Track.position);
                        Check(local.x-bar.Track.rect.width*.5f>=panelRect.rect.xMin && local.x+bar.Track.rect.width*.5f<=panelRect.rect.xMax,"track inside panel X");
                        Check(local.y-bar.Track.rect.height*.5f>=panelRect.rect.yMin && local.y+bar.Track.rect.height*.5f<=panelRect.rect.yMax,"track inside panel Y");
                    }
                    panel.Hide();f.manager.preGameUI.SetActive(true);f.menu.RestoreMenu();f.menu.SetOpeningMenuReady(true);Call(f.menu,"OpenSettings");
                    Call(f.menu,"Update");Near(sfx.Fill.rect.width/sfx.Track.rect.width,.12f,"Home receives Gameplay setting");
                    Near(bgm.Fill.rect.width/bgm.Track.rect.width,.9f,"Home BGM synchronized");
                    Call(f.menu,"RestoreDefaultSettings");Near(AudioVolumeSettings.Get(GameAudioChannel.Sfx),1,"Home reset both channels");
                    Near(MapBgmController.CurrentVolume,.6f,"BGM reset still compatible");
                }
            });
            Run("audio BGM zero/SFX one and inverse; volume changes preserve BGM playhead and fade clock",()=>{
                using(var clips=new SfxResources()) {
                    var f=new Field();var previous=MapBgmController.Instance;
                    if(previous!=null)Call(previous,"OnDestroy");
                    var music=new GameObject("test BGM").AddComponent<MapBgmController>();
                    float oldUnscaled=Time.unscaledDeltaTime;
                    try {
                        Call(music,"Awake");var gm=new GameObject().AddComponent<GameManager>();gm.preGameUI=new GameObject();
                        Set(music,"gameManager",gm);var opening=new AudioClip();Set(music,"catalog",new MapBgmCatalog {opening=opening});
                        AudioVolumeSettings.Set(GameAudioChannel.Bgm,0);AudioVolumeSettings.Set(GameAudioChannel.Sfx,1);
                        Time.unscaledDeltaTime=5;Call(music,"Update");
                        var sources=Get<AudioSource[]>(music,"sources");var active=sources[Get<int>(music,"activeIndex")];
                        active.time=42;Near(active.volume,0,"BGM muted");
                        Check(Bus(f).TryPlay(f.combat.CurrentHeroCombatState,TuTienCore.AttackMode.Melee),"SFX event");
                        Check(Sources(f)[0].volume>0,"SFX audible gain despite BGM zero");
                        AudioVolumeSettings.Set(GameAudioChannel.Bgm,1);AudioVolumeSettings.Set(GameAudioChannel.Sfx,0);
                        float fade=Get<float>(music,"elapsed");Time.unscaledDeltaTime=.1f;Call(music,"Update");Call(Bus(f),"Update");
                        Check(active.volume>0,"BGM enabled");Near(active.time,42,"no music restart");Near(Get<float>(music,"elapsed"),fade+.1f,"fade not reset");
                        foreach(var source in Sources(f))Near(source.volume,0,"SFX independently muted");
                    }
                    finally {
                        Call(music,"OnDestroy");Time.unscaledDeltaTime=oldUnscaled;
                        typeof(MapBgmController).GetField("<Instance>k__BackingField",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).SetValue(null,previous);
                    }
                }
            });
            Run("gameplay volume drag cannot bypass reset confirmation modal",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    var panel=Get<MusicVolumePanelUI>(f.ui,"musicVolumePanel");panel.Toggle();
                    var bar=Get<AudioVolumeBar>(panel,"sfxBar");var point=BarPoint(bar,.1f);
                    f.manager.OnResetClicked();float before=AudioVolumeSettings.Get(GameAudioChannel.Sfx);
                    Check(!f.ui.TryBeginOpeningVolumeDrag(point),"modal blocks underlying volume input");
                    Near(AudioVolumeSettings.Get(GameAudioChannel.Sfx),before,"setting untouched");f.manager.OnCancelResetClicked();
                }
            });
        }
        finally
        {
            if(hadBgm)PlayerPrefs.SetFloat(bgmKey,oldBgm);else PlayerPrefs.DeleteKey(bgmKey);
            if(hadSfx)PlayerPrefs.SetFloat(sfxKey,oldSfx);else PlayerPrefs.DeleteKey(sfxKey);
        }
    }
}
