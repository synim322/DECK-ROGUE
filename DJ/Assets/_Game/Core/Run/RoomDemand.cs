namespace DeckRogue.Core.Run
{
    /// <summary>
    /// Hype, стартовый зал и длина ночи от crate не меняются: сложнее становится следующий клуб, потому что зал хочет услышать ответ.
    /// Heat — индекс клуба в маршруте (отрицательный считается 0). При Heat = 0 дополнительного условия нет.
    /// При Heat не ниже 1 проход по этому слою есть, только если за сет прозвучал хотя бы один CleanTransition на пластинке вкуса
    /// или два CleanTransition на любых; отрицательные счётчики считаются нулём. Владение треком в crate без эфира не считается.
    /// Вкус — точная строка жанра, без библиотеки.
    /// Act I: Neon — House, Deep House, Electro House, Acid House;
    /// Warehouse — Warehouse Techno, Dub Techno; Bass Factory — Bass Tech;
    /// The Signal — Warehouse Techno, Dub Techno, Bass Tech, Industrial.
    /// Act II: Dream Lounge — Deep House; Rave District — Broken Techno, Bass Tech;
    /// Chrome Pier — Electro, Electro House; The Static Throne — Industrial, Electro.
    /// Act III: Glass Cathedral — Glass Techno, Deep House; Night Exchange — Electro, Acid House;
    /// Roof Relay — Industrial, Dub Techno;
    /// Final Festival — Industrial, Deep House, Glass Techno, Electro, Bass Tech.
    /// Tech House не попадает. Pass rule клуба эта функция не заменяет. HeardEnough остаётся отдельным слоем.
    /// </summary>
    public readonly struct RoomDemand
    {
        private const string House = "House";
        private const string DeepHouse = "Deep House";
        private const string ElectroHouse = "Electro House";
        private const string AcidHouse = "Acid House";
        private const string WarehouseTechno = "Warehouse Techno";
        private const string DubTechno = "Dub Techno";
        private const string BassTech = "Bass Tech";
        private const string Industrial = "Industrial";
        private const string BrokenTechno = "Broken Techno";
        private const string Electro = "Electro";
        private const string GlassTechno = "Glass Techno";

        public int Heat { get; }

        private RoomDemand(int heat)
        {
            Heat = heat;
        }

        /// <summary>
        /// Room index for this club. Taste is <see cref="GenreFits"/>; this does not rewrite hype, open, or length.
        /// </summary>
        public static RoomDemand ForClub(string clubId, int heat)
        {
            // Every room shares this gate. clubId is the floor GenreFits reads; it does not retune hype.
            _ = clubId;
            if (heat < 0)
                heat = 0;
            return new RoomDemand(heat);
        }

        public static bool GenreFits(string clubId, string genre)
        {
            if (string.IsNullOrEmpty(clubId) || string.IsNullOrEmpty(genre))
                return false;

            if (clubId == Act1ClubIds.NeonBasement)
                return genre == House || genre == DeepHouse || genre == ElectroHouse || genre == AcidHouse;

            if (clubId == Act1ClubIds.WarehousePulse)
                return genre == WarehouseTechno || genre == DubTechno;

            if (clubId == Act1ClubIds.BassFactory)
                return genre == BassTech;

            if (clubId == Act1ClubIds.TheSignal)
                return genre == WarehouseTechno || genre == DubTechno || genre == BassTech || genre == Industrial;

            if (clubId == CampaignClubIds.DreamLounge)
                return genre == DeepHouse;

            if (clubId == CampaignClubIds.RaveDistrict)
                return genre == BrokenTechno || genre == BassTech;

            if (clubId == CampaignClubIds.ChromePier)
                return genre == Electro || genre == ElectroHouse;

            if (clubId == CampaignClubIds.TheStaticThrone)
                return genre == Industrial || genre == Electro;

            if (clubId == CampaignClubIds.GlassCathedral)
                return genre == GlassTechno || genre == DeepHouse;

            if (clubId == CampaignClubIds.NightExchange)
                return genre == Electro || genre == AcidHouse;

            if (clubId == CampaignClubIds.RoofRelay)
                return genre == Industrial || genre == DubTechno;

            if (clubId == CampaignClubIds.FinalFestival)
                return genre == Industrial || genre == DeepHouse || genre == GlassTechno || genre == Electro || genre == BassTech;

            return false;
        }

        /// <summary>
        /// heat &lt;= 0 is already enough. Otherwise one clean on a fitting record, or two cleans on any records.
        /// </summary>
        public static bool HeardEnough(int heat, int dialectCleans, int anyCleans)
        {
            if (heat <= 0)
                return true;

            if (dialectCleans < 0)
                dialectCleans = 0;
            if (anyCleans < 0)
                anyCleans = 0;

            return dialectCleans >= 1 || anyCleans >= 2;
        }
    }
}
