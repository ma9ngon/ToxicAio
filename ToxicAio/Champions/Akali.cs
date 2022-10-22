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
    public class Akali
    {
        private static Spell Q, W, E, E2, R, R2;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuP, menuL, menuK, menuD;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;
        private static readonly HpBarIndicator Indicator = new HpBarIndicator();

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Akali")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 550f);
            Q.SetSkillshot(0.25f, 150f, float.MaxValue, false, SpellType.Cone);
            W = new Spell(SpellSlot.W, 240f);
            E = new Spell(SpellSlot.E, 800f);
            E.SetSkillshot(0.25f, 60f, 1800f, true, SpellType.Line);
            E2 = new Spell(SpellSlot.E, 25000f);
            R = new Spell(SpellSlot.R, 675f);
            R.SetTargetted(0f, 1500f);
            R2 = new Spell(SpellSlot.R, 675f);
            R2.SetSkillshot(0f, 55f, 3000f, false, SpellType.Line);

            Config = new Menu("Akali", "[ToxicAio Reborn]: Akali", true);

            menuQ = new Menu("Qsettings", "Q settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo", true));
            Config.Add(menuQ);
            
            menuW = new Menu("Wsettings", "W settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo", true));
            Config.Add(menuW);
            
            menuE = new Menu("Esettings", "E settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo", true));
            menuE.Add(new MenuBool("useE2", "Use E2 in Combo", true));
            menuE.Add(new MenuSlider("E2range", "E2 Cast Range", 2000, 1000, 25000));
            Config.Add(menuE);
            
            menuR = new Menu("Rsettings", "R settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo", true));
            menuR.Add(new MenuBool("useR2", "Use R2 in Combo", true));
            Config.Add(menuR);

            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));
            menuP.Add(new MenuList("EPred", "E Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));
            menuP.Add(new MenuList("R2Pred", "R2 Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal Settings", true);
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal", true));
            menuK.Add(new MenuBool("KsE", "use E to Killsteal", true));
            menuK.Add(new MenuBool("KsE2", "use E2 to Killsteal", true));
            menuK.Add(new MenuBool("KsR", "use R to Killsteal", false));
            menuK.Add(new MenuBool("KsR2", "use R to Killsteal", false));
            Config.Add(menuK);
            
            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuSeparator("Lane", "Lane clear"));
            menuL.Add(new MenuBool("LcQ", "use Q to Lane clear", true));
            menuL.Add(new MenuSeparator("jungler", "Jungle clear"));
            menuL.Add(new MenuBool("JcQ", "use Q to Jungle clear", true));
            menuL.Add(new MenuBool("JcE", "use E to Jungle clear", true));
            Config.Add(menuL);
            
            menuD = new Menu("Draw", "Draw settings");
            menuD.Add(new MenuBool("drawQ", "Q Range  (White)", true));
            menuD.Add(new MenuBool("drawW", "W Range  (Blue)", true));
            menuD.Add(new MenuBool("drawE", "E Range (Green)", true));
            menuD.Add(new MenuBool("drawE2", "E2 Range (Green)", true));
            menuD.Add(new MenuBool("drawR", "R Range  (Red)", true));
            menuD.Add(new MenuBool("drawR2", "R2 Range  (Red)", true));
            menuD.Add(new MenuBool("drawIn", "Draw Damage Indicator", true));
            Config.Add(menuD);
            
            Config.Attach();
            GameEvent.OnGameTick += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            Drawing.OnEndScene += DrawingOnEnd;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }
        
        private static void OnGameUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicR();
                LogicQ();
                LogicW();
                LogicE();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Jungle();
                Laneclear();
            }
            Killsteal();
        }

        private static void LogicQ()
        {
            var qtarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
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
                if (Q.IsReady() && qtarget.IsValidTarget(Q.Range) && useQ.Enabled)
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
            var wtarget = W.GetTarget(W.Range);
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");
            if (wtarget == null) return;

            if (wtarget.InRange(W.Range))
            {
                if (useW.Enabled && W.IsReady() && wtarget.IsValidTarget(W.Range))
                {
                    W.Cast(Me.Position);
                }
            }
        }

        private static void LogicE()
        {
            var erange = Config["Esettings"].GetValue<MenuSlider>("E2range");
            var etarget = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            var etarget2 = TargetSelector.GetTarget(erange.Value, DamageType.Magical);
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            var useE2 = Config["Esettings"].GetValue<MenuBool>("useE2");
            if (etarget == null) return;
            
            switch (comb(menuP, "EPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (etarget.InRange(E.Range))
            {
                if (E.IsReady() && useE.Enabled && etarget.IsValidTarget(E.Range) && E.Name == "AkaliE")
                {
                    var epred = SebbyLibPorted.Prediction.Prediction.GetPrediction(E, etarget);
                    if (epred.Hitchance >= hitchance)
                    {
                        E.Cast(epred.UnitPosition);
                    }
                }
            }

            if (E.Name == "AkaliEb")
            {
                if (etarget2.InRange(erange.Value))
                {
                    if (E.IsReady() && useE.Enabled && etarget2.IsValidTarget(erange.Value) && useE2.Enabled)
                    {
                        if (Q.GetDamage(etarget2) + E2.GetDamage(etarget2, 1) + R.GetDamage(etarget2) +
                            R2.GetDamage(etarget2, 1) >= etarget2.Health)
                        {
                            E2.Cast();
                        }
                    }
                }
            }
        }

        private static void LogicR()
        {
            var rtarget = TargetSelector.GetTarget(R.Range, DamageType.Magical);
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var useR2 = Config["Rsettings"].GetValue<MenuBool>("useR2");
            if (rtarget == null) return;
            
            switch (comb(menuP, "R2Pred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (rtarget.InRange(R.Range))
            {
                if (R.IsReady() && useR.Enabled && rtarget.IsValidTarget(R.Range) && R.Name == "AkaliR")
                {
                    if (rtarget.HealthPercent <= 75 && Q.IsReady())
                    {
                        R.Cast(rtarget);
                    }
                }
                else if (R.IsReady() && useR2.Enabled && rtarget.IsValidTarget(R.Range) && R.Name == "AkaliRb")
                {
                    if (rtarget.HealthPercent <= 50)
                    {
                        var rpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(R2, rtarget);
                        if (rpred.Hitchance >= hitchance)
                        {
                            R2.Cast(rpred.UnitPosition);
                        }
                    }
                }
            }
        }

        private static void Jungle()
        {
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
                if (JcEe.Enabled && E.IsReady() && E.Name == "AkaliE" && Me.Distance(mob.Position) < E.Range) E.Cast(mob.Position);
                if (JcEe.Enabled && E.IsReady() && E.Name == "AkaliEb" && Me.Distance(mob.Position) < E.Range) E2.Cast();
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
            var ksE2 = Config["Killsteal"].GetValue<MenuBool>("KsE2").Enabled;
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;
            var ksR2 = Config["Killsteal"].GetValue<MenuBool>("KsR2").Enabled;
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

                if (ksE && E.IsReady() && target.IsValidTarget(E.Range) && E.Name == "AkaliE")
                {
                    if (target != null)
                    {
                        if (target.DistanceToPlayer() <= E.Range)
                        {
                            var epred = SebbyLibPorted.Prediction.Prediction.GetPrediction(E, target);
                            if (epred.Hitchance >= HitChance.High)
                            {
                                if (target.Health + target.AllShield <= Me.GetSpellDamage(target, SpellSlot.E))
                                {
                                    E.Cast(epred.UnitPosition);
                                }
                            }
                        }
                    }
                }

                if (ksE2 && E.IsReady() && target.IsValidTarget(2500) && E.Name == "AkaliEb")
                {
                    if (target != null)
                    {
                        if (target.DistanceToPlayer() <= 2500)
                        {
                            if (target.Health + target.AllShield <= Me.GetSpellDamage(target, SpellSlot.E, 1))
                            {
                                E2.Cast();
                            }
                        }
                    }
                }

                if (ksR && R.IsReady() && target.IsValidTarget(R.Range) && R.Name == "AkaliR")
                {
                    if (target != null)
                    {
                        if (target.DistanceToPlayer() <= R.Range)
                        {
                            if (target.Health + target.AllShield <= Me.GetSpellDamage(target, SpellSlot.R))
                            {
                                R.Cast(target);
                            }
                        }
                    }
                }
                
                if (ksR2 && R.IsReady() && target.IsValidTarget(R2.Range) && R.Name == "AkaliRb")
                {
                    if (target != null)
                    {
                        if (target.DistanceToPlayer() <= R2.Range)
                        {
                            var rpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(R2, target);
                            if (rpred.Hitchance >= HitChance.High)
                            {
                                if (target.Health + target.AllShield <= Me.GetSpellDamage(target, SpellSlot.R, 1))
                                {
                                    R2.Cast(target);
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

            if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled && E.Name == "AkaliE")
            {
                Drawing.DrawCircle(Me.Position, E.Range, 2, System.Drawing.Color.Green);
            }

            var erange = Config["Esettings"].GetValue<MenuSlider>("E2range");
            if (Config["Draw"].GetValue<MenuBool>("drawE2").Enabled && E.Name == "AkaliEb")
            {
                Drawing.DrawCircle(Me.Position, erange.Value, 2, System.Drawing.Color.Green);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled && R.Name == "AkaliR")
            {
                Drawing.DrawCircle(Me.Position, R.Range, 2, System.Drawing.Color.Red);
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawR2").Enabled && R.Name == "AkaliRb")
            {
                Drawing.DrawCircle(Me.Position, R2.Range, 2, System.Drawing.Color.Red);
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

            if (E.IsReady() && E.Name == "AkaliE")
            {
                Damage += E.GetDamage(target, 0);
            }
            
            if (E.IsReady() && E.Name == "AkaliEb")
            {
                Damage += E2.GetDamage(target, 1);
            }
            
            if (R.IsReady() && R.Name == "AkaliR")
            {
                Damage += R.GetDamage(target, 0);
            }
            
            if (R.IsReady() && R.Name == "AkaliRb")
            {
                Damage += R2.GetDamage(target, 1);
            }

            return (float)Damage;
        }
    }
}