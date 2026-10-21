#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.TestTools;

namespace IdleGame.Tests.SaveLoad.PlayMode
{
    // The Test Framework invokes this in Edit Mode, BEFORE entering Play Mode.
    // Never close/save a user's scene automatically to satisfy this precondition.
    public sealed class SaveLoadPlayModePrebuild : IPrebuildSetup
    {
        public void Setup()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Run W1.8a from the Editor Test Runner before entering Play Mode.");
            // The runner may already have replaced the user's scene with a
            // bootstrap scene. Validate content, not root count, path or names.
            SaveLoadPlayModeFixture.RequireCleanTestEnvironment();
        }
    }
}
#endif
