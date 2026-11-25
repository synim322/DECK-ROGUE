namespace GenerateTestLoops
{
    internal static class TrackDispatch
    {
        public static void Render(string stem, double[] low, double[] perc, double[] music)
        {
            switch (stem)
            {
                case "neon_pulse": Tracks.NeonPulse.Render(low, perc, music); break;
                case "bass_reactor": Tracks.BassReactor.Render(low, perc, music); break;
                case "afterhours": Tracks.Afterhours.Render(low, perc, music); break;
                case "city_static": Tracks.CityStatic.Render(low, perc, music); break;
                case "dusk_pad": Tracks.DuskPad.Render(low, perc, music); break;
                case "velvet_room": Tracks.VelvetRoom.Render(low, perc, music); break;
                case "strobe_grid": Tracks.StrobeGrid.Render(low, perc, music); break;
                case "yellow_siren": Tracks.YellowSiren.Render(low, perc, music); break;
                case "pier_salt": Tracks.PierSalt.Render(low, perc, music); break;
                case "tide_click": Tracks.TideClick.Render(low, perc, music); break;
                case "throne_hum": Tracks.ThroneHum.Render(low, perc, music); break;
                case "crown_loop": Tracks.CrownLoop.Render(low, perc, music); break;
                case "prism_pane": Tracks.PrismPane.Render(low, perc, music); break;
                case "choir_dust": Tracks.ChoirDust.Render(low, perc, music); break;
                case "ledger_tick": Tracks.LedgerTick.Render(low, perc, music); break;
                case "favor_bass": Tracks.FavorBass.Render(low, perc, music); break;
                case "sky_cable": Tracks.SkyCable.Render(low, perc, music); break;
                case "wind_hat": Tracks.WindHat.Render(low, perc, music); break;
                case "festival_grid": Tracks.FestivalGrid.Render(low, perc, music); break;
                case "city_choir": Tracks.CityChoir.Render(low, perc, music); break;
                case "lounge_key": Tracks.LoungeKey.Render(low, perc, music); break;
                case "district_hat": Tracks.DistrictHat.Render(low, perc, music); break;
                case "siren_sub": Tracks.SirenSub.Render(low, perc, music); break;
                case "pier_roll": Tracks.PierRoll.Render(low, perc, music); break;
                case "chrome_wake": Tracks.ChromeWake.Render(low, perc, music); break;
                case "nave_pad": Tracks.NavePad.Render(low, perc, music); break;
                case "market_click": Tracks.MarketClick.Render(low, perc, music); break;
                case "roof_hum": Tracks.RoofHum.Render(low, perc, music); break;
                case "exchange_acid": Tracks.ExchangeAcid.Render(low, perc, music); break;
                case "relay_dub": Tracks.RelayDub.Render(low, perc, music); break;
                case "velvet_hush": Tracks.VelvetHush.Render(low, perc, music); break;
                case "hardline_yellow": Tracks.HardlineYellow.Render(low, perc, music); break;
                case "salt_chrome": Tracks.SaltChrome.Render(low, perc, music); break;
                case "prism_choir": Tracks.PrismChoir.Render(low, perc, music); break;
                case "night_ledger": Tracks.NightLedger.Render(low, perc, music); break;
                case "skywire": Tracks.Skywire.Render(low, perc, music); break;
                case "static_crown": Tracks.StaticCrown.Render(low, perc, music); break;
                case "festival_anthem": Tracks.FestivalAnthem.Render(low, perc, music); break;
                case "warehouse_line": Tracks.WarehouseLine.Render(low, perc, music); break;
                case "smoke_column": Tracks.SmokeColumn.Render(low, perc, music); break;
                case "press_line": Tracks.PressLine.Render(low, perc, music); break;
                case "torque_drum": Tracks.TorqueDrum.Render(low, perc, music); break;
                case "voltage_alley": Tracks.VoltageAlley.Render(low, perc, music); break;
                case "mutabor_pulse": Tracks.MutaborPulse.Render(low, perc, music); break;
                case "foundry_drop": Tracks.FoundryDrop.Render(low, perc, music); break;
                case "afterglow_key": Tracks.AfterglowKey.Render(low, perc, music); break;
                case "signal_carrier": Tracks.SignalCarrier.Render(low, perc, music); break;
                case "signal_breach": Tracks.SignalBreach.Render(low, perc, music); break;
                case "carbon_copy": Tracks.CarbonCopy.Render(low, perc, music); break;
                case "cold_cathode": Tracks.ColdCathode.Render(low, perc, music); break;
                case "night_tram": Tracks.NightTram.Render(low, perc, music); break;
                case "freight_lift": Tracks.FreightLift.Render(low, perc, music); break;
                case "return_duct": Tracks.ReturnDuct.Render(low, perc, music); break;
                case "ratchet_works": Tracks.RatchetWorks.Render(low, perc, music); break;
                case "phase_fault": Tracks.PhaseFault.Render(low, perc, music); break;
                case "mirror_shunt": Tracks.MirrorShunt.Render(low, perc, music); break;
                case "amber_bloom": Tracks.AmberBloom.Render(low, perc, music); break;
                case "void_static": Tracks.VoidStatic.Render(low, perc, music); break;
                case "glass_rain": Tracks.GlassRain.Render(low, perc, music); break;
                case "redline_siren": Tracks.RedlineSiren.Render(low, perc, music); break;
                default:
                    throw new System.InvalidOperationException("no renderer for " + stem);
            }
        }
    }
}
