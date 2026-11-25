using DeckRogue.Core.Audio;
using DeckRogue.Core.Scoring;
using NUnit.Framework;

namespace DeckRogue.Core.Tests
{
    public sealed class TrackAuraTests
    {
        [Test]
        public void MoodOf_MapsFourIds_AndNoneOtherwise()
        {
            Assert.AreEqual(TrackMood.AmberBloom, TrackAura.MoodOf(TrackAura.AmberBloomId));
            Assert.AreEqual(TrackMood.VoidStatic, TrackAura.MoodOf(TrackAura.VoidStaticId));
            Assert.AreEqual(TrackMood.GlassRain, TrackAura.MoodOf(TrackAura.GlassRainId));
            Assert.AreEqual(TrackMood.RedlineSiren, TrackAura.MoodOf(TrackAura.RedlineSirenId));

            Assert.AreEqual(TrackMood.None, TrackAura.MoodOf("track_neon_pulse"));
            Assert.AreEqual(TrackMood.None, TrackAura.MoodOf(null));
            Assert.AreEqual(TrackMood.None, TrackAura.MoodOf(""));
            Assert.AreEqual(TrackMood.None, TrackAura.MoodOf("TRACK_AMBER_BLOOM"));

            Assert.IsTrue(TrackAura.IsSpecial(TrackAura.AmberBloomId));
            Assert.IsTrue(TrackAura.IsSpecial(TrackAura.VoidStaticId));
            Assert.IsTrue(TrackAura.IsSpecial(TrackAura.GlassRainId));
            Assert.IsTrue(TrackAura.IsSpecial(TrackAura.RedlineSirenId));
            Assert.IsFalse(TrackAura.IsSpecial("track_neon_pulse"));
            Assert.IsFalse(TrackAura.IsSpecial(null));
            Assert.IsFalse(TrackAura.IsSpecial(""));
        }

        [Test]
        public void CardBody_AndShortTag_DescribeSpecialsOnly()
        {
            Assert.AreEqual(
                "Buff: cleans and drops pay +3 hype. Debuff: a bass clash hurts 3 more.",
                TrackAura.CardBody(TrackAura.AmberBloomId));
            Assert.AreEqual("BLOOM +clean −bass", TrackAura.ShortTag(TrackAura.AmberBloomId));

            Assert.AreEqual(
                "Buff: misses, late loads and dead air are forgiven. Debuff: cleans and drops pay 3 less.",
                TrackAura.CardBody(TrackAura.VoidStaticId));
            Assert.AreEqual("STATIC forgive −drop", TrackAura.ShortTag(TrackAura.VoidStaticId));

            Assert.AreEqual(
                "Buff: filter, stem and loop snaps pay +3. Debuff: fuss and hot-swap fumbles hurt 3 more.",
                TrackAura.CardBody(TrackAura.GlassRainId));
            Assert.AreEqual("RAIN +snap −fuss", TrackAura.ShortTag(TrackAura.GlassRainId));

            Assert.AreEqual(
                "Buff: drops pay +5 hype. Debuff: a stale groove and phrase decay hurt 3 more.",
                TrackAura.CardBody(TrackAura.RedlineSirenId));
            Assert.AreEqual("SIREN +drop −stale", TrackAura.ShortTag(TrackAura.RedlineSirenId));

            Assert.AreEqual("", TrackAura.CardBody("track_neon_pulse"));
            Assert.AreEqual("", TrackAura.ShortTag("track_neon_pulse"));
            Assert.AreEqual("", TrackAura.CardBody(null));
            Assert.AreEqual("", TrackAura.ShortTag(""));
        }

