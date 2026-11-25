using System;
using DeckRogue.Core.Audio;
using NUnit.Framework;

namespace DeckRogue.Core.Tests
{
    public sealed class TrackLibraryTests
    {
        [Test]
        public void CreateMvp_HasTheFourContentBibleRecords()
        {
            TrackLibrary library = TrackLibrary.CreateMvp();

            Assert.AreEqual(4, library.Tracks.Count);
            Assert.AreEqual("track_neon_pulse", library.Tracks[0].Id);
            Assert.AreEqual("track_bass_reactor", library.Tracks[1].Id);
            Assert.AreEqual("track_afterhours", library.Tracks[2].Id);
            Assert.AreEqual("track_city_static", library.Tracks[3].Id);
        }

        [Test]
        public void CreateMvp_SharesTempoKeyAndLoopLength()
        {
            foreach (TrackDefinition track in TrackLibrary.CreateMvp().Tracks)
            {
                Assert.AreEqual(120d, track.Bpm, 1e-9d);
                Assert.AreEqual(4, track.BeatsPerBar);
                Assert.AreEqual(8, track.BarsPerLoop);
                Assert.AreEqual(16d, track.LengthSeconds, TrackLibrary.LengthToleranceSeconds);
                Assert.AreEqual("A_minor", track.Key);
                Assert.IsFalse(string.IsNullOrWhiteSpace(track.LicenseId));
                Assert.GreaterOrEqual(track.Energy, 1);
                Assert.LessOrEqual(track.Energy, 10);
                Assert.AreEqual(track.Id.Substring("track_".Length) + "_low", track.StemFile(StemId.Low));
                Assert.AreEqual(track.Id.Substring("track_".Length) + "_perc", track.StemFile(StemId.Perc));
                Assert.AreEqual(track.Id.Substring("track_".Length) + "_music", track.StemFile(StemId.Music));
            }
        }

        [Test]
        public void Constructor_RejectsADuplicateId()
        {
            TrackDefinition first = TrackLibrary.CreateMvp().Tracks[0];

            Assert.Throws<ArgumentException>(() => new TrackLibrary(new[] { first, first }));
        }

        [Test]
        public void Constructor_RejectsALoopThatIsNotSixteenSeconds()
        {
            var odd = new TrackDefinition(
                "track_odd",
                "Odd",
                100d,
                4,
                8,
                "A_minor",
                "House",
                5,
                "Bright",
                "odd_low",
                "odd_perc",
                "odd_music",
                "LIC-001");

            Assert.Throws<ArgumentException>(() => new TrackLibrary(new[] { odd }));
        }

        [Test]
        public void TrackDefinition_RejectsAMissingStemAndABadId()
        {
            Assert.Throws<ArgumentException>(() => new TrackDefinition(
                "track_ok",
                "Ok",
                120d,
                4,
                8,
                "A_minor",
                "House",
                5,
                "Bright",
                "ok_low",
                "",
                "ok_music",
                "LIC-001"));

            Assert.Throws<ArgumentException>(() => new TrackDefinition(
                "NeonPulse",
                "Ok",
                120d,
                4,
                8,
                "A_minor",
                "House",
                5,
                "Bright",
                "ok_low",
                "ok_perc",
                "ok_music",
                "LIC-001"));
        }

        [Test]
        public void CreateAct1Catalog_HasTheCampaignRecords()
        {
            TrackLibrary catalog = TrackLibrary.CreateAct1Catalog();

            Assert.AreEqual(56, catalog.Tracks.Count);
            Assert.AreEqual("track_warehouse_line", catalog.Tracks[0].Id);
            Assert.AreEqual("track_smoke_column", catalog.Tracks[1].Id);
            Assert.AreEqual("track_press_line", catalog.Tracks[2].Id);
            Assert.AreEqual("track_torque_drum", catalog.Tracks[3].Id);
            Assert.AreEqual("track_voltage_alley", catalog.Tracks[4].Id);
            Assert.AreEqual("track_mutabor_pulse", catalog.Tracks[5].Id);
            Assert.AreEqual("track_foundry_drop", catalog.Tracks[6].Id);
            Assert.AreEqual("track_afterglow_key", catalog.Tracks[7].Id);
            Assert.AreEqual("track_signal_carrier", catalog.Tracks[8].Id);
            Assert.AreEqual("track_signal_breach", catalog.Tracks[9].Id);
            Assert.AreEqual("track_amber_bloom", catalog.Tracks[18].Id);
            Assert.AreEqual("track_void_static", catalog.Tracks[19].Id);
            Assert.AreEqual("track_glass_rain", catalog.Tracks[20].Id);
            Assert.AreEqual("track_redline_siren", catalog.Tracks[21].Id);
            Assert.AreEqual("track_dusk_pad", catalog.Tracks[22].Id);
            Assert.AreEqual("track_festival_anthem", catalog.Tracks[55].Id);
            Assert.AreEqual(TrackFlags.BossExclusive, catalog.Get("track_throne_hum").Flags);
            Assert.AreEqual(
                TrackFlags.Reward | TrackFlags.BossDrop | TrackFlags.MetaUnlock | TrackFlags.Special,
                catalog.Get("track_static_crown").Flags);
            Assert.AreEqual(
                TrackFlags.Reward | TrackFlags.BossDrop | TrackFlags.MetaUnlock | TrackFlags.Special,
                catalog.Get("track_festival_anthem").Flags);
        }

