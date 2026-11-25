using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeckRogue.Client
{
    /// <summary>
    /// Player builds only invoke RuntimeInitialize AfterSceneLoad for the first scene.
    /// Subsequent SceneManager.LoadScene calls need SceneManager.sceneLoaded — otherwise
    /// MainMenu / AudioSpike mount as an empty camera scene (blank map after Continue).
    /// </summary>
    public static class ShellSceneRouter
    {
        private static bool _hooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _hooked = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook()
        {
            if (_hooked)
                return;
            _hooked = true;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return;
            Mount(scene.name);
        }

        /// <summary>Idempotent mount for Boot / MainMenu / AudioSpike overlays.</summary>
        public static void Mount(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return;

            // Prefer the scene name from sceneLoaded — GetActiveScene can lag one frame in players.
            if (string.Equals(sceneName, ShellScenes.Boot, StringComparison.Ordinal))
            {
                BootShell.Mount();
                return;
            }

            if (string.Equals(sceneName, ShellScenes.MainMenu, StringComparison.Ordinal))
            {
                MainMenuOverlay.MountForScene(sceneName);
                return;
            }

            if (string.Equals(sceneName, ShellScenes.AudioSpike, StringComparison.Ordinal))
                GameplayShell.Mount();
        }
    }

    /// <summary>Boot overlay entry used by <see cref="ShellSceneRouter"/> (avoids circular static init).</summary>
    public static class BootShell
    {
        public static Action Mount = () => { };
    }

    /// <summary>AudioSpike host entry used by <see cref="ShellSceneRouter"/>.</summary>
    public static class GameplayShell
    {
        public static Action Mount = () => { };
    }
}
