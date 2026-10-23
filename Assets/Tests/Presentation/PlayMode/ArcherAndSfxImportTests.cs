using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace IdleGame.Tests.Presentation.PlayMode
{
    // Import verification on native Unity resources; no managers, scene hooks,
    // AudioSource.Play, filesystem writes or player save access.
    [TestFixture]
    public sealed class ArcherAndSfxImportTests
    {
        [TestCase("Normal")]
        [TestCase("Attack")]
        [TestCase("Hit")]
        public void Archer_ThreeOrderedTexturesKeepRegisteredCanvas(string pose)
        {
            var unique=new HashSet<Texture2D>();
            for(int frame=1;frame<=3;frame++)
            {
                var texture=Resources.Load<Texture2D>("CultivationSprites/Archer_"+pose+"_"+frame);
                Assert.That(texture,Is.Not.Null);
                Assert.That(unique.Add(texture),Is.True,"Different frame resource for each index.");
                Assert.That(texture.width,Is.EqualTo(900));
                Assert.That(texture.height,Is.EqualTo(600));
                Assert.That(texture.mipmapCount,Is.EqualTo(1));
            }
        }

        [TestCase("Sword",18)]
        [TestCase("Shoot",3)]
        [TestCase("Magic",8)]
        public void Sfx_AllExpectedClipsImportAsReadyStereo48k(string group,int count)
        {
            var clips=Resources.LoadAll<AudioClip>("CombatSfx/"+group);
            Assert.That(clips.Length,Is.EqualTo(count));
            var names=new HashSet<string>();
            foreach(var clip in clips)
            {
                Assert.That(clip,Is.Not.Null);
                Assert.That(names.Add(clip.name),Is.True);
                Assert.That(clip.channels,Is.EqualTo(2));
                Assert.That(clip.frequency,Is.EqualTo(48000));
                Assert.That(clip.samples,Is.GreaterThan(0));
                Assert.That(clip.length,Is.InRange(.1f,3f));
                Assert.That(clip.loadState,Is.EqualTo(AudioDataLoadState.Loaded));
            }
        }
    }
}
