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
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using EnsoulSharp.SDK.Utility;
using SebbyLib;
using SebbyLibPorted.Prediction;
using SharpDX.DXGI;
using ToxicAio.Utils;
using AIHeroClient = EnsoulSharp.AIHeroClient;
using HitChance = SebbyLibPorted.Prediction.HitChance;
using OktwCommon = SebbyLib.OktwCommon;
using PredictionInput = SebbyLibPorted.Prediction.PredictionInput;

namespace ToxicAio.Champions
{
    public class LeeSin
    {
        private static Spell Q, Q2, W, E, R, flash;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuP, menuL, menuK, menuD, menuM;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;
        private static readonly HpBarIndicator Indicator = new HpBarIndicator();
        private static SpellSlot Flashslot;
        private static Items.Item Ward;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "LeeSin")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 1200f);
            Q.SetSkillshot(0.25f, 60f, 1800f, true, SpellType.Line);
            Q2 = new Spell(SpellSlot.Q, 1250f);
            W = new Spell(SpellSlot.W, 700f);
            E = new Spell(SpellSlot.E, 450f);
            R = new Spell(SpellSlot.R, 375f);

            flash = new Spell(SpellSlot.Summoner1, 400f);
            flash = new Spell(SpellSlot.Summoner2, 400f);
            Flashslot = Me.GetSpellSlot("SummonerFlash");
            Ward = new Items.Item(3340, 600f);

            Config = new Menu("LeeSin", "[ToxicAio Reborn]: LeeSin", true);

            menuQ = new Menu("Qsettings", "Q settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo", true));
            menuQ.Add(new MenuBool("useQ2", "Use Q2 in Combo", true));
            menuQ.Add(new MenuKeyBind("useQTurret", "Use Q2 under Turret", Keys.T, KeyBindType.Toggle)).AddPermashow();
            Config.Add(menuQ);
            
            menuW = new Menu("Wsettings", "W settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo", true));
            Config.Add(menuW);
            
            menuE = new Menu("Esettings", "E settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo", true));
            Config.Add(menuE);
            
            menuR = new Menu("Rsettings", "R settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo", true));
            menuR.Add(new MenuBool("try", "Try to use Q-R-Q2", true));
            menuR.Add(new MenuSeparator("insecc", "Insec settings"));
            menuR.Add(new MenuKeyBind("Ins", "Insec", Keys.G, KeyBindType.Press)).AddPermashow();
            menuR.Add(new MenuBool("Gapp", "Gapclose with Q if target is not in Insec Range", true));
            Config.Add(menuR);

            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal Settings", true);
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal", true));
            menuK.Add(new MenuBool("KsQ2", "use Q2 to Killsteal", true));
            menuK.Add(new MenuBool("KsE", "use E to Killsteal", true));
            menuK.Add(new MenuBool("KsR", "use R to Killsteal", false));
            Config.Add(menuK);
            
            menuM = new Menu("Misc", "Misc Settings");
            menuM.Add(new MenuBool("AG", "AntiGapcloser", true));
            menuM.Add(new MenuBool("Int", "Interrupter", true));
            Config.Add(menuM);
            
            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuSeparator("Lane", "Lane clear"));
            menuL.Add(new MenuBool("LcE", "use E to Lane clear", true));
            menuL.Add(new MenuSeparator("jungler", "Jungle clear"));
            menuL.Add(new MenuBool("JcQ", "use Q to Jungle clear", true));
            menuL.Add(new MenuBool("JcW", "use W to Jungle clear", true));
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
            Drawing.OnEndScene += DrawingOnEnd;
            Game.OnUpdate += OnTickUpdate;
            Interrupter.OnInterrupterSpell += Interrupter_OnInterrupterSpell;
            AntiGapcloser.OnGapcloser += OnGapCloser;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void OnTickUpdate(EventArgs args)
        {
            if (Config["Rsettings"].GetValue<MenuKeyBind>("Ins").Active)
            {
                Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
                var gapclose = Config["Rsettings"].GetValue<MenuBool>("Gapp").Enabled;
                if (target == null) return;

                if (!target.InRange(Me.GetRealAutoAttackRange() + 70))
                {
                    if (gapclose)
                    {
                        LogicQ();
                    }
                }

                Insec();
                
                if (!Flashslot.IsReady())
                {
                    LogicQ();
                    LogicE(); 
                }
            }
            
            Killsteal();
        }
        
        private static void OnGameUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicQRQ2();
                LogicQ();
                LogicE();
                LogicW();
                LogicR();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Jungle();
                Laneclear();
            }