        [Test]
        public void Apply_AmberBloom_RaisesCleanByThree()
        {
            ScoreEvent clean = ScoreEvent.Clean(1, 0, 1, false);
            MixSnapshot amber = OnlyA(TrackAura.AmberBloomId);
            ScoreEvent applied = TrackAuraModifier.Apply(clean, amber);

            Assert.AreEqual(TrackAura.AmberBloomId, TrackAuraModifier.DominantTrackId(amber));
            Assert.AreEqual(clean.HypeDelta + 3d, applied.HypeDelta, 1e-9d);
            AssertSameIdentity(clean, applied);

            ScoreEvent prep = ScoreEvent.PrepBonus(clean.Id);
            Assert.AreEqual(prep.HypeDelta + 3d, TrackAuraModifier.Apply(prep, amber).HypeDelta, 1e-9d);

            ScoreEvent drop = ScoreEvent.Drop(4);
            Assert.AreEqual(drop.HypeDelta + 3d, TrackAuraModifier.Apply(drop, amber).HypeDelta, 1e-9d);

            ScoreEvent honor = ScoreEvent.CueHonor(1);
            Assert.AreEqual(honor.HypeDelta, TrackAuraModifier.Apply(honor, amber).HypeDelta, 1e-9d);
        }

        [Test]
        public void Apply_AmberBloom_LowersBassConflictByThree()
        {
            ScoreEvent bass = ScoreEvent.Bass(2);
            ScoreEvent applied = TrackAuraModifier.Apply(bass, OnlyA(TrackAura.AmberBloomId));

            Assert.AreEqual(bass.HypeDelta - 3d, applied.HypeDelta, 1e-9d);
            Assert.AreEqual(-9d, applied.HypeDelta, 1e-9d);
            AssertSameIdentity(bass, applied);
            Assert.IsTrue(applied.ResetsCombo);
        }

        [Test]
        public void Apply_VoidStatic_ForgivesDeadAir_AndLowersClean()
        {
            MixSnapshot voidStatic = OnlyA(TrackAura.VoidStaticId);

            ScoreEvent dead = ScoreEvent.DeadAir(3);
            ScoreEvent forgiven = TrackAuraModifier.Apply(dead, voidStatic);
            Assert.Less(dead.HypeDelta, 0d);
            Assert.AreEqual(0d, forgiven.HypeDelta, 1e-9d);
            AssertSameIdentity(dead, forgiven);

            ScoreEvent late = ScoreEvent.Late(7);
            Assert.AreEqual(0d, TrackAuraModifier.Apply(late, voidStatic).HypeDelta, 1e-9d);

            ScoreEvent miss = ScoreEvent.Miss(1);
            Assert.AreEqual(miss.HypeDelta, TrackAuraModifier.Apply(miss, voidStatic).HypeDelta, 1e-9d);

            ScoreEvent already = new ScoreEvent(
                "dead-air-pos",
                ScoreKind.DeadAir,
                1d,
                0d,
                false,
                false,
                false);
            Assert.AreEqual(1d, TrackAuraModifier.Apply(already, voidStatic).HypeDelta, 1e-9d);

            ScoreEvent clean = ScoreEvent.Clean(1, 0, 1, false);
            Assert.AreEqual(clean.HypeDelta - 3d, TrackAuraModifier.Apply(clean, voidStatic).HypeDelta, 1e-9d);

            ScoreEvent prep = ScoreEvent.PrepBonus(clean.Id);
            Assert.AreEqual(prep.HypeDelta - 3d, TrackAuraModifier.Apply(prep, voidStatic).HypeDelta, 1e-9d);

            ScoreEvent drop = ScoreEvent.Drop(4);
            Assert.AreEqual(drop.HypeDelta - 3d, TrackAuraModifier.Apply(drop, voidStatic).HypeDelta, 1e-9d);
        }

