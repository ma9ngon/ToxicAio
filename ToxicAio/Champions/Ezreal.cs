using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using SharpDX;
using SharpDX.Direct3D9;
using SebbyLibPorted;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using EnsoulSharp.SDK.Utility;
using SebbyLib;
using SebbyLibPorted.Prediction;
using SharpDX.DXGI;
using ToxicAio.Utils;
using HitChance = SebbyLibPorted.Prediction.HitChance;
using OktwCommon = SebbyLib.OktwCommon;
using PredictionInput = SebbyLibPorted.Prediction.PredictionInput;

namespace ToxicAio.Champions
{
    public class Ezreal
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuM, menuP, menuL, menuK, menuD;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;
        private static readonly HpBarIndicator Indicator = new HpBarIndicator();

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Ezreal")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 1200f);
            Q.SetSkillshot(0.25f, 60f, 2000f, true, SpellType.Line);
            W = new Spell(SpellSlot.W, 1200f);
            W.SetSkillshot(0.25f, 80f, 1700f, false, SpellType.Line);
            E = new Spell(SpellSlot.E, 475f);
            R = new Spell(SpellSlot.R, 25000f);
            R.SetSkillshot(1f, 160f, 2000f, false, SpellType.Line);

            Config = new Menu("Ezreal", "[ToxicAio Reborn]: Ezreal", true);

