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

namespace ToxicAio.Champions
{
    public class Xerath
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuM, menuP, menuL, menuK, menuD;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;
        private static readonly HpBarIndicator Indicator = new HpBarIndicator();

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Xerath")
            {
                return;
            }
            
            Q = new Spell(SpellSlot.Q, 735f);
            Q.SetSkillshot(0.60f, 70f, float.MaxValue, false, SpellType.Line);
            Q.SetCharged("XerathArcanopulseChargeUp", "XerathArcanopulseChargeUp", 735, 1450, 1.5f);
            W = new Spell(SpellSlot.W, 1000f);
            W.SetSkillshot(0.25f, 137f, float.MaxValue, false, SpellType.Circle);
            E = new Spell(SpellSlot.E, 1125f);
            E.SetSkillshot(0.25f, 60f, 1400f, true, SpellType.Line);
            R = new Spell(SpellSlot.R, 5000f);
            R.SetSkillshot(0.70f, 100f, float.MaxValue, false, SpellType.Circle);
            
            Config = new Menu("Xerath", "[ToxicAio Reborn]: Xerath", true);

            menuQ = new Menu("Qsettings", "Q settings");
            menuQ.Add(new MenuBool("UseQ", "Use Q in Combo", true));
            Config.Add(menuQ);
            
            menuW = new Menu("Wsettings", "W settings");
            menuW.Add(new MenuBool("UseW", "use W in Combo", true));
            menuW.Add(new MenuList("Wmode", "W Mode",
                new string[] { "Q-W-E", "W-Q-E", "W-E-Q", "E-W-Q" }, 1));
            Config.Add(menuW);
            
            menuE = new Menu("Esettings", "E settings");
            menuE.Add(new MenuBool("UseE", "use E in Combo", true));
            menuE.Add(new MenuBool("AE", "Auto E on Dashing Target", true));
            Config.Add(menuE);
            