        [Test]
        public void Apply_GlassRain_RaisesFilterPhrase_AndLowersFuss()
        {
            MixSnapshot rain = OnlyA(TrackAura.GlassRainId);

            ScoreEvent filter = ScoreEvent.FilterPhrase(2);
            Assert.AreEqual(filter.HypeDelta + 3d, TrackAuraModifier.Apply(filter, rain).HypeDelta, 1e-9d);

            ScoreEvent stem = ScoreEvent.StemPunch(5);
            Assert.AreEqual(stem.HypeDelta + 3d, TrackAuraModifier.Apply(stem, rain).HypeDelta, 1e-9d);

            ScoreEvent loop = ScoreEvent.LoopSnap(6, 4);
            Assert.AreEqual(loop.HypeDelta + 3d, TrackAuraModifier.Apply(loop, rain).HypeDelta, 1e-9d);

            ScoreEvent fuss = ScoreEvent.Fuss(2);
            ScoreEvent fumbled = TrackAuraModifier.Apply(fuss, rain);
            Assert.AreEqual(fuss.HypeDelta - 3d, fumbled.HypeDelta, 1e-9d);
            AssertSameIdentity(fuss, fumbled);

            ScoreEvent swap = ScoreEvent.HotSwap(2);
            Assert.AreEqual(swap.HypeDelta - 3d, TrackAuraModifier.Apply(swap, rain).HypeDelta, 1e-9d);

            ScoreEvent clean = ScoreEvent.Clean(1, 0, 1, false);
            Assert.AreEqual(clean.HypeDelta, TrackAuraModifier.Apply(clean, rain).HypeDelta, 1e-9d);
        }

        [Test]
        public void Apply_RedlineSiren_RaisesDrop_AndLowersStaleGroove()
        {
            MixSnapshot siren = OnlyA(TrackAura.RedlineSirenId);

            ScoreEvent drop = ScoreEvent.Drop(8);
            ScoreEvent applied = TrackAuraModifier.Apply(drop, siren);
            Assert.AreEqual(drop.HypeDelta + 5d, applied.HypeDelta, 1e-9d);
            Assert.AreEqual(17d, applied.HypeDelta, 1e-9d);
            AssertSameIdentity(drop, applied);

            ScoreEvent stale = ScoreEvent.Stale(3);
            Assert.AreEqual(stale.HypeDelta - 3d, TrackAuraModifier.Apply(stale, siren).HypeDelta, 1e-9d);

            ScoreEvent decay = ScoreEvent.Decay(3);
            Assert.AreEqual(decay.HypeDelta - 3d, TrackAuraModifier.Apply(decay, siren).HypeDelta, 1e-9d);

            ScoreEvent clean = ScoreEvent.Clean(1, 0, 1, false);
            Assert.AreEqual(clean.HypeDelta, TrackAuraModifier.Apply(clean, siren).HypeDelta, 1e-9d);
        }

        [Test]
        public void Apply_OrdinaryDominantTrack_LeavesCleanUnchanged()
        {
            ScoreEvent clean = ScoreEvent.Clean(1, 0, 1, false);
            MixSnapshot neon = OnlyA("track_neon_pulse");
            ScoreEvent applied = TrackAuraModifier.Apply(clean, neon);

            Assert.AreEqual("track_neon_pulse", TrackAuraModifier.DominantTrackId(neon));
            Assert.AreEqual(clean.HypeDelta, applied.HypeDelta, 1e-9d);
            Assert.AreEqual(clean.ReputationBase, applied.ReputationBase, 1e-9d);
            Assert.AreEqual(clean.Id, applied.Id);
            Assert.AreEqual(clean.Kind, applied.Kind);
        }

        [Test]
        public void Apply_NeitherDeckPlaying_LeavesTrackAAuraIdle()
        {
            ScoreEvent clean = ScoreEvent.Clean(1, 0, 1, false);
            MixSnapshot idle = new MixSnapshot(
                0d,
                false,
                false,
                false,
                1d,
                1d,
                1d,
                1d,
                TrackAura.AmberBloomId,
                TrackAura.RedlineSirenId,
                true);
            ScoreEvent applied = TrackAuraModifier.Apply(clean, idle);

            Assert.AreEqual("", TrackAuraModifier.DominantTrackId(idle));
            Assert.AreEqual(clean.HypeDelta, applied.HypeDelta, 1e-9d);
            Assert.AreEqual(clean.Id, applied.Id);
        }

