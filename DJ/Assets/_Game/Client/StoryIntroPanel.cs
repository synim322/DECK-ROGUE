using DeckRogue.Core.Run;
using UnityEngine;

namespace DeckRogue.Client
{
    /// <summary>
    /// Launch intro: reads <see cref="CampaignStory.IntroPages"/> with typewriter before the menu unlocks.
    /// </summary>
    public static class StoryIntroPanel
    {
        private static bool _shownThisSession;

        /// <summary>
        /// Shows the campaign intro once per app session on MainMenu.
        /// Returns true if a panel was opened (caller should defer other overlays).
        /// </summary>
        public static bool TryShow(Transform menuRoot, System.Action onFinished)
        {
            if (menuRoot == null)
                return false;
            if (_shownThisSession)
                return false;
            if (menuRoot.Find("StoryBeatPanel") != null)
                return true;

            _shownThisSession = true;
            new StoryBeatPanel(menuRoot, CampaignStory.IntroPages, onFinished, "CAMPAIGN INTRO");
            return true;
        }

        /// <summary>Test / debug: allow intro again this session.</summary>
        public static void ResetSessionFlag()
        {
            _shownThisSession = false;
        }
    }
}
