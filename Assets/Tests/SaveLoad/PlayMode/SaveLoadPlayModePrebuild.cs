#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
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
            if (SceneManager.sceneCount != 1)
                throw new InvalidOperationException("Open exactly one unsaved EMPTY scene before running W1.8a.");
            Scene scene = SceneManager.GetSceneAt(0);
            if (!scene.isLoaded || !string.IsNullOrEmpty(scene.path) || scene.rootCount != 0)
                throw new InvalidOperationException("W1.8a requires an unsaved EMPTY scene: no Camera, Light or gameplay roots.");
            SaveLoadPlayModeFixture.RequireNoSaveOrGameManager();
        }
    }
}
#endif