        [Test]
        public void Apply_BothPlaying_HigherGainWins_TieUsesTrackB()
        {
            ScoreEvent clean = ScoreEvent.Clean(1, 0, 1, false);

            MixSnapshot amberLeads = Both(TrackAura.AmberBloomId, TrackAura.VoidStaticId, 0.8d, 0.4d);
            Assert.AreEqual(TrackAura.AmberBloomId, TrackAuraModifier.DominantTrackId(amberLeads));
            Assert.AreEqual(
                clean.HypeDelta + 3d,
                TrackAuraModifier.Apply(clean, amberLeads).HypeDelta,
                1e-9d);

            MixSnapshot voidLeads = Both(TrackAura.AmberBloomId, TrackAura.VoidStaticId, 0.3d, 0.7d);
            Assert.AreEqual(TrackAura.VoidStaticId, TrackAuraModifier.DominantTrackId(voidLeads));
            Assert.AreEqual(
                clean.HypeDelta - 3d,
                TrackAuraModifier.Apply(clean, voidLeads).HypeDelta,
                1e-9d);

            MixSnapshot tie = Both(TrackAura.AmberBloomId, TrackAura.VoidStaticId, 0.6d, 0.6d);
            Assert.AreEqual(TrackAura.VoidStaticId, TrackAuraModifier.DominantTrackId(tie));
            Assert.AreEqual(
                clean.HypeDelta - 3d,
                TrackAuraModifier.Apply(clean, tie).HypeDelta,
                1e-9d);

            MixSnapshot onlyB = OnlyB(TrackAura.RedlineSirenId);
            Assert.AreEqual(TrackAura.RedlineSirenId, TrackAuraModifier.DominantTrackId(onlyB));
            ScoreEvent drop = ScoreEvent.Drop(1);
            Assert.AreEqual(drop.HypeDelta + 5d, TrackAuraModifier.Apply(drop, onlyB).HypeDelta, 1e-9d);
        }

