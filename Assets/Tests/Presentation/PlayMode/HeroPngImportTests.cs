using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace IdleGame.Tests.Presentation.PlayMode
{
    // Read-only Resources/import checks. No scene, gameplay manager or save I/O.
    public sealed class HeroPngImportTests
    {
        [TestCase("idle",48)] [TestCase("walk",17)]
        [TestCase("run",11)] [TestCase("hit",27)]
        public void SharedHeroProfileLoadsFullResolutionFramesInExportOrder(string clip,int count)
        {
            Type bridge=null;
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                bridge=assembly.GetType("CultivationSpriteBridge",false);
                if(bridge!=null)break;
            }
            Assert.That(bridge,Is.Not.Null);
            var type=bridge.GetNestedType("HeroAnimationProfile",BindingFlags.NonPublic);
            var profile=Activator.CreateInstance(type,true);
            var json=Resources.Load<TextAsset>("HeroAnimation/SharedHero");
            Assert.That(json,Is.Not.Null);
            JsonUtility.FromJsonOverwrite(json.text,profile);
            Assert.That(type.GetField(clip+"Count").GetValue(profile),Is.EqualTo(count));
            Assert.That((float)type.GetField(clip+"Fps").GetValue(profile),Is.GreaterThan(0f));
            string prefix=(string)type.GetField(clip+"Prefix").GetValue(profile);
            for(int i=0;i<count;i++)
            {
                var texture=Resources.Load<Texture2D>(prefix+i.ToString("D5"));
                Assert.That(texture,Is.Not.Null,"Frame "+i);
                Assert.That(texture.width,Is.EqualTo(360));
                Assert.That(texture.height,Is.EqualTo(500));
                Assert.That(texture.mipmapCount,Is.EqualTo(1));
                Assert.That(texture.isReadable,Is.False);
                Assert.That(texture.wrapMode,Is.EqualTo(TextureWrapMode.Clamp));
                Assert.That(texture.filterMode,Is.EqualTo(FilterMode.Bilinear));
            }
            Assert.That(Resources.Load<Texture2D>(prefix+count.ToString("D5")),Is.Null,"No extra frame");
        }
    }
}
