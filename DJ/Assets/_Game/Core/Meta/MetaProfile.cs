using System;
using System.Collections.Generic;
using System.Globalization;
using DeckRogue.Core.Run;
using DeckRogue.Core.Save;

namespace DeckRogue.Core.Meta
{
    /// <summary>
    /// Hub wallet, shop, and dog walk on the caller's <see cref="SaveGameDocument"/>.
    /// Mutates that instance. Does not copy it and does not read or write Hype.
    /// </summary>
    public sealed class MetaProfile
    {
        public const int WalkCoins = 4;

        private readonly SaveGameDocument _document;

        public MetaProfile(SaveGameDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            _document = document;
            EnsureLists();
        }

        public bool WalkAvailable
        {
            get { return !_document.WalkUsedThisDay; }
        }

        public DeckKit EquippedKit
        {
            get { return DeckKit.For(_document.EquippedDeckId); }
        }

        public bool Owns(string id)
        {
            return Contains(_document.OwnedDeckIds, id) || Contains(_document.OwnedItemIds, id);
        }

        /// <summary>
        /// Pays a finished set once. Key is <c>gig:{runId}:{setIndex}</c>.
        /// Empty run id, in-progress, and a repeated key add nothing.
        /// </summary>
        public bool TryClaimGig(string runId, int setIndex, SetOutcome outcome, int hype, out int coins)
        {
            coins = 0;
            if (string.IsNullOrEmpty(runId))
                return false;
            if (outcome != SetOutcome.Win && outcome != SetOutcome.Lose)
                return false;

            EnsureLists();
            string key = "gig:" + runId + ":" + setIndex.ToString(CultureInfo.InvariantCulture);
            if (Contains(_document.ClaimedGigKeys, key))
                return false;

            int payout = GigPayout.Coins(outcome, hype);
            _document.MetaCoins += payout;
            _document.ClaimedGigKeys.Add(key);
            coins = payout;
            return true;
        }

        /// <summary>
        /// Charges the catalog price once and appends the id.
        /// Duplicate, unknown, or short wallet leaves the document alone.
        /// </summary>
        public bool TryPurchase(string id)
        {
            int price;
            if (!DeckCatalog.TryGetPrice(id, out price))
                return false;
            if (Owns(id))
                return false;
            if (_document.MetaCoins < price)
                return false;

            EnsureLists();
            _document.MetaCoins -= price;
            if (DeckCatalog.IsDeck(id))
                _document.OwnedDeckIds.Add(id);
            else
                _document.OwnedItemIds.Add(id);
            return true;
        }

        /// <summary>Wears a deck the save already owns. Items and unknown ids stay off the platter.</summary>
        public bool TryEquip(string deckId)
        {
            if (!DeckCatalog.IsDeck(deckId))
                return false;
            if (!Contains(_document.OwnedDeckIds, deckId))
                return false;

            _document.EquippedDeckId = deckId;
            return true;
        }

        /// <summary>One paid walk per hub day. +4 coins and the day is spent. A second call pays 0.</summary>
        public bool TryCompleteWalk(out int coins)
        {
            coins = 0;
            if (!WalkAvailable)
                return false;

            _document.MetaCoins += WalkCoins;
            _document.WalkUsedThisDay = true;
            coins = WalkCoins;
            return true;
        }

        /// <summary>Player quit the walk early. Day and wallet stay as they were.</summary>
        public void AbandonWalk()
        {
        }

        /// <summary>
        /// Once per finished night. Same run id or an empty id does nothing.
        /// Otherwise the hub day advances and the walk is free again.
        /// </summary>
        public bool SettleNight(string runId)
        {
            if (string.IsNullOrEmpty(runId))
                return false;
            if (string.Equals(_document.SettledRunId, runId, StringComparison.Ordinal))
                return false;

            _document.HubDay += 1;
            _document.WalkUsedThisDay = false;
            _document.SettledRunId = runId;
            return true;
        }

        /// <summary>
        /// Opens the basement after the first finished club.
        /// Returns true only when the flag flips from locked to open.
        /// </summary>
        public bool TryUnlockHub()
        {
            if (_document.HubUnlocked)
                return false;

            _document.HubUnlocked = true;
            return true;
        }

        private void EnsureLists()
        {
            if (_document.OwnedDeckIds == null)
                _document.OwnedDeckIds = new List<string>();
            if (_document.OwnedItemIds == null)
                _document.OwnedItemIds = new List<string>();
            if (_document.ClaimedGigKeys == null)
                _document.ClaimedGigKeys = new List<string>();
        }

        private static bool Contains(List<string> ids, string id)
        {
            if (ids == null || string.IsNullOrEmpty(id))
                return false;
            for (int i = 0; i < ids.Count; i++)
            {
                if (string.Equals(ids[i], id, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