        [Test]
        public void LaterMoods_CardBodyAndShortTag()
        {
            Assert.AreEqual(TrackMood.VelvetHush, TrackAura.MoodOf(TrackAura.VelvetHushId));
            Assert.AreEqual(TrackMood.Hardline, TrackAura.MoodOf(TrackAura.HardlineYellowId));
            Assert.AreEqual(TrackMood.SaltChrome, TrackAura.MoodOf(TrackAura.SaltChromeId));
            Assert.AreEqual(TrackMood.PrismChoir, TrackAura.MoodOf(TrackAura.PrismChoirId));
            Assert.AreEqual(TrackMood.Ledger, TrackAura.MoodOf(TrackAura.NightLedgerId));
            Assert.AreEqual(TrackMood.Skywire, TrackAura.MoodOf(TrackAura.SkywireId));
            Assert.AreEqual(TrackMood.StaticCrown, TrackAura.MoodOf(TrackAura.StaticCrownId));
            Assert.AreEqual(TrackMood.FestivalAnthem, TrackAura.MoodOf(TrackAura.FestivalAnthemId));

            Assert.AreEqual(
                "Buff: a stale phrase and decay are forgiven. Debuff: drops pay 3 less.",
                TrackAura.CardBody(TrackAura.VelvetHushId));
            Assert.AreEqual("HUSH forgive \u2212drop", TrackAura.ShortTag(TrackAura.VelvetHushId));
            Assert.AreEqual(
                "Buff: drops pay +4 and stem punches +3. Debuff: a bass clash hurts 4 more.",
                TrackAura.CardBody(TrackAura.HardlineYellowId));
            Assert.AreEqual("HARD +drop \u2212bass", TrackAura.ShortTag(TrackAura.HardlineYellowId));
            Assert.AreEqual(
                "Buff: cleans and bass handoffs pay +3. Debuff: a missed phrase hurts 3 more.",
                TrackAura.CardBody(TrackAura.SaltChromeId));
            Assert.AreEqual("SALT +clean \u2212miss", TrackAura.ShortTag(TrackAura.SaltChromeId));
            Assert.AreEqual(
                "Buff: filter phrases pay +4 and loop snaps +3. Debuff: fuss hurts 4 more.",
                TrackAura.CardBody(TrackAura.PrismChoirId));
            Assert.AreEqual("PRISM +filter \u2212fuss", TrackAura.ShortTag(TrackAura.PrismChoirId));
            Assert.AreEqual(
                "Buff: a prepped clean pays +4. Debuff: a hot-swap fumble hurts 4 more.",
                TrackAura.CardBody(TrackAura.NightLedgerId));
            Assert.AreEqual("LEDGER +prep \u2212swap", TrackAura.ShortTag(TrackAura.NightLedgerId));
            Assert.AreEqual(
                "Buff: line lifts pay +4 and phrase starts +2. Debuff: phrase decay hurts 3 more.",
                TrackAura.CardBody(TrackAura.SkywireId));
            Assert.AreEqual("WIRE +line \u2212decay", TrackAura.ShortTag(TrackAura.SkywireId));
            Assert.AreEqual(
                "Buff: an honored cue pays +4. Debuff: a stale groove hurts 4 more, dead air 3 more.",
                TrackAura.CardBody(TrackAura.StaticCrownId));
            Assert.AreEqual("CROWN +cue \u2212stale", TrackAura.ShortTag(TrackAura.StaticCrownId));
            Assert.AreEqual(
                "Buff: drops pay +4 and cleans +2. Debuff: a bass clash hurts 5 more.",
                TrackAura.CardBody(TrackAura.FestivalAnthemId));
            Assert.AreEqual("ANTHEM +drop \u2212bass", TrackAura.ShortTag(TrackAura.FestivalAnthemId));
        }

        [Test]
        public void Apply_VelvetHush_ForgivesDecay_AndLowersDrop()
        {
            MixSnapshot hush = OnlyA(TrackAura.VelvetHushId);
            Assert.AreEqual(TrackAura.VelvetHushId, TrackAuraModifier.DominantTrackId(hush));

            ScoreEvent decay = ScoreEvent.Decay(2);
            ScoreEvent forgiven = TrackAuraModifier.Apply(decay, hush);
            Assert.Less(decay.HypeDelta, 0d);
            Assert.AreEqual(0d, forgiven.HypeDelta, 1e-9d);
            AssertSameIdentity(decay, forgiven);

            ScoreEvent stale = ScoreEvent.Stale(3);
            Assert.AreEqual(0d, TrackAuraModifier.Apply(stale, hush).HypeDelta, 1e-9d);

            ScoreEvent already = new ScoreEvent("decay-pos", ScoreKind.PhraseDecay, 1d, 0d, false, false, false);
            Assert.AreEqual(1d, TrackAuraModifier.Apply(already, hush).HypeDelta, 1e-9d);

            ScoreEvent drop = ScoreEvent.Drop(4);
            Assert.AreEqual(drop.HypeDelta - 3d, TrackAuraModifier.Apply(drop, hush).HypeDelta, 1e-9d);

            ScoreEvent clean = ScoreEvent.Clean(1, 0, 1, false);
            Assert.AreEqual(clean.HypeDelta, TrackAuraModifier.Apply(clean, hush).HypeDelta, 1e-9d);
            AssertDominantOtherDeckLeavesBuff(TrackAura.VelvetHushId, decay);
        }

        [Test]
        public void Apply_Hardline_RaisesDrop_AndLowersBass()
        {
            AssertFlatMood(
                TrackAura.HardlineYellowId,
                ScoreEvent.Drop(4),
                4d,
                ScoreEvent.Bass(2),
                -4d,
                ScoreEvent.Clean(1, 0, 1, false));
            ScoreEvent stem = ScoreEvent.StemPunch(5);
            Assert.AreEqual(stem.HypeDelta + 3d, TrackAuraModifier.Apply(stem, OnlyA(TrackAura.HardlineYellowId)).HypeDelta, 1e-9d);
        }

