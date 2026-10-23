using NUnit.Framework;
using UnityEngine;

namespace IdleGame.Tests.Presentation.PlayMode
{
    // Only loads Resources. No scene/manager activation, save access or I/O.
    [TestFixture]
    public sealed class PlainsPngImportTests
    {
        [TestCase("Ground",530)]
        [TestCase("Ground2",724)]
        [TestCase("Ground3",724)]
        [TestCase("sky",274)]
        public void Plains_SingleFullRectSpritePreservesSourceCanvas(string name,int height)
        {
            string path="Environment/Plains/"+name;
            var sprites=Resources.LoadAll<Sprite>(path);
            Assert.That(sprites.Length,Is.EqualTo(1),"Single Sprite; no automatic alpha slicing.");
            var sprite=sprites[0];
            Assert.That(sprite,Is.Not.Null);
            Assert.That(sprite.rect.x,Is.EqualTo(0f));
            Assert.That(sprite.rect.y,Is.EqualTo(0f));
            Assert.That(sprite.rect.width,Is.EqualTo(2172f));
            Assert.That(sprite.rect.height,Is.EqualTo((float)height));
            Assert.That(sprite.vertices.Length,Is.EqualTo(4),"Full rectangle, including intended alpha regions.");
            Assert.That(sprite.texture.width,Is.EqualTo(2172));
            Assert.That(sprite.texture.height,Is.EqualTo(height));
            Assert.That(sprite.texture.mipmapCount,Is.EqualTo(1));
            Assert.That(sprite.texture.isReadable,Is.False);
            Assert.That(sprite.texture.wrapMode,Is.EqualTo(TextureWrapMode.Clamp));
        }
    }
}