        [Test]
        public void CreateAct1Catalog_UsesOriginalAssetIdsAndSharedGrid()
        {
            foreach (TrackDefinition track in TrackLibrary.CreateAct1Catalog().Tracks)
            {
                Assert.AreEqual(120d, track.Bpm, 1e-9d);
                Assert.AreEqual("A_minor", track.Key);
                Assert.AreEqual(16d, track.LengthSeconds, TrackLibrary.LengthToleranceSeconds);
                StringAssert.StartsWith("DR-CLUB-", track.LicenseId);
                Assert.AreNotEqual(TrackFlags.None, track.Flags);
            }
        }

        [Test]
        public void CreateMvp_DoesNotIncludeAct1CatalogIds()
        {
            TrackLibrary mvp = TrackLibrary.CreateMvp();
            TrackLibrary act1 = TrackLibrary.CreateAct1Catalog();

            Assert.AreEqual(4, mvp.Tracks.Count);
            foreach (TrackDefinition track in act1.Tracks)
                Assert.IsFalse(mvp.TryGet(track.Id, out _));
        }

        [Test]
        public void CreateAct1Playable_HasSixtyUniqueIds()
        {
            TrackLibrary playable = TrackLibrary.CreateAct1Playable();
            TrackLibrary mvp = TrackLibrary.CreateMvp();

            Assert.AreEqual(4, mvp.Tracks.Count);
            Assert.AreEqual(60, playable.Tracks.Count);

            var ids = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (TrackDefinition track in playable.Tracks)
                Assert.IsTrue(ids.Add(track.Id), "Duplicate id " + track.Id);

            Assert.AreEqual("track_neon_pulse", playable.Tracks[0].Id);
            Assert.AreEqual("track_bass_reactor", playable.Tracks[1].Id);
            Assert.AreEqual("track_afterhours", playable.Tracks[2].Id);
            Assert.AreEqual("track_city_static", playable.Tracks[3].Id);
            Assert.AreEqual("track_warehouse_line", playable.Tracks[4].Id);
            Assert.AreEqual("track_signal_breach", playable.Tracks[13].Id);
        }

        [Test]
        public void CreateAct1Playable_HasDistinctStemsAndTwentyEightRewards()
        {
            int rewards = 0;
            foreach (TrackDefinition track in TrackLibrary.CreateAct1Playable().Tracks)
            {
                string prefix = track.Id.Substring("track_".Length);
                Assert.AreEqual(prefix + "_low", track.StemFile(StemId.Low));
                Assert.AreEqual(prefix + "_perc", track.StemFile(StemId.Perc));
                Assert.AreEqual(prefix + "_music", track.StemFile(StemId.Music));
                if ((track.Flags & TrackFlags.Reward) != 0)
                    rewards++;
            }
            Assert.AreEqual(28, rewards);
        }

#if UNITY_EDITOR
        [Test]
        public void ClubRecordStems_LoadOnTheExactGameGrid()
        {
            foreach (TrackDefinition track in TrackLibrary.CreateAct1Catalog().Tracks)
            {
                foreach (StemId stem in new[] { StemId.Low, StemId.Perc, StemId.Music })
                {
                    string resource = "TestLoops/" + track.StemFile(stem);
                    var clip = UnityEngine.Resources.Load<UnityEngine.AudioClip>(resource);
                    Assert.IsNotNull(clip, resource);
                    // Some Unity backends resample the source to their output rate.
                    Assert.AreEqual(16 * clip.frequency, clip.samples, resource);
                    UnityEngine.Resources.UnloadAsset(clip);
                }
            }
        }
#endif

        [Test]
        public void TrackDefinition_DefaultsFlagsToNone()
        {
            var track = new TrackDefinition(
                "track_ok",
                "Ok",
                120d,
                4,
                8,
                "A_minor",
                "House",
                5,
                "Bright",
                "ok_low",
                "ok_perc",
                "ok_music",
                "LIC-001");

            Assert.AreEqual(TrackFlags.None, track.Flags);
        }
    }
}