        [Test]
        public void Apply_SaltChrome_RaisesClean_AndLowersMiss()
        {
            AssertFlatMood(
                TrackAura.SaltChromeId,
                ScoreEvent.Clean(1, 0, 1, false),
                3d,
                ScoreEvent.Miss(2),
                -3d,
                ScoreEvent.PrepBonus("transition-1-0-1"));
            ScoreEvent handoff = ScoreEvent.BassHandoff(3);
            Assert.AreEqual(
                handoff.HypeDelta + 3d,
                TrackAuraModifier.Apply(handoff, OnlyA(TrackAura.SaltChromeId)).HypeDelta,
                1e-9d);
        }

        [Test]
        public void Apply_PrismChoir_RaisesFilter_AndLowersFuss()
        {
            AssertFlatMood(
                TrackAura.PrismChoirId,
                ScoreEvent.FilterPhrase(2),
                4d,
                ScoreEvent.Fuss(2),
                -4d,
                ScoreEvent.Clean(1, 0, 1, false));
            ScoreEvent loop = ScoreEvent.LoopSnap(6, 4);
            Assert.AreEqual(loop.HypeDelta + 3d, TrackAuraModifier.Apply(loop, OnlyA(TrackAura.PrismChoirId)).HypeDelta, 1e-9d);
        }

        [Test]
        public void Apply_NightLedger_RaisesPrep_AndLowersHotSwap()
        {
            AssertFlatMood(
                TrackAura.NightLedgerId,
                ScoreEvent.PrepBonus("transition-1-0-1"),
                4d,
                ScoreEvent.HotSwap(2),
                -4d,
                ScoreEvent.Clean(1, 0, 1, false));
        }

        [Test]
        public void Apply_Skywire_RaisesLineLift_AndLowersDecay()
        {
            AssertFlatMood(
                TrackAura.SkywireId,
                ScoreEvent.LineLift(2, 0),
                4d,
                ScoreEvent.Decay(3),
                -3d,
                ScoreEvent.Clean(1, 0, 1, false));
            ScoreEvent start = ScoreEvent.PhraseStart(1, 0);
            Assert.AreEqual(start.HypeDelta + 2d, TrackAuraModifier.Apply(start, OnlyA(TrackAura.SkywireId)).HypeDelta, 1e-9d);
        }

        [Test]
        public void Apply_StaticCrown_RaisesCue_AndLowersStaleAndDeadAir()
        {
            MixSnapshot crown = OnlyA(TrackAura.StaticCrownId);
            Assert.AreEqual(TrackAura.StaticCrownId, TrackAuraModifier.DominantTrackId(crown));

            ScoreEvent honor = ScoreEvent.CueHonor(4);
            ScoreEvent applied = TrackAuraModifier.Apply(honor, crown);
            Assert.AreEqual(honor.HypeDelta + 4d, applied.HypeDelta, 1e-9d);
            AssertSameIdentity(honor, applied);

            ScoreEvent stale = ScoreEvent.Stale(2);
            Assert.AreEqual(stale.HypeDelta - 4d, TrackAuraModifier.Apply(stale, crown).HypeDelta, 1e-9d);

            ScoreEvent dead = ScoreEvent.DeadAir(3);
            Assert.Less(dead.HypeDelta, 0d);
            Assert.AreEqual(dead.HypeDelta - 3d, TrackAuraModifier.Apply(dead, crown).HypeDelta, 1e-9d);

            ScoreEvent already = new ScoreEvent("dead-air-pos", ScoreKind.DeadAir, 1d, 0d, false, false, false);
            Assert.AreEqual(1d, TrackAuraModifier.Apply(already, crown).HypeDelta, 1e-9d);

            ScoreEvent clean = ScoreEvent.Clean(1, 0, 1, false);
            Assert.AreEqual(clean.HypeDelta, TrackAuraModifier.Apply(clean, crown).HypeDelta, 1e-9d);
            AssertDominantOtherDeckLeavesBuff(TrackAura.StaticCrownId, honor);
        }

