using System;
using System.Collections.Generic;
using DeckRogue.Core.Run;

namespace DeckRogue.Core.Save
{
    /// <summary>
    /// Pure Act1 ↔ SaveGame wiring. Mutates the document; never performs I/O.
    /// Client calls after phase changes, then flushes when any trigger is returned.
    /// </summary>
    public static class Act1SaveSync
    {
        private static readonly SaveFlushTrigger[] Empty = Array.Empty<SaveFlushTrigger>();

        /// <summary>
        /// After an Act1 phase transition: capture meta growth; at FinalResult, raise best rep.
        /// Idempotent when polled with unchanged state.
        /// </summary>
        public static SaveFlushTrigger[] Apply(
            SaveGameDocument document,
            MetaUnlocks meta,
            RunPhase phase,
            double runReputation)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (meta == null)
                throw new ArgumentNullException(nameof(meta));

            var triggers = new List<SaveFlushTrigger>(2);

            if (MetaDiffers(document, meta))
            {
                document.CaptureMeta(meta);
                triggers.Add(SaveFlushTrigger.MetaUnlockChanged);
            }

            if (phase == RunPhase.FinalResult)
            {
                int rep = ToReputationInt(runReputation);
                if (document.TryUpdateBestReputation(rep))
                    triggers.Add(SaveFlushTrigger.RunEndedBestReputation);
            }

            if (triggers.Count == 0)
                return Empty;
            return triggers.ToArray();
        }

        /// <summary>Convenience: sync from a live director readout.</summary>
        public static SaveFlushTrigger[] Apply(SaveGameDocument document, Act1RunDirector director)
        {
            if (director == null)
                throw new ArgumentNullException(nameof(director));
            Act1Readout readout = director.Readout;
            return Apply(document, director.Meta, readout.Phase, director.Reputation);
        }

        private static bool MetaDiffers(SaveGameDocument document, MetaUnlocks meta)
        {
            IReadOnlyCollection<string> metaIds = meta.UnlockedTrackIds;
            List<string> docIds = document.UnlockedTrackIds;
            if (docIds == null)
                return metaIds.Count > 0;
            if (docIds.Count != metaIds.Count)
                return true;

            for (int i = 0; i < docIds.Count; i++)
            {
                string id = docIds[i];
                if (string.IsNullOrEmpty(id) || !meta.Contains(id))
                    return true;
            }

            return false;
        }

        private static int ToReputationInt(double runReputation)
        {
            if (double.IsNaN(runReputation) || double.IsInfinity(runReputation) || runReputation <= 0d)
                return 0;
            if (runReputation >= int.MaxValue)
                return int.MaxValue;
            return (int)runReputation;
        }
    }
}