            menuQ = new Menu("Qsettings", "Q settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo", true));
            Config.Add(menuQ);
            
            menuW = new Menu("Wsettings", "W settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo", true));
            menuW.Add(new MenuBool("Wforce", "Force focus W target", true));
            Config.Add(menuW);

            menuR = new Menu("Rsettings", "R settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo", true));
            menuR.Add(new MenuBool("Raa", "Dont use R when target is in AA Range", true));
            menuR.Add(new MenuSlider("Rrange", "R Range", 2000, 1000, 10000));
            Config.Add(menuR);

            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));
            menuP.Add(new MenuList("WPred", "W Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));
            menuP.Add(new MenuList("RPred", "R Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal Settings", true);
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal", true));
            menuK.Add(new MenuBool("KsE", "use E to Killsteal", true));
            menuK.Add(new MenuBool("KsR", "use R to Killsteal", true));
            Config.Add(menuK);

            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuSeparator("Lane", "Lane clear"));
            menuL.Add(new MenuBool("LcQ", "use Q to Lane clear", true));
            menuL.Add(new MenuSeparator("jungler", "Jungle clear"));
            menuL.Add(new MenuBool("JcQ", "use Q to Jungle clear", true));
            Config.Add(menuL);
            
            menuD = new Menu("Draw", "Draw settings");
            menuD.Add(new MenuBool("drawQ", "Q Range  (White)", true));
            menuD.Add(new MenuBool("drawW", "W Range  (Blue)", true));
            menuD.Add(new MenuBool("drawE", "E Range (Green)", true));
            menuD.Add(new MenuBool("drawR", "R Range  (Red)", true));
            menuD.Add(new MenuBool("drawIn", "Draw Damage Indicator", true));
            Config.Add(menuD);
            
            Config.Attach();
            GameEvent.OnGameTick += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            Drawing.OnEndScene += DrawingOnEnd;
            Game.OnUpdate += killstealLogic;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }
        
        private static void OnGameUpdate(EventArgs args)
        {
            var forcetarget =
                GameObjects.EnemyHeroes.FirstOrDefault(x =>
                    x.HasBuff("ezrealwattach") && x.IsValidTarget(Q.Range));

            if (forcetarget != null && Orbwalker.ActiveMode == OrbwalkerMode.Combo &&
                Orbwalker.GetTarget() != forcetarget && Config["Wsettings"].GetValue<MenuBool>("Wforce").Enabled)
            {
                Orbwalker.ForceTarget = forcetarget;
            }
            
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicW();
                LogicQ();
                LogicR();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Laneclear();
                Jungle();
            }
        }

        private static void killstealLogic(EventArgs args)
        {
            Killsteal();
        }

        private static void LogicQ()
        {
            var qtarget = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");
            if (qtarget == null) return;
            
            switch (comb(menuP, "QPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (qtarget.InRange(Q.Range))
            {
                if (Q.IsReady() && qtarget.IsValidTarget(Q.Range) && useQ.Enabled && !Me.IsWindingUp)
                {
                    var qpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(Q, qtarget);
                    if (qpred.Hitchance >= hitchance)
                    {
                        Q.Cast(qpred.UnitPosition);
                    }
                }
            }
        }

        private static void LogicW()
        {
            var wtarget = TargetSelector.GetTarget(W.Range, DamageType.Magical);
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");
            if (wtarget == null) return;
            
            switch (comb(menuP, "WPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (wtarget.InRange(W.Range))
            {
                if (useW.Enabled && W.IsReady() && wtarget.IsValidTarget(W.Range))
                {
                    var wpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(W, wtarget);
                    if (wpred.Hitchance >= hitchance)
                    {
                        W.Cast(wpred.UnitPosition);
                    }
                }
            }
        }

        private static void LogicR()
        {
            var rtarget = TargetSelector.GetTarget(R.Range, DamageType.Magical);
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var raa = Config["Rsettings"].GetValue<MenuBool>("Raa");
            var range = Config["Rsettings"].GetValue<MenuSlider>("Rrange");
            if (rtarget == null) return;
            
            switch (comb(menuP, "RPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (raa.Enabled && rtarget.InAutoAttackRange())
            {
                return;
            }
            
            if (rtarget.InRange(range.Value))
            {
                if (R.IsReady() && useR.Enabled && rtarget.IsValidTarget(range.Value) && rtarget.HealthPercent <= Me.GetSpellDamage(rtarget, SpellSlot.R) + Me.GetSpellDamage(rtarget, SpellSlot.Q))
                {
                    var rpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(R, rtarget);
                    if (rpred.Hitchance >= hitchance)
                    {
                        R.Cast(rpred.UnitPosition);
                    }
                }
            }
        }

        private static void Jungle()
        {
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
            }
        }
        
        private static void Laneclear()
        {
            var lcq = Config["Clear"].GetValue<MenuBool>("LcQ");

            if (lcq.Enabled && Q.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                
                if (minions.Any())
                {
                    var qFarmLoaction = Q.GetLineFarmLocation(minions);
                    if (qFarmLoaction.Position.IsValid())
                    {
                        Q.Cast(qFarmLoaction.Position);
                        return;
                    }
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;
            foreach (var target in GameObjects.EnemyHeroes.Where(hero => hero.IsValidTarget(Q.Range) && !hero.HasBuff("JudicatorIntervention") && !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage")))
            {
                if (ksQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                {
                    if (target != null)
                    {
                        if (target.DistanceToPlayer() <= Q.Range)
                        {
                            if (target.Health + target.AllShield <= Me.GetSpellDamage(target, SpellSlot.Q))
                            {
                                var qpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(Q, target);
                                if (qpred.Hitchance >= HitChance.High)
                                {
                                    Q.Cast(qpred.UnitPosition);
                                }
                            }
                        }
                    }
                }
                
                if (ksE && E.IsReady() && target.IsValidTarget(730f))
                {
                    if (target != null)
                    {
                        if (target.DistanceToPlayer() <= 730f)
                        {
                            if (target.Health + target.AllShield <= Me.GetSpellDamage(target, SpellSlot.E))
                            {
                                E.Cast(target.Position);
                            }
                        }
                    }
                }

                foreach (var target2 in GameObjects.EnemyHeroes.Where(hero =>
                             hero.IsValidTarget(R.Range) && !hero.HasBuff("JudicatorIntervention") &&
                             !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage")))
                {
                    var range = Config["Rsettings"].GetValue<MenuSlider>("Rrange");
                    if (ksR && R.IsReady() && target.IsValidTarget(range.Value))
                    {
                        if (target2 != null)
                        {
                            if (target2.DistanceToPlayer() <= range.Value)
                            {
                                if (target2.Health + target.AllShield <= Me.GetSpellDamage(target2, SpellSlot.R))
                                {
                                    var rpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(R, target2);
                                    if (rpred.Hitchance >= HitChance.High)
                                    {
                                        R.Cast(rpred.UnitPosition);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (Config["Draw"].GetValue<MenuBool>("drawQ").Enabled )
            {
                Drawing.DrawCircle(Me.Position, Q.Range, 2, System.Drawing.Color.White);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawW").Enabled)
            {
                Drawing.DrawCircle(Me.Position, W.Range, 2, System.Drawing.Color.Blue);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled)
            {
                Drawing.DrawCircle(Me.Position, E.Range, 2, System.Drawing.Color.Green);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled)
            {
                var range = Config["Rsettings"].GetValue<MenuSlider>("Rrange");
                Drawing.DrawCircle(Me.Position, range.Value, 2, System.Drawing.Color.Red);
            }
        }
        
        private static void DrawingOnEnd(EventArgs args)
        {
            foreach (
                var enemy in ObjectManager.Get<AIHeroClient>()
                    .Where(x => x.IsValidTarget() && !x.IsDead))
                if (Config["Draw"].GetValue<MenuBool>("drawIn").Enabled)
                {
                    Indicator.unit = enemy;
                    Indicator.drawDmg(GetComboDamage(enemy), new ColorBGRA(255, 204, 0, 170));
                }
        }
        
        private static float GetComboDamage(AIHeroClient target)
        {
            var Damage = 0d;
            if (Q.IsReady())
            {
                Damage += Q.GetDamage(target);
            }
            
            if (W.IsReady())
            {
                Damage += W.GetDamage(target);
            }

            if (E.IsReady())
            {
                Damage += E.GetDamage(target, 0);
            }

            if (R.IsReady())
            {
                Damage += R.GetDamage(target, 0);
            }

            return (float)Damage;
        }
    }
}