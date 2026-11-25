using System;
using DeckRogue.Core.Save;

namespace DeckRogue.Core.Meta
{
    /// <summary>
    /// Deck worn at the moment a run starts. Later <see cref="SaveGameDocument.EquippedDeckId"/>
    /// edits do not move this copy. An unknown id resolves as <see cref="DeckCatalog.StarterId"/>.
    /// </summary>
    public sealed class RunDeckLock
    {
        public RunDeckLock(SaveGameDocument document)
            : this(Resolve(document))
        {
        }

        /// <summary>Lock a known deck id. Unknown ids resolve as <see cref="DeckCatalog.StarterId"/>.</summary>
        public static RunDeckLock FromId(string deckId)
        {
            return new RunDeckLock(DeckCatalog.IsDeck(deckId) ? deckId : DeckCatalog.StarterId);
        }

        private RunDeckLock(string deckId)
        {
            DeckId = deckId;
            Kit = DeckKit.For(deckId);
        }

        private static string Resolve(SaveGameDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            string equipped = document.EquippedDeckId;
            return DeckCatalog.IsDeck(equipped) ? equipped : DeckCatalog.StarterId;
        }

        public string DeckId { get; }

        public DeckKit Kit { get; }
    }
}