        [Test]
        public void Apply_FestivalAnthem_RaisesDrop_AndLowersBass()
        {
            AssertFlatMood(
                TrackAura.FestivalAnthemId,
                ScoreEvent.Drop(4),
                4d,
                ScoreEvent.Bass(1),
                -5d,
                ScoreEvent.CueHonor(1));
            ScoreEvent clean = ScoreEvent.Clean(1, 0, 1, false);
            Assert.AreEqual(
                clean.HypeDelta + 2d,
                TrackAuraModifier.Apply(clean, OnlyA(TrackAura.FestivalAnthemId)).HypeDelta,
                1e-9d);
        }

        private static void AssertFlatMood(
            string trackId,
            ScoreEvent buff,
            double buffAdjust,
            ScoreEvent debuff,
            double debuffAdjust,
            ScoreEvent untouched)
        {
            MixSnapshot dominant = OnlyA(trackId);
            Assert.AreEqual(trackId, TrackAuraModifier.DominantTrackId(dominant));
            ScoreEvent buffed = TrackAuraModifier.Apply(buff, dominant);
            Assert.AreEqual(buff.HypeDelta + buffAdjust, buffed.HypeDelta, 1e-9d);
            AssertSameIdentity(buff, buffed);

            ScoreEvent hurt = TrackAuraModifier.Apply(debuff, dominant);
            Assert.AreEqual(debuff.HypeDelta + debuffAdjust, hurt.HypeDelta, 1e-9d);
            AssertSameIdentity(debuff, hurt);

            Assert.AreEqual(untouched.HypeDelta, TrackAuraModifier.Apply(untouched, dominant).HypeDelta, 1e-9d);
            AssertDominantOtherDeckLeavesBuff(trackId, buff);
        }

        private static void AssertDominantOtherDeckLeavesBuff(string trackId, ScoreEvent buff)
        {
            MixSnapshot other = new MixSnapshot(
                0d,
                false,
                true,
                true,
                0.9d,
                0.2d,
                1d,
                1d,
                "track_neon_pulse",
                trackId,
                true);
            Assert.AreEqual("track_neon_pulse", TrackAuraModifier.DominantTrackId(other));
            Assert.AreEqual(buff.HypeDelta, TrackAuraModifier.Apply(buff, other).HypeDelta, 1e-9d);
        }

        private static void AssertSameIdentity(ScoreEvent before, ScoreEvent after)
        {
            Assert.AreEqual(before.Id, after.Id);
            Assert.AreEqual(before.Kind, after.Kind);
            Assert.AreEqual(before.ReputationBase, after.ReputationBase, 1e-9d);
            Assert.AreEqual(before.IsQuality, after.IsQuality);
            Assert.AreEqual(before.ResetsCombo, after.ResetsCombo);
            Assert.AreEqual(before.HardCut, after.HardCut);
        }

        private static MixSnapshot OnlyA(string trackId)
        {
            return new MixSnapshot(
                0d,
                false,
                true,
                false,
                0.2d,
                0.9d,
                1d,
                1d,
                trackId,
                TrackAura.RedlineSirenId,
                true);
        }

        private static MixSnapshot OnlyB(string trackId)
        {
            return new MixSnapshot(0d, false, false, true, 0.9d, 0.2d, 1d, 1d, TrackAura.AmberBloomId, trackId, true);
        }

        private static MixSnapshot Both(string trackA, string trackB, double gainA, double gainB)
        {
            return new MixSnapshot(0d, false, true, true, gainA, gainB, 1d, 1d, trackA, trackB, true);
        }
    }
}
