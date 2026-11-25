using UnityEditor;
using UnityEditor.SceneManagement;

namespace DeckRogue.EditorTools
{
    public static class AudioSpikeMenu
    {
        [MenuItem("DECK ROGUE/Records/Club tracks")]
        private static void ShowClubTracks()
        {
            ShowRecord("Club/Warehouse Line");
        }

        [MenuItem("DECK ROGUE/Records/Reward tracks")]
        private static void ShowRewardTracks()
        {
            ShowRecord("Rewards/Voltage Alley");
        }

        private static void ShowRecord(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>(
                "Assets/_Game/Editor/Records/" + name + ".wav");
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = clip;
            EditorGUIUtility.PingObject(clip);
        }

        [MenuItem("DECK ROGUE/Open Audio Spike")]
        private static void Open()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/AudioSpike.unity");
        }
    }
}
