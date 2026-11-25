using DeckRogue.Client;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeckRogue.Boot
{
    /// <summary>
    /// DJ-001 + DJ-117 Boot shell: version readout → AudioUnlock gesture → MainMenu.
    /// Only the scene named Boot mounts this overlay.
    /// </summary>
    public static class BootVersionOverlay
    {
        private const string CanvasName = "BootVersionCanvas";
        private const float AutoAdvanceSeconds = 1.6f;

        private enum Phase
        {
            Boot,
            AudioUnlock
        }

        private static Phase _phase;
        private static float _bootShownAt;
        private static bool _advanced;
        private static GameObject _bootPanel;
        private static GameObject _unlockPanel;
        private static GameObject _ticker;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterShell()
        {
            BootShell.Mount = Mount;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterFirstScene()
        {
            Mount();
        }

        /// <summary>Idempotent Boot overlay mount (first scene + ShellSceneRouter).</summary>
        public static void Mount()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != ShellScenes.Boot)
                return;
            if (GameObject.Find(CanvasName) != null)
                return;

            GameSaveService.Ensure();
            ShellUi.EnsureEventSystem();

            _phase = Phase.Boot;
            _advanced = false;
            _bootShownAt = Time.realtimeSinceStartup;

            Canvas canvas = ShellUi.CreateCanvas(CanvasName, 100);
            Transform root = canvas.transform;

            ShellUi.FullBleed(root, "Backdrop", new Color(0.04f, 0.05f, 0.08f, 1f));

            Color ink = new Color(0.86f, 0.95f, 1f, 1f);
            Color face = new Color(0.16f, 0.2f, 0.3f, 1f);
            Color accent = new Color(0.22f, 0.55f, 0.5f, 1f);

            _bootPanel = new GameObject("BootPanel");
            _bootPanel.transform.SetParent(root, false);
            RectTransform bootRect = _bootPanel.AddComponent<RectTransform>();
            bootRect.anchorMin = Vector2.zero;
            bootRect.anchorMax = Vector2.one;
            bootRect.offsetMin = Vector2.zero;
            bootRect.offsetMax = Vector2.zero;

            ShellUi.Label(
                _bootPanel.transform,
                "VersionLabel",
                string.Join(
                    "\n",
                    "DECK//ROGUE",
                    "version " + Application.version,
                    "Unity " + Application.unityVersion,
                    scene.name,
                    Application.platform.ToString()),
                48,
                TextAnchor.MiddleCenter,
                ink,
                new Vector2(0.1f, 0.28f),
                new Vector2(0.9f, 0.78f));

            ShellUi.Button(
                _bootPanel.transform,
                "Continue",
                "Continue",
                32,
                new Vector2(0.32f, 0.12f),
                new Vector2(0.68f, 0.22f),
                accent,
                ink,
                AdvanceToUnlock);

            _unlockPanel = new GameObject("AudioUnlockPanel");
            _unlockPanel.transform.SetParent(root, false);
            RectTransform unlockRect = _unlockPanel.AddComponent<RectTransform>();
            unlockRect.anchorMin = Vector2.zero;
            unlockRect.anchorMax = Vector2.one;
            unlockRect.offsetMin = Vector2.zero;
            unlockRect.offsetMax = Vector2.zero;
            _unlockPanel.SetActive(false);

            ShellUi.Label(
                _unlockPanel.transform,
                "UnlockCopy",
                "Enable audio\n(gesture required on Web)",
                42,
                TextAnchor.MiddleCenter,
                ink,
                new Vector2(0.1f, 0.35f),
                new Vector2(0.9f, 0.7f));

            ShellUi.Button(
                _unlockPanel.transform,
                "EnableAudio",
                "Enable audio",
                34,
                new Vector2(0.28f, 0.14f),
                new Vector2(0.72f, 0.26f),
                accent,
                ink,
                UnlockAndEnterMenu);

            // Also offer a plain Continue label path for desktop.
            ShellUi.Button(
                _unlockPanel.transform,
                "ContinueDesktop",
                "Continue",
                26,
                new Vector2(0.35f, 0.05f),
                new Vector2(0.65f, 0.12f),
                face,
                ink,
                UnlockAndEnterMenu);

            _ticker = new GameObject("BootFlowTicker");
            _ticker.transform.SetParent(root, false);
            _ticker.AddComponent<BootFlowTicker>();
        }

        private static void AdvanceToUnlock()
        {
            if (_advanced && _phase == Phase.AudioUnlock)
                return;
            _advanced = true;
            _phase = Phase.AudioUnlock;
            if (_bootPanel != null)
                _bootPanel.SetActive(false);
            if (_unlockPanel != null)
                _unlockPanel.SetActive(true);
        }

        private static void UnlockAndEnterMenu()
        {
            // Gesture / click unlocks the audio context for Web; desktop proceeds the same path.
            AudioListener.pause = false;
            GameSaveService.Ensure().ApplyRuntimeSettings();
            SceneManager.LoadScene(ShellScenes.MainMenu);
        }

        /// <summary>Auto-advance Boot → AudioUnlock after a short delay if the player does not tap.</summary>
        private sealed class BootFlowTicker : MonoBehaviour
        {
            private void Update()
            {
                if (_phase != Phase.Boot || _advanced)
                    return;
                if (Time.realtimeSinceStartup - _bootShownAt < AutoAdvanceSeconds)
                    return;
                AdvanceToUnlock();
            }
        }
    }
}
