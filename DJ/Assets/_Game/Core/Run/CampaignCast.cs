namespace DeckRogue.Core.Run
{
    /// <summary>Named crew who speak story beats across the campaign.</summary>
    public readonly struct CastMember
    {
        public CastMember(string id, string displayName, string role)
        {
            Id = id ?? "";
            DisplayName = displayName ?? "";
            Role = role ?? "";
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Role { get; }

        public string Badge =>
            string.IsNullOrEmpty(Role) ? DisplayName : DisplayName + " · " + Role;
    }

    /// <summary>
    /// Campaign crew roster. Ids are stable; display strings are English to match shell copy.
    /// </summary>
    public static class CampaignCast
    {
        public static readonly CastMember Mira = new CastMember(
            "cast_mira_vance",
            "Mira Vance",
            "Promoter");

        public static readonly CastMember PlayerGhost = new CastMember(
            "cast_you",
            "You",
            "Guest DJ");

        public static readonly CastMember KidVoltage = new CastMember(
            "cast_kid_voltage",
            "Kid Voltage",
            "Rival DJ");

        public static readonly CastMember Nyx = new CastMember(
            "cast_nyx",
            "Nyx",
            "Warehouse tech");

        public static readonly CastMember Coil = new CastMember(
            "cast_coil",
            "Coil",
            "Bass Factory resident");

        public static readonly CastMember Antenna = new CastMember(
            "cast_the_antenna",
            "The Antenna",
            "Signal voice");

        public static readonly CastMember EchoVale = new CastMember(
            "cast_echo_vale",
            "Echo Vale",
            "Dream Lounge host");

        public static readonly CastMember Chrome = new CastMember(
            "cast_chrome",
            "Chrome",
            "Pier skipper");

        public static readonly CastMember StaticCrown = new CastMember(
            "cast_static_crown",
            "Static Crown",
            "Throne DJ");

        public static readonly CastMember GlassWarden = new CastMember(
            "cast_glass_warden",
            "Glass Warden",
            "Cathedral keeper");

        public static readonly CastMember Broker = new CastMember(
            "cast_broker",
            "The Broker",
            "Night Exchange");

        public static readonly CastMember Relay = new CastMember(
            "cast_relay",
            "Relay",
            "Rooftop runner");

        public static readonly CastMember FestivalChoir = new CastMember(
            "cast_festival_choir",
            "Festival Choir",
            "Citywide voice");

        public static CastMember[] All
        {
            get
            {
                return new[]
                {
                    Mira,
                    PlayerGhost,
                    KidVoltage,
                    Nyx,
                    Coil,
                    Antenna,
                    EchoVale,
                    Chrome,
                    StaticCrown,
                    GlassWarden,
                    Broker,
                    Relay,
                    FestivalChoir
                };
            }
        }
    }
}
