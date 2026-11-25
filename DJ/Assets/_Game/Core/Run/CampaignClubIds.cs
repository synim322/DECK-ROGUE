namespace DeckRogue.Core.Run
{
    /// <summary>
    /// Stable club ids across the three-act campaign. Act1 ids live in <see cref="Act1ClubIds"/>;
    /// Act2/Act3 packs ship later but ids are locked for story and route tables.
    /// </summary>
    public static class CampaignClubIds
    {
        // Act 2 ordinary
        public const string DreamLounge = "club_dream_lounge";
        public const string RaveDistrict = "club_rave_district";
        public const string ChromePier = "club_chrome_pier";

        // Act 2 boss
        public const string TheStaticThrone = "club_the_static_throne";

        // Act 3 ordinary
        public const string GlassCathedral = "club_glass_cathedral";
        public const string NightExchange = "club_night_exchange";
        public const string RoofRelay = "club_roof_relay";

        // Act 3 boss (finale)
        public const string FinalFestival = "club_final_festival";
    }
}
