namespace DeckRogue.Core.Run
{
    /// <summary>
    /// Visual silhouette bits for club rooms. Scoring does not read this mask.
    /// </summary>
    [System.Flags]
    public enum ClubSilhouette
    {
        None = 0,
        LowCeiling = 1,
        Curtain = 2,
        Bar = 4,
        Truss = 8,
        DockDoor = 16,
        Racks = 32,
        Press = 64,
        Pipes = 128,
        EmberFloor = 256,
        Antenna = 512,
        Towers = 1024,
        BossAxis = 2048
    }

    /// <summary>
    /// Pure look contract: club id to silhouette mask. Unknown ids use the basement set.
    /// </summary>
    public static class ClubLook
    {
        public static ClubSilhouette Mask(string clubId)
        {
            if (clubId == Act1ClubIds.WarehousePulse)
                return ClubSilhouette.Truss | ClubSilhouette.DockDoor | ClubSilhouette.Racks;
            if (clubId == Act1ClubIds.BassFactory)
                return ClubSilhouette.Press | ClubSilhouette.Pipes | ClubSilhouette.EmberFloor;
            if (clubId == Act1ClubIds.TheSignal)
                return ClubSilhouette.Antenna | ClubSilhouette.Towers | ClubSilhouette.BossAxis;
            if (clubId == CampaignClubIds.DreamLounge)
                return ClubSilhouette.LowCeiling | ClubSilhouette.Curtain;
            if (clubId == CampaignClubIds.RaveDistrict)
                return ClubSilhouette.Truss | ClubSilhouette.EmberFloor;
            if (clubId == CampaignClubIds.ChromePier)
                return ClubSilhouette.DockDoor | ClubSilhouette.Towers;
            if (clubId == CampaignClubIds.TheStaticThrone)
                return ClubSilhouette.Antenna | ClubSilhouette.BossAxis | ClubSilhouette.Towers;
            if (clubId == CampaignClubIds.GlassCathedral)
                return ClubSilhouette.Curtain | ClubSilhouette.Towers;
            if (clubId == CampaignClubIds.NightExchange)
                return ClubSilhouette.Bar | ClubSilhouette.Racks;
            if (clubId == CampaignClubIds.RoofRelay)
                return ClubSilhouette.Antenna | ClubSilhouette.Truss;
            if (clubId == CampaignClubIds.FinalFestival)
                return ClubSilhouette.Truss | ClubSilhouette.EmberFloor | ClubSilhouette.BossAxis;
            return ClubSilhouette.LowCeiling | ClubSilhouette.Curtain | ClubSilhouette.Bar;
        }
    }
}
