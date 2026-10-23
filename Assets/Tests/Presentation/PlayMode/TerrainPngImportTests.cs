using NUnit.Framework;
using UnityEngine;

namespace IdleGame.Tests.Presentation.PlayMode
{
    // Resource import only: no managers, scene lifecycle hooks or save I/O.
    [TestFixture]
    public sealed class TerrainPngImportTests
    {
        [TestCase("Forest","ground_j")]
        [TestCase("Forest","ground_j1")]
        [TestCase("Forest","ground_j2")]
        [TestCase("Forest","sky_j")]
        public void Terrain_ImportKeepsFullSourceCanvas(string group,string name)
        {
            var sprites=Resources.LoadAll<Sprite>("Environment/"+group+"/"+name);
            Assert.That(sprites.Length,Is.EqualTo(1));
            var sprite=sprites[0];
            Assert.That(sprite,Is.Not.Null);
            Assert.That(sprite.rect.x,Is.EqualTo(0f));
            Assert.That(sprite.rect.y,Is.EqualTo(0f));
            Assert.That(sprite.rect.width,Is.EqualTo(2172f));
            Assert.That(sprite.rect.height,Is.EqualTo(724f));
            Assert.That(sprite.vertices.Length,Is.EqualTo(4));
            Assert.That(sprite.texture.width,Is.EqualTo(2172));
            Assert.That(sprite.texture.height,Is.EqualTo(724));
            Assert.That(sprite.texture.mipmapCount,Is.EqualTo(1));
            Assert.That(sprite.texture.isReadable,Is.False);
            Assert.That(sprite.texture.wrapMode,Is.EqualTo(TextureWrapMode.Clamp));
        }
    }
}
