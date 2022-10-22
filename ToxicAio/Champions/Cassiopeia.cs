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
    public class Cassiopeia
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuM, menuP, menuL, menuK, menuD;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;
        private static readonly HpBarIndicator Indicator = new HpBarIndicator();

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Cassiopeia")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 850f);
            Q.SetSkillshot(0.25f, 30f, float.MaxValue, false, SpellType.Circle);
            W = new Spell(SpellSlot.W, 700f);
            W.SetSkillshot(0.25f, 25f, float.MaxValue, false, SpellType.Circle);
            E = new Spell(SpellSlot.E, 700f);
            E.SetTargetted(0.125f, 2500f);
            R = new Spell(SpellSlot.R, 825f);
            R.SetSkillshot(0.5f, 40f, float.MaxValue, false, SpellType.Cone);

            Config = new Menu("Cassiopeia", "[ToxicAio Reborn]: Cassiopeia", true);

            menuQ = new Menu("Qsettings", "Q settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo", true));
            menuQ.Add(new MenuBool("autoQ", "Auto Q Dashing Target", false));
            menuQ.Add(new MenuKeyBind("Qpois", "Use Q Only When target is not Poisoned", Keys.G, KeyBindType.Toggle)).AddPermashow();
            Config.Add(menuQ);
            
            menuW = new Menu("Wsettings", "W settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo", true));
            menuW.Add(new MenuSlider("WHP", "HP % To use W", 50, 1, 100));
            Config.Add(menuW);
            
            menuE = new Menu("Esettings", "E settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo", true));
            menuE.Add(new MenuKeyBind("Epois", "Use E only when target is Poisoned", Keys.T, KeyBindType.Toggle)).AddPermashow();
            Config.Add(menuE);
            
            menuR = new Menu("Rsettings", "R settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo", true));
            menuR.Add(new MenuSlider("RHP", "HP % To use R", 50, 1, 100));
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
            Config.Add(menuK);
            
            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuBool("Int", "Interrupter", true));
            menuM.Add(new MenuBool("AG", "Antigapcloser", true));
            menuM.Add(new MenuBool("AA", "Disable AutoAttacks at Level 6", true));
            Config.Add(menuM);
            
            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuSeparator("Lane", "Lane clear"));
            menuL.Add(new MenuBool("LcQ", "use Q to Lane clear", true));
            menuL.Add(new MenuBool("LcE", "use E to Last Hit", true));
            menuL.Add(new MenuSeparator("jungler", "Jungle clear"));
            menuL.Add(new MenuBool("JcQ", "use Q to Jungle clear", true));
            menuL.Add(new MenuBool("JcE", "use E to Jungle clear", true));
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
            AntiGapcloser.OnGapcloser += OnGapCloser;
            Interrupter.OnInterrupterSpell += Interrupter_OnInterrupterSpell;
            Dash.OnDash += OnDash;
            Orbwalker.OnBeforeAttack += BeforeAA;
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
                LogicW();
                LogicQ();
                LogicE();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Laneclear();
                LastHit();
                Jungle();
            }
            Killsteal();
        }

        private static void LogicQ()
        {
            var qtarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");
            var qhotkey = Config["Qsettings"].GetValue<MenuKeyBind>("Qpois");
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
                if (Q.IsReady() && qtarget.IsValidTarget(Q.Range) && useQ.Enabled && qhotkey.Active)
                {
                    if (!qtarget.HasBuff("cassiopeiaqdebuff") && !qtarget.HasBuff("cassiopeiawpoison"))
                    {
                        var qpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(Q, qtarget);
                        if (qpred.Hitchance >= hitchance)
                        {
                            Q.Cast(qpred.UnitPosition);
                        }
                    }
                }
            }

            if (qtarget.InRange(Q.Range))
            {
                if (Q.IsReady() && qtarget.IsValidTarget(Q.Range) && useQ.Enabled && !qhotkey.Active)
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
            var whp = Config["Wsettings"].GetValue<MenuSlider>("WHP");
            if (wtarget == null) return;
            
            switch (comb(menuP, "WPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (wtarget.InRange(E.Range))
            {
                if (useW.Enabled && W.IsReady() && wtarget.IsValidTarget(W.Range))
                {
                    if (wtarget.HealthPercent <= whp.Value)
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

        private static void LogicE()
        {
            var etarget = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            var estack = Config["Esettings"].GetValue<MenuKeyBind>("Epois");
            if (etarget == null) return;

            if (etarget.InRange(E.Range))
            {
                if (E.IsReady() && useE.Enabled && estack.Active && etarget.IsValidTarget(E.Range))
                {
                    if (etarget.HasBuff("cassiopeiaqdebuff") || etarget.HasBuff("cassiopeiawpoison"))
                    {
                        E.Cast(etarget);
                    }
                }
                else if (E.IsReady() && useE.Enabled && !estack.Active && etarget.IsValidTarget(E.Range))
                {
                    E.Cast(etarget);
                }
            }
        }

        private static void LogicR()
        {
            var rtarget = TargetSelector.GetTarget(R.Range, DamageType.Magical);
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var rhp = Config["Rsettings"].GetValue<MenuSlider>("RHP");
            if (rtarget == null) return;
            
            switch (comb(menuP, "RPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (rtarget.InRange(R.Range))
            {
                if (R.IsReady() && useR.Enabled && rtarget.IsValidTarget(R.Range) && rtarget.IsFacing(Me) && rtarget.HealthPercent <= rhp.Value)
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
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range) E.Cast(mob);
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
                    var qFarmLoaction = Q.GetCircularFarmLocation(minions);
                    if (qFarmLoaction.Position.IsValid())
                    {
                        Q.Cast(qFarmLoaction.Position);
                        return;
                    }
                }
            }
        }
        
        private static void LastHit()
        {
            if (Config["Clear"].GetValue<MenuBool>("LcE").Enabled)
            {
                var allMinions = GameObjects.EnemyMinions.Where(x => x.IsMinion() && !x.IsDead)
                    .OrderBy(x => x.Distance(ObjectManager.Player.Position));

                foreach (var min in allMinions.Where(x => x.IsValidTarget(E.Range) && x.Health < E.GetDamage(x)))
                {
                    Orbwalker.ForceTarget = min;
                    E.Cast(min);
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;
            foreach (var target in GameObjects.EnemyHeroes.Where(hero => hero.IsValidTarget(Q.Range) && !hero.HasBuff("JudicatorIntervention") && !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage")))
            {
                if (ksQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                {
                    if (target != null)
                    {
                        if (target.DistanceToPlayer() <= Q.Range)
                        {
                            if (target.Health + target.AllShield <= OktwCommon.GetKsDamage(target, Q))
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

                if (ksE && E.IsReady() && target.IsValidTarget(E.Range))
                {
                    if (target != null)
                    {
                        if (target.DistanceToPlayer() <= E.Range)
                        {
                            if (target.Health + target.AllShield <= OktwCommon.GetKsDamage(target, E))
                            {
                                E.Cast(target);
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
                Drawing.DrawCircle(Me.Position, R.Range, 2, System.Drawing.Color.Red);
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

        private static void Interrupter_OnInterrupterSpell(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("Int").Enabled)
            {
                if (Me.Distance(sender.ServerPosition) < R.Range && sender.IsFacing(Me))
                {
                    R.Cast(sender);
                }
            }
        }
        
        private static void OnGapCloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("AG").Enabled)
            {
                if (OktwCommon.CheckGapcloser(sender, args))
                {
                    if (Me.Distance(sender.PreviousPosition) < R.Range && sender.IsFacing(Me))
                    {
                        E.Cast(sender);
                    }
                }
            }
        }

        private static void OnDash(AIBaseClient sender, Dash.DashArgs e)
        {
            var useea = Config["Qsettings"].GetValue<MenuBool>("autoQ");
            var spred = SebbyLibPorted.Prediction.Prediction.GetPrediction(E, sender);
            
            if (!Q.IsReady() || !sender.IsEnemy || !useea.Enabled) return;
            if (e.EndPos.IsValid() && E.IsInRange(e.EndPos))
            {
                if (spred.Hitchance >= HitChance.Dashing)
                {
                    Q.Cast(spred.CastPosition);
                }
            }
        }

        private static void BeforeAA(object sender, BeforeAttackEventArgs args)
        {
            var aa = Config["Misc"].GetValue<MenuBool>("AA");
            if (aa.Enabled)
            {
                if (Me.Level >= 6)
                {
                    if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                    {
                        args.Process = false;
                    }
                }
            }
        }
    }
}