using System;

namespace DeckRogue.Core.Run
{
    /// <summary>
    /// One act of the three-night campaign: three ordinary clubs + one boss anchor.
    /// </summary>
    public readonly struct CampaignActDefinition
    {
        public CampaignActDefinition(
            int actIndex,
            string id,
            string title,
            string tagline,
            string bossId,
            string[] ordinaryClubIds)
        {
            ActIndex = actIndex;
            Id = id ?? "";
            Title = title ?? "";
            Tagline = tagline ?? "";
            BossId = bossId ?? "";
            OrdinaryClubIds = ordinaryClubIds ?? Array.Empty<string>();
        }

        public int ActIndex { get; }
        public string Id { get; }
        public string Title { get; }
        public string Tagline { get; }
        public string BossId { get; }
        public string[] OrdinaryClubIds { get; }

        public int ClubCount => OrdinaryClubIds.Length + (string.IsNullOrEmpty(BossId) ? 0 : 1);
    }

    /// <summary>
    /// Display names + the three-act roster. Act1 is playable; Act2/Act3 ids are locked for story.
    /// </summary>
    public static class CampaignCatalog
    {
        public const int ActCount = 3;

        public static readonly CampaignActDefinition Act1 = new CampaignActDefinition(
            0,
            "act1_basement_circuit",
            "Act I — Basement Circuit",
            "Three floors. One antenna. Take back the underground.",
            Act1ClubIds.TheSignal,
            new[]
            {
                Act1ClubIds.NeonBasement,
                Act1ClubIds.WarehousePulse,
                Act1ClubIds.BassFactory
            });

        public static readonly CampaignActDefinition Act2 = new CampaignActDefinition(
            1,
            "act2_uptown_frequency",
            "Act II — Uptown Frequency",
            "Soft rooms, hard streets, a throne made of static.",
            CampaignClubIds.TheStaticThrone,
            new[]
            {
                CampaignClubIds.DreamLounge,
                CampaignClubIds.RaveDistrict,
                CampaignClubIds.ChromePier
            });

        public static readonly CampaignActDefinition Act3 = new CampaignActDefinition(
            2,
            "act3_citywide_broadcast",
            "Act III — Citywide Broadcast",
            "Glass, markets, rooftops — then the Final Festival.",
            CampaignClubIds.FinalFestival,
            new[]
            {
                CampaignClubIds.GlassCathedral,
                CampaignClubIds.NightExchange,
                CampaignClubIds.RoofRelay
            });

        public static CampaignActDefinition GetAct(int actIndex)
        {
            switch (actIndex)
            {
                case 0:
                    return Act1;
                case 1:
                    return Act2;
                case 2:
                    return Act3;
                default:
                    throw new ArgumentOutOfRangeException(nameof(actIndex), actIndex, "Act index must be 0..2");
            }
        }

        public static string DisplayName(string clubId)
        {
            if (string.IsNullOrEmpty(clubId))
                return "—";
            if (clubId == Act1ClubIds.NeonBasement)
                return "Neon Basement";
            if (clubId == Act1ClubIds.WarehousePulse)
                return "Warehouse Pulse";
            if (clubId == Act1ClubIds.BassFactory)
                return "Bass Factory";
            if (clubId == Act1ClubIds.TheSignal)
                return "The Signal";
            if (clubId == CampaignClubIds.DreamLounge)
                return "Dream Lounge";
            if (clubId == CampaignClubIds.RaveDistrict)
                return "Rave District";
            if (clubId == CampaignClubIds.ChromePier)
                return "Chrome Pier";
            if (clubId == CampaignClubIds.TheStaticThrone)
                return "The Static Throne";
            if (clubId == CampaignClubIds.GlassCathedral)
                return "Glass Cathedral";
            if (clubId == CampaignClubIds.NightExchange)
                return "Night Exchange";
            if (clubId == CampaignClubIds.RoofRelay)
                return "Roof Relay";
            if (clubId == CampaignClubIds.FinalFestival)
                return "Final Festival";
            return clubId;
        }
    }
}