            menuR = new Menu("Rsettings", "R settings");
            menuR.Add(new MenuBool("UseR", "Use R Shots", true));
            menuR.Add(new MenuSlider("Cusor", "Cusor Range", 400, 0, 2000));
            menuR.Add(new MenuSlider("Rms", "R Delay", 100, 0, 1000));
            menuR.Add(new MenuKeyBind("semiR", "Semi R Key", Keys.T, KeyBindType.Press));
            Config.Add(menuR);

            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));
            menuP.Add(new MenuList("WPred", "W Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));
            menuP.Add(new MenuList("EPred", "E Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));
            menuP.Add(new MenuList("RPred", "R Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal Settings", true);
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal", true));
            menuK.Add(new MenuBool("KsW", "use W to Killsteal", true));
            menuK.Add(new MenuBool("KsE", "use E to Killsteal", true));
            Config.Add(menuK);
            
            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuBool("AG", "AntiGapcloser", true));
            menuM.Add(new MenuBool("Int", "Interrupter", true));
            Config.Add(menuM);
            
            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuBool("LcW", "use W to Lane clear", true));
            menuL.Add(new MenuBool("JcW", "use W to Jungle clear", true));
            menuL.Add(new MenuBool("JcE", "use E to Jungle clear", true));
            Config.Add(menuL);
            
            menuD = new Menu("Draw", "Draw settings");
            menuD.Add(new MenuBool("drawQ", "Q Range  (White)", true));
            menuD.Add(new MenuBool("drawW", "W Range  (Blue)", true));
            menuD.Add(new MenuBool("drawE", "E Range (Green)", true));
            menuD.Add(new MenuBool("drawR", "R Range  (Red)", true));
            menuD.Add(new MenuBool("drawIn", "Draw Damage Indicator", true));
            menuD.Add(new MenuBool("drawCusor", "R Cusor Range  (Red)", true));
            menuD.Add(new MenuBool("drawKill", "R Killable Message", true));
            Config.Add(menuD);
            
            Config.Attach();
            GameEvent.OnGameTick += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnGapcloser += OnGapCloser;
            Interrupter.OnInterrupterSpell += Interrupter_OnInterrupterSpell;
            Dash.OnDash += OnDash;
            Drawing.OnEndScene += DrawingOnEnd;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }
        
        private static void OnGameUpdate(EventArgs args)
        {
            if (ObjectManager.Player.HasBuff("XerathLocusOfPower2"))
            {
                Orbwalker.AttackEnabled = false;
                Orbwalker.MoveEnabled = false;
                
                switch (comb(menuP, "RPred"))
                {
                    case 0: hitchance = HitChance.Low; break;
                    case 1: hitchance = HitChance.Medium; break;
                    case 2: hitchance = HitChance.High; break;
                    case 3: hitchance = HitChance.VeryHigh; break;
                    default: hitchance = HitChance.High; break;
                }

                if (Config["Rsettings"].GetValue<MenuKeyBind>("semiR").Active)
                {
                    var targets = GameObjects.EnemyHeroes.Where(i =>
                        i.Distance(Game.CursorPos) <= Config["Rsettings"].GetValue<MenuSlider>("Cusor").Value &&
                        !i.IsDead).OrderBy(i => i.Health);

                    if (targets != null)
                    {
                        var delay = Config["Rsettings"].GetValue<MenuSlider>("Rms");
                        var target = targets.Find(i =>
                            i.DistanceToCursor() <= Config["Rsettings"].GetValue<MenuSlider>("Cusor").Value);
                        
                        if (target != null)
                        {
                            var rpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(R, target);
                            if (rpred.Hitchance >= hitchance)
                            {
                                if (Variables.GameTimeTickCount - R.LastCastAttemptTime >= delay.Value)
                                {
                                    R.Cast(rpred.UnitPosition);
                                }
                            }
                        }
                    }
                }
            }

            if (!Me.HasBuff("XerathLocusOfPower2"))
            {
                Orbwalker.AttackEnabled = true;
                Orbwalker.MoveEnabled = true;
            }

            if (Q.IsCharging && Orbwalker.ActiveMode != OrbwalkerMode.None)
            {
                Orbwalker.AttackEnabled = false;
                ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }


            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                    switch (comb(menuW, "Wmode"))
                    {
                        case 0:
                            LogicQ();
                            LogicW();
                            LogicE();
                            LogicR();
                            break;

                        case 1:
                            LogicW();
                            LogicQ();
                            LogicE();
                            LogicR();
                            break;

                        case 2:
                            LogicW();
                            LogicE();
                            LogicQ();
                            LogicR();
                            break;

                        case 3:
                            LogicE();
                            LogicW();
                            LogicQ();
                            LogicR();
                            break;
                    }

            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                    Laneclear();
                    Jungle();
            }
            Killsteal();
        }

        private static void LogicQ()
        {
            var qtarget = TargetSelector.GetTarget(Q.ChargedMaxRange, DamageType.Magical);
            var useQ = Config["Qsettings"].GetValue<MenuBool>("UseQ");
            if (qtarget == null) return;

            switch (comb(menuP, "QPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (!Me.HasBuff("XerathLocusOfPower2"))
            {
                if (qtarget.InRange(Q.ChargedMaxRange) && useQ.Enabled)
                {
                    if (Q.IsReady() && qtarget.IsValidTarget(Q.ChargedMaxRange))
                    {
                        var qpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(Q, qtarget);
                        if (qpred.Hitchance >= hitchance)
                        {
                            Q.StartCharging();
                        }
                    }

                    if (Q.IsReady() && Q.IsCharging)
                    {
                        var target = TargetSelector.GetTarget(Q.ChargedMaxRange, DamageType.Magical);
                        if (target != null && target.IsValidTarget(Q.ChargedMaxRange))
                        {
                            var qpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(Q, qtarget);
                            if (qpred.Hitchance >= HitChance.High)
                            {
                                Q.ShootChargedSpell(qpred.UnitPosition);
                            }
                        }
                    }
                }
            }
        }

        private static void LogicW()
        {
            var wtarget = TargetSelector.GetTarget(W.Range, DamageType.Magical);
            var useW = Config["Wsettings"].GetValue<MenuBool>("UseW");
            if (wtarget == null) return;
            
            switch (comb(menuP, "WPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }
            
            if (!Me.HasBuff("XerathLocusOfPower2"))
            {
                if (wtarget.InRange(W.Range))
                {
                    if (useW.Enabled)
                    {
                        if (W.IsReady() && wtarget.IsValidTarget(W.Range))
                        {
                            var wpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(W, wtarget);
                            if (wpred.Hitchance >= hitchance)
                            {
                                W.Cast(wpred.UnitPosition);
                            }
                        }
                    }
                }
            }
        }

        private static void LogicE()
        {
            var etarget = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            var useE = Config["Esettings"].GetValue<MenuBool>("UseE");
            if (etarget == null) return;

            switch (comb(menuP, "EPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (!Me.HasBuff("XerathLocusOfPower2"))
            {
                if (etarget.InRange(E.Range) && useE.Enabled)
                {
                    if (E.IsReady() && etarget.IsValidTarget(E.Range))
                    {
                        var epred = SebbyLibPorted.Prediction.Prediction.GetPrediction(E, etarget);
                        if (epred.Hitchance >= hitchance)
                        {
                            E.Cast(epred.UnitPosition);
                        }
                    }
                }
            }
        }

        private static void LogicR()
        {
            switch (comb(menuP, "RPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }
            
            if (Me.HasBuff("XerathLocusOfPower2"))
            {
                if (Config["Rsettings"].GetValue<MenuBool>("UseR").Enabled)
                {
                    var targets = GameObjects.EnemyHeroes.Where(i =>
                        i.Distance(Game.CursorPos) <= Config["Rsettings"].GetValue<MenuSlider>("Cusor").Value &&
                        !i.IsDead).OrderBy(i => i.Health);

                    if (targets != null)
                    {
                        var delay = Config["Rsettings"].GetValue<MenuSlider>("Rms");
                        var target = targets.Find(i =>
                            i.DistanceToCursor() <= Config["Rsettings"].GetValue<MenuSlider>("Cusor").Value);

                        if (target != null)
                        {
                            var rpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(R, target);
                            if (rpred.Hitchance >= hitchance)
                            {
                                if (Variables.GameTimeTickCount - R.LastCastAttemptTime >= delay.Value)
                                {
                                    R.Cast(rpred.UnitPosition);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void Jungle()
        {
            var JcWw = Config["Clear"].GetValue<MenuBool>("JcW");
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcWw.Enabled && W.IsReady() && Me.Distance(mob.Position) < W.Range) W.Cast(mob.Position);
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range) E.Cast(mob.Position);
            }
        }
        
        private static void Laneclear()
        {
            var lcw = Config["Clear"].GetValue<MenuBool>("LcW");
            if (lcw.Enabled && W.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var wFarmLoaction = W.GetCircularFarmLocation(minions);
                    if (wFarmLoaction.Position.IsValid())
                    {
                        W.Cast(wFarmLoaction.Position);
                        return;
                    }
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;
            
            var enemies = GameObjects.EnemyHeroes.Where(x => x != null && x.IsVisibleOnScreen && x.IsValidTarget() && !x.HasBuff("UndyingRage") && !x.IsInvulnerable && !x.HasBuffOfType(BuffType.UnKillable) && !x.IsDead && !x.HasBuffOfType(BuffType.Invulnerability));
            foreach (AIHeroClient enemyHero in enemies)
            {
                var qpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(Q, enemyHero);
                if (!Me.HasBuff("XerathLocusOfPower2"))
                {
                    if (enemyHero.InRange(Q.ChargedMaxRange) && ksQ)
                    {
                        if (Q.IsReady() && enemyHero.IsValidTarget(Q.ChargedMaxRange) && Me.GetSpellDamage(enemyHero, SpellSlot.Q) >= enemyHero.Health)
                        {
                            if (qpred.Hitchance >= HitChance.High)
                            {
                                Q.StartCharging();
                            }
                        }

                        if (Q.IsReady() && Q.IsCharging)
                        {
                            var target = TargetSelector.GetTarget(Q.ChargedMaxRange, DamageType.Magical);
                            if (target != null && target.IsValidTarget(Q.ChargedMaxRange))
                            {
                                if (qpred.Hitchance >= HitChance.High)
                                {
                                    Q.ShootChargedSpell(qpred.UnitPosition);
                                }
                            }
                        }
                    }
                }
                
                if (!Me.HasBuff("XerathLocusOfPower2"))
                {
                    if (W.IsReady() && ksW && enemyHero.DistanceToPlayer() <= W.Range &&
                        Me.GetSpellDamage(enemyHero, SpellSlot.W) >= enemyHero.Health)
                    {
                        var wpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(W, enemyHero);
                        if (wpred.Hitchance >= HitChance.High)
                        {
                            W.Cast(wpred.UnitPosition);
                        }
                    }
                }

                if (!Me.HasBuff("XerathLocusOfPower2"))
                {
                    if (E.IsReady() && ksE && enemyHero.DistanceToPlayer() <= E.Range &&
                    Me.GetSpellDamage(enemyHero, SpellSlot.E) >= enemyHero.Health)
                    {
                        var epred = SebbyLibPorted.Prediction.Prediction.GetPrediction(E, enemyHero);
                        if (epred.Hitchance >= HitChance.High)
                        {
                            E.Cast(epred.UnitPosition);
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
                Drawing.DrawCircle(Me.Position, R.Range, 2, System.Drawing.Color.Red);
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawCusor").Enabled)
            {
                Drawing.DrawCircleIndicator(Game.CursorPos, Config["Rsettings"].GetValue<MenuSlider>("Cusor").Value, System.Drawing.Color.Red);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawKill").Enabled)
            {
                var t = TargetSelector.GetTarget(R.Range, DamageType.Magical);

                if (R.Level >= 0 && R.Level <= 2)
                {
                    if (t.IsValidTarget() && R.IsReady() && R.GetDamage(t) * 3 >= t.Health)
                    {
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.5f, System.Drawing.Color.Red, "Ult can kill: " + t.CharacterName + " have: " + t.Health + " HP");
                        Drawing.DrawLine(Drawing.WorldToScreen(Me.Position), Drawing.WorldToScreen(t.Position), 5, System.Drawing.Color.Red);
                    }
                }

                if (R.Level >= 1 && R.Level <= 3)
                {
                    if (t.IsValidTarget() && R.IsReady() && R.GetDamage(t) * 4 >= t.Health)
                    {
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.5f, System.Drawing.Color.Red, "Ult can kill: " + t.CharacterName + " have: " + t.Health + " HP");
                        Drawing.DrawLine(Drawing.WorldToScreen(Me.Position), Drawing.WorldToScreen(t.Position), 5, System.Drawing.Color.Red);
                    }
                }

                if (R.Level >= 2 && R.Level <= 4)
                {
                    if (t.IsValidTarget() && R.IsReady() && R.GetDamage(t) * 5 >= t.Health)
                    {
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.5f, System.Drawing.Color.Red, "Ult can kill: " + t.CharacterName + " have: " + t.Health + " HP");
                        Drawing.DrawLine(Drawing.WorldToScreen(Me.Position), Drawing.WorldToScreen(t.Position), 5, System.Drawing.Color.Red);
                    }
                }
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
                Damage += E.GetDamage(target);
            }

            if (R.IsReady())
            {
                Damage += R.GetDamage(target);
            }
            
            return (float)Damage;
        }
        
        private static void OnDash(AIBaseClient sender, Dash.DashArgs args)
        {
            var useea = Config["Esettings"].GetValue<MenuBool>("AE");

            if (!E.IsReady() || !sender.IsEnemy || !useea.Enabled) return;
            if (args.EndPos.DistanceToPlayer() > E.Range || Invulnerable.Check(sender as AIHeroClient))
            {
                return;
            }

            E.Cast(args.EndPos);
        }
        
        private static void OnGapCloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("AG").Enabled)
            {
                if (OktwCommon.CheckGapcloser(sender, args))
                {
                    if (Me.Distance(sender.PreviousPosition) < E.Range)
                    {
                        E.Cast(sender);
                    }
                }
            }
        }
        
        private static void Interrupter_OnInterrupterSpell(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("Int").Enabled)
            {
                if (Me.Distance(sender.ServerPosition) < E.Range)
                {
                    E.Cast(sender);
                }
            }
        }
    }
}