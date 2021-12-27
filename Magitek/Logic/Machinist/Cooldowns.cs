using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.Account;
using Magitek.Models.Machinist;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;
using MachinistRoutine = Magitek.Utilities.Routines.Machinist;

namespace Magitek.Logic.Machinist
{
    internal static class Cooldowns
    {
        public static async Task<bool> BarrelStabilizer()
        {
            if (!MachinistRoutine.ToggleAndSpellCheck(MachinistSettings.Instance.UseBarrelStabilizer, Spells.BarrelStabilizer))
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!MachinistRoutine.IsInWeaveingWindow)
                return false;

            if (!Spells.BarrelStabilizer.IsReady())
                return false;

            if (ActionResourceManager.Machinist.Heat > 30 && Spells.Wildfire.IsReady(8000))
                return false;

            if (ActionResourceManager.Machinist.Heat >= 35 && Spells.Wildfire.Cooldown.TotalMilliseconds > 8000)
                return false;

            if (Spells.Hypercharge.Cooldown.TotalMilliseconds > 8000 && Casting.LastSpell == Spells.HeatBlast && Spells.GaussRound.Charges < 1.2f && Spells.Ricochet.Charges < 1.2f)
                return await Spells.BarrelStabilizer.Cast(Core.Me);

            if (ActionResourceManager.Machinist.Heat < 50)
            {
                if (MachinistSettings.Instance.UseDrill && Spells.Drill.IsReady(8000) && MachinistSettings.Instance.UseWildfire && Spells.Wildfire.IsReady(4000))
                    return false;

                if (MachinistSettings.Instance.UseHotAirAnchor && Spells.AirAnchor.IsReady(8000) && MachinistSettings.Instance.UseWildfire && Spells.Wildfire.IsReady(4000))
                    return false;
            }

            Logger.WriteInfo($@"Using BarrelStabilizer with {ActionResourceManager.Machinist.Heat} Heat.");
            return await Spells.BarrelStabilizer.Cast(Core.Me);
        }

        public static async Task<bool> Hypercharge()
        {

            if (!MachinistRoutine.ToggleAndSpellCheck(MachinistSettings.Instance.UseHypercharge, Spells.Hypercharge))
                return false;

            if (!Spells.Hypercharge.IsReady())
                return false;

            if (ActionResourceManager.Machinist.Heat < 50)
                return false;

            if (Core.Me.ClassLevel >= 45)
            {
                if (Core.Me.HasAura(Auras.WildfireBuff, true) && Spells.Wildfire.Cooldown.TotalMilliseconds > 117200)
                    return await Spells.Hypercharge.Cast(Core.Me);

                if (MachinistSettings.Instance.UseWildfire && Spells.Wildfire.IsReady(12000))
                    return false;

                if (!MachinistSettings.Instance.UseWildfire || !ActionManager.CurrentActions.Values.Contains(Spells.Wildfire))
                    return await Spells.Hypercharge.Cast(Core.Me);
            }

            if (MachinistSettings.Instance.UseDrill && Spells.Drill.IsReady(8000))
                return false;

            if (MachinistSettings.Instance.UseHotAirAnchor && Spells.AirAnchor.IsReady(8000))
                return false;

            if (MachinistSettings.Instance.UseChainSaw && Spells.ChainSaw.IsReady(8000))
                return false;

            if (Spells.Ricochet.Charges >= 2.5f || Spells.GaussRound.Charges >= 2.5f)
                return false;

            //Force Delay CD
            if (Spells.SplitShot.Cooldown.TotalMilliseconds > 650 + BaseSettings.Instance.UserLatencyOffset)
                return false;

            Logger.WriteInfo($@"Using Hypercharge at {Spells.SplitShot.Cooldown.TotalMilliseconds} ms before CD.");
            return await Spells.Hypercharge.Cast(Core.Me);
        }

        public static async Task<bool> Wildfire()
        {
            if (!MachinistRoutine.ToggleAndSpellCheck(MachinistSettings.Instance.UseWildfire, Spells.Wildfire))
                return false;

            if (!Spells.Wildfire.IsReady())
                return false;

            if (Core.Me.HasAura(Auras.WildfireBuff, true) || Casting.SpellCastHistory.Any(x => x.Spell == Spells.Wildfire))
                return false;

            if (MachinistSettings.Instance.UseDrill && Spells.Drill.IsReady(9000))
                return false;

            if (MachinistSettings.Instance.UseHotAirAnchor && Spells.AirAnchor.IsReady(9000))
                return false;

            if (MachinistSettings.Instance.UseChainSaw && Spells.ChainSaw.IsReady(9000))
                return false;

            if (ActionResourceManager.Machinist.Heat < 50 && ActionResourceManager.Machinist.OverheatRemaining == TimeSpan.Zero)
                return false;

            if (Weaving.GetCurrentWeavingCounter() > 0)
                return false;

            //Force Delay CD
            if (Spells.SplitShot.Cooldown.TotalMilliseconds > 1350 + BaseSettings.Instance.UserLatencyOffset)
                return false;

            Logger.WriteInfo($@"Using Wildfire at {Spells.SplitShot.Cooldown.TotalMilliseconds} ms before CD.");
            return await Spells.Wildfire.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Reassemble()
        {
            if (!MachinistRoutine.ToggleAndSpellCheck(MachinistSettings.Instance.UseReassemble, Spells.Reassemble))
                return false;

            if (!MachinistRoutine.IsInWeaveingWindow)
                return false;

            // Added check for cooldown, gets stuck at lower levels otherwise.
            if (Spells.Reassemble.Charges == 0
                && Spells.Reassemble.Cooldown != TimeSpan.Zero)
                return false;

            //If we're in AoE logic, use Reassemble for SpreadShot
            if (MachinistSettings.Instance.UseAoe && Combat.Enemies.Count(r => r.ValidAttackUnit() && r.Distance(Core.Me) <= 12 + r.CombatReach) >= 4)
                return await Spells.Reassemble.Cast(Core.Me);

            if (Core.Me.ClassLevel < 58)
            {
                if (ActionManager.LastSpell != MachinistRoutine.HeatedSlugShot)
                    return false;
            }

            if (Core.Me.ClassLevel >= 58 && Core.Me.ClassLevel < 76)
            {
                if (MachinistSettings.Instance.UseDrill && !Spells.Drill.IsReady() && Spells.Drill.Cooldown.TotalMilliseconds - 100 >= MachinistRoutine.HeatedSplitShot.Cooldown.TotalMilliseconds)
                    return false;
            }

            if (Core.Me.ClassLevel >= 76 && Core.Me.ClassLevel < 90)
            {
                if ((MachinistSettings.Instance.UseDrill && !Spells.Drill.IsReady() && Spells.Drill.Cooldown.TotalMilliseconds - 100 >= MachinistRoutine.HeatedSplitShot.Cooldown.TotalMilliseconds)
                    && (MachinistSettings.Instance.UseHotAirAnchor && !Spells.AirAnchor.IsReady() && Spells.AirAnchor.Cooldown.TotalMilliseconds - 100 >= MachinistRoutine.HeatedSplitShot.Cooldown.TotalMilliseconds))
                    return false;
            }

            if (Core.Me.ClassLevel >= 90)
            {
                if ((MachinistSettings.Instance.UseDrill && !Spells.Drill.IsReady() && Spells.Drill.Cooldown.TotalMilliseconds - 100 >= MachinistRoutine.HeatedSplitShot.Cooldown.TotalMilliseconds)
                    && (MachinistSettings.Instance.UseHotAirAnchor && !Spells.AirAnchor.IsReady() && Spells.AirAnchor.Cooldown.TotalMilliseconds - 100 >= MachinistRoutine.HeatedSplitShot.Cooldown.TotalMilliseconds)
                    && (MachinistSettings.Instance.UseChainSaw && !Spells.ChainSaw.IsReady() && Spells.ChainSaw.Cooldown.TotalMilliseconds - 100 >= MachinistRoutine.HeatedSplitShot.Cooldown.TotalMilliseconds))
                    return false;
            }

            return await Spells.Reassemble.Cast(Core.Me);
        }

    }
}