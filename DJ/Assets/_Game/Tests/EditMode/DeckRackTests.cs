using DeckRogue.Core.Audio;
using NUnit.Framework;

namespace DeckRogue.Core.Tests
{
    public sealed class DeckRackTests
    {
        [Test]
        public void TryLoad_StoresTheRecordWhenTheDeckIsIdle()
        {
            var rack = new DeckRack();
            TrackDefinition track = TrackLibrary.CreateMvp().Get("track_city_static");

            string reason;
            bool loaded = rack.TryLoad(DeckId.B, track, false, out reason);

            Assert.IsTrue(loaded);
            Assert.AreEqual("loaded", reason);
            Assert.AreSame(track, rack.Loaded(DeckId.B));
        }

        [Test]
        public void TryLoad_RefusesToReplaceAPlayingDeck()
        {
            var rack = new DeckRack();
            TrackLibrary library = TrackLibrary.CreateMvp();
            string reason;
            rack.TryLoad(DeckId.A, library.Tracks[0], false, out reason);

            bool loaded = rack.TryLoad(DeckId.A, library.Tracks[1], true, out reason);

            Assert.IsFalse(loaded);
            Assert.AreEqual("deck-is-active", reason);
            Assert.AreSame(library.Tracks[0], rack.Loaded(DeckId.A));
        }
    }
}