;
        }

        private static void LogicQ()
        {
            var qtarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");
            var useQ2 = Config["Qsettings"].GetValue<MenuBool>("useQ2");
            var turret = Config["Qsettings"].GetValue<MenuKeyBind>("useQTurret").Active;
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
                if (Q.IsReady() && qtarget.IsValidTarget(Q.Range) && useQ.Enabled && Q.Name == "BlindMonkQOne")
                {
                    var qpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(Q, qtarget);
                    if (qpred.Hitchance >= hitchance)
                    {
                        Q.Cast(qpred.UnitPosition);
                    }
                }

                if (!turret && qtarget.IsUnderEnemyTurret())
                {
                    return;
                }
                
                if (Q.IsReady() && qtarget.IsValidTarget(Q2.Range) && useQ2.Enabled && Q.Name == "BlindMonkQTwo")
                {
                    Q.Cast();
                }
            }
        }

        private static void LogicW()
        {
            var wtarget = W.GetTarget(Me.GetRealAutoAttackRange());
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");
            if (wtarget == null) return;

            if (wtarget.InRange(Me.GetRealAutoAttackRange()))
            {
                if (useW.Enabled && W.IsReady() && wtarget.IsValidTarget(Me.GetRealAutoAttackRange()))
                {
                    W.Cast(Me);
                }
            }
        }

        private static void LogicE()
        {
            var erange = Config["Esettings"].GetValue<MenuSlider>("E2range");
            var etarget = TargetSelector.GetTarget(E.Range, DamageType.Physical);
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            if (etarget == null) return;

            if (etarget.InRange(E.Range))
            {
                if (E.IsReady() && useE.Enabled && etarget.IsValidTarget(E.Range))
                {
                    E.Cast();
                }
            }
        }

        private static void LogicR()
        {
            var rtarget = TargetSelector.GetTarget(R.Range, DamageType.Physical);
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            if (rtarget == null) return;

            if (rtarget.InRange(R.Range))
            {
                if (R.IsReady() && useR.Enabled && rtarget.IsValidTarget(R.Range))
                {
                    if (rtarget.Health <= R.GetDamage(rtarget))
                    {
                        R.Cast(rtarget);
                    }
                }
            }
        }

        private static void LogicQRQ2()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
            var trying = Config["Rsettings"].GetValue<MenuBool>("try");
            if (target == null) return;

            switch (comb(menuP, "QPred"))
            {
                case 0:
                    hitchance = HitChance.Low;
                    break;
                case 1:
                    hitchance = HitChance.Medium;
                    break;
                case 2:
                    hitchance = HitChance.High;
                    break;
                case 3:
                    hitchance = HitChance.VeryHigh;
                    break;
                default:
                    hitchance = HitChance.High;
                    break;
            }

            if (target.InRange(Q.Range))
            {
                if (Q.IsReady() && R.IsReady() && target.IsValidTarget() && Q.Name == "BlindMonkQOne" && trying.Enabled)
                {
                    var Qpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(Q, target);
                    if (Qpred.Hitchance >= hitchance)
                    {
                        if (Q.GetDamage(target, 0) + R.GetDamage(target) + Q.GetDamage(target, 1) >= target.Health)
                        {
                            Q.Cast(Qpred.UnitPosition);
                        }
                    }
                }
                
                if (Q.IsReady() && R.IsReady() && target.IsValidTarget() && Q.Name == "BlindMonkQTwo" && trying.Enabled)
                {
                    if (Q.GetDamage(target, 0) + R.GetDamage(target) + Q.GetDamage(target, 1) >= target.Health)
                    {
                        R.Cast(target);
                    }
                }
                
                if (Q.IsReady() && !R.IsReady() && target.IsValidTarget() && Q.Name == "BlindMonkQTwo" && trying.Enabled)
                {
                    if (Q.GetDamage(target, 0) + R.GetDamage(target) + Q.GetDamage(target, 1) >= target.Health)
                    {
                        if (!target.InRange(Me.GetRealAutoAttackRange()))
                        {
                            DelayAction.Add(350, () =>
                            {
                                Q2.Cast();
                            });
                        }
                    }
                }
            }

        }

        private static void Jungle()
        {
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var JcWw = Config["Clear"].GetValue<MenuBool>("JcW");
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
                if (JcWw.Enabled && W.IsReady() && Me.Distance(mob.Position) < Me.GetRealAutoAttackRange()) W.Cast(Me);
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range) E.Cast();
            }
        }
        
        private static void Laneclear()
        {
            var lce = Config["Clear"].GetValue<MenuBool>("LcE");

            if (lce.Enabled && E.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                
                if (minions.Any())
                {
                    var eFarmLoaction = E.GetCircularFarmLocation(minions);
                    if (eFarmLoaction.Position.IsValid())
                    {
                        E.Cast();
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
                if (ksQ && Q.IsReady() && target.IsValidTarget(Q.Range) && Q.Name == "BlindMonkQOne")
                {
                    if (target != null)
                    {
                        if (target.DistanceToPlayer() <= Q.Range)
                        {
                            if (target.Health + target.AllShield <= Me.GetSpellDamage(target, SpellSlot.Q, 0))
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
                
                if (ksQ && Q.IsReady() && target.IsValidTarget(Q2.Range) && Q.Name == "BlindMonkQTwo")
                {
                    if (target != null)
                    {
                        if (target.DistanceToPlayer() <= Q2.Range)
                        {
                            if (target.Health + target.AllShield <= Me.GetSpellDamage(target, SpellSlot.Q, 1))
                            {
                                Q.Cast();
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
                            if (target.Health + target.AllShield <= Me.GetSpellDamage(target, SpellSlot.E))
                            {
                                E.Cast();
                            }
                        }
                    }
                }
                
                if (ksR && R.IsReady() && target.IsValidTarget(R.Range))
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
            if (Q.IsReady() && Q.Name == "BlindMonkQOne")
            {
                Damage += Q.GetDamage(target);
            }
            
            if (Q.IsReady() && Q.Name == "BlindMonkQTwo")
            {
                Damage += Q.GetDamage(target, 1);
            }
            
            if (W.IsReady())
            {
                Damage += W.GetDamage(target);
            }
            
            if (R.IsReady())
            {
                Damage += R.GetDamage(target);
            }

            return (float)Damage;
        }
        
        private static void OnGapCloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("AG").Enabled)
            {
                if (OktwCommon.CheckGapcloser(sender, args))
                {
                    if (Me.Distance(sender.PreviousPosition) < R.Range)
                    {
                        R.Cast(sender);
                    }
                }
            }
        }
        
        private static void Interrupter_OnInterrupterSpell(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("Int").Enabled)
            {
                if (Me.Distance(sender.ServerPosition) < R.Range)
                {
                    R.Cast(sender);
                }
            }
        }

        private static void Insec()
        {
            var target = TargetSelector.GetTarget(400f, DamageType.Physical);
            if (target == null) return;

            if (target.InRange(Me.GetRealAutoAttackRange() + 70) && Flashslot.IsReady() && !Ward.IsReady && !target.HasBuff("BlindMonkQOne"))
            {
                var insectarget = TargetSelector.GetTarget(400, DamageType.Physical);
                if (insectarget == null) return;

                Me.IssueOrder(GameObjectOrder.MoveTo, insectarget.Position.Extend(Me.Position, 70));
                DelayAction.Add(100, () =>
                {
                    R.Cast(target);
                    Me.Spellbook.CastSpell(Flashslot, target.Position.Extend(Me.Position, -100));
                });
            }
        }
    }
}