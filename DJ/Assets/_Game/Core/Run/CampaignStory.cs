using System;

namespace DeckRogue.Core.Run
{
    /// <summary>One readable story page / club-transition beat.</summary>
    public readonly struct StoryBeat
    {
        public StoryBeat(string title, string speakerBadge, string body)
        {
            Title = title ?? "";
            SpeakerBadge = speakerBadge ?? "";
            Body = body ?? "";
        }

        public string Title { get; }
        public string SpeakerBadge { get; }
        public string Body { get; }

        public bool IsEmpty =>
            string.IsNullOrEmpty(Title) && string.IsNullOrEmpty(Body);
    }

    /// <summary>
    /// Campaign plot + dialogue. Intro at launch; per-club beats on every PreClub transition.
    /// </summary>
    public static class CampaignStory
    {
        public static readonly StoryBeat[] IntroPages =
        {
            new StoryBeat(
                "DECK//ROGUE",
                CampaignCast.Mira.Badge,
                "The city used to have a thousand nights.\n"
                + "Now one broadcast — The Signal — flattens every floor into the same sterile loop.\n"
                + "Crowds still dance. They just don't choose."),
            new StoryBeat(
                "The Offer",
                CampaignCast.Mira.Badge,
                "I'm Mira Vance. I book the rooms that still fight.\n"
                + "You bring a crate, two decks, and a spine.\n"
                + "We take the circuit back — club by club, act by act."),
            new StoryBeat(
                "Three Acts",
                CampaignCast.KidVoltage.Badge,
                "Kid Voltage here — don't trip over my name on the flyer.\n"
                + "Act I: Basement Circuit → The Signal.\n"
                + "Act II: Uptown Frequency → Static Throne.\n"
                + "Act III: Citywide Broadcast → Final Festival.\n"
                + "Win the room or the night ends. Simple.\n"
                + "Twelve rooms, three bosses — you only go home after the Festival."),
            new StoryBeat(
                "Your Crew",
                CampaignCast.Mira.Badge,
                "Nyx runs Warehouse doors. Coil owns the low end at Bass Factory.\n"
                + "Echo Vale, Chrome, the Broker, Relay — they'll find you when the acts open.\n"
                + "Tonight we start underground. Read the room. Mix like it matters.")
        };

        /// <summary>
        /// Story shown when a club opens (PreClub). Keyed by club id; Act1 is fully written.
        /// </summary>
        public static StoryBeat ForClubEntry(string clubId, int clubIndex, bool isBoss)
        {
            if (string.IsNullOrEmpty(clubId))
                return default(StoryBeat);

            string place = CampaignCatalog.DisplayName(clubId);
            string slot = isBoss
                ? "BOSS"
                : "Club " + (clubIndex + 1);

            if (clubId == Act1ClubIds.NeonBasement)
            {
                return new StoryBeat(
                    slot + " — " + place,
                    CampaignCast.Mira.Badge,
                    "Warm lights. Soft crowd. First clean transitions — prove you can hold a room.\n"
                    + "Kid Voltage is already posting that you'll choke on the drop.\n"
                    + "Ignore the chat. Feed the floor.");
            }

            if (clubId == Act1ClubIds.WarehousePulse)
            {
                return new StoryBeat(
                    slot + " — " + place,
                    CampaignCast.Nyx.Badge,
                    "Long lines outside. Longer phrases inside.\n"
                    + "Keep deck B ready — if the groove goes stale without a prep, I cut your power.\n"
                    + "Mira says the Signal is listening through the fog machines. Don't give it a boring night.");
            }

            if (clubId == Act1ClubIds.BassFactory)
            {
                return new StoryBeat(
                    slot + " — " + place,
                    CampaignCast.Coil.Badge,
                    "We like it heavy. We hate it dirty.\n"
                    + "One bass conflict too many and the foundry goes cold.\n"
                    + "Give me clean low end and at least one honest transition — then we talk about Act II.");
            }

            if (clubId == Act1ClubIds.TheSignal)
            {
                return new StoryBeat(
                    "BOSS — " + place,
                    CampaignCast.Antenna.Badge,
                    "You climbed the basement circuit. Cute.\n"
                    + "Hold the lock through the midpoint. Then surge — a drop and an honored cue.\n"
                    + "Fail, and every club you touched goes back to my loop.\n"
                    + "Mira: \"Take the booth. Break the carrier.\"");
            }

            if (clubId == CampaignClubIds.DreamLounge)
            {
                return new StoryBeat(
                    slot + " — " + place,
                    CampaignCast.EchoVale.Badge,
                    "Soft dusk, soft pads — don't mistake quiet for weak.\n"
                    + "Don't let the groove go stale. A flat floor fails this room.\n"
                    + "The Signal's echo still hums in the velvet. Keep the night breathing.");
            }

            if (clubId == CampaignClubIds.RaveDistrict)
            {
                return new StoryBeat(
                    slot + " — " + place,
                    CampaignCast.KidVoltage.Badge,
                    "Yellow and blue down the block. Hard floor. No mercy, no strobe lecture.\n"
                    + "They need a drop and a clean transition — miss either and you don't leave.\n"
                    + "I'm already on the next flyer if this room goes quiet.");
            }

            if (clubId == CampaignClubIds.ChromePier)
            {
                return new StoryBeat(
                    slot + " — " + place,
                    CampaignCast.Chrome.Badge,
                    "Salt on the faders. Chrome under the lights.\n"
                    + "Zero bass clashes, and one clean transition — dirty low end sinks the pier.\n"
                    + "Keep the set moving. The water doesn't wait.");
            }

            if (clubId == CampaignClubIds.TheStaticThrone)
            {
                return new StoryBeat(
                    "BOSS — " + place,
                    CampaignCast.StaticCrown.Badge,
                    "Uptown ends here. Sit the throne or become static yourself.\n"
                    + "Hold the room through the midpoint. Then a drop and an honored cue.\n"
                    + "Mira only opens Act III if this room bows.");
            }

            if (clubId == CampaignClubIds.GlassCathedral)
            {
                return new StoryBeat(
                    slot + " — " + place,
                    CampaignCast.GlassWarden.Badge,
                    "Light through glass. Act III opens — no more hiding in basements.\n"
                    + "Hear this room's own taste twice: two clean transitions, on a record that fits these walls.\n"
                    + "Every mix echoes up the city. Miss the second and the doors stay shut.");
            }

            if (clubId == CampaignClubIds.NightExchange)
            {
                return new StoryBeat(
                    slot + " — " + place,
                    CampaignCast.Broker.Badge,
                    "Records change hands faster than favors.\n"
                    + "Land a drop. Don't dirty the bass more than once — a second clash kills the deal.\n"
                    + "Play what you can defend. The Exchange remembers.");
            }

            if (clubId == CampaignClubIds.RoofRelay)
            {
                return new StoryBeat(
                    slot + " — " + place,
                    CampaignCast.Relay.Badge,
                    "Antennas on the skyline. Wind in the cables.\n"
                    + "Land an honored cue, and don't go stale — dead air dies on this roof.\n"
                    + "Hold that line. Then the Festival hears everything.");
            }

            if (clubId == CampaignClubIds.FinalFestival)
            {
                return new StoryBeat(
                    "BOSS — " + place,
                    CampaignCast.FestivalChoir.Badge,
                    "Last booth. The whole city on one PA.\n"
                    + "Hold the room through the middle. Then a drop — on a record the city claims as its own.\n"
                    + "Win and the nights are free. Lose and you become another carrier.\n"
                    + "Mira: \"This is the last booth. Make it count.\"");
            }

            return new StoryBeat(
                slot + " — " + place,
                CampaignCast.Mira.Badge,
                "New floor. Same rule: the crowd decides if you walk out.");
        }

        /// <summary>Short plot synopsis for docs / debug.</summary>
        public static string PlotSynopsis()
        {
            return "The Signal flattens the city's nights into one sterile broadcast. "
                + "Promoter Mira Vance recruits you to reclaim three acts: "
                + "Basement Circuit (The Signal), Uptown Frequency (Static Throne), "
                + "and Citywide Broadcast (Final Festival). "
                + "Rival Kid Voltage, club residents, and boss voices push each transition. "
                + "Win rooms by crowd reaction — not by a health bar.";
        }
    }
}
