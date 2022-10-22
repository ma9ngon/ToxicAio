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
using HitChance = EnsoulSharp.SDK.HitChance;
using OktwCommon = SebbyLib.OktwCommon;
using PredictionInput = SebbyLibPorted.Movement.PredictionInput;
using SkillshotType = SebbyLibPorted.Movement.SkillshotType;

namespace ToxicAio.Champions
{
    public class Viktor
    {
        private static Spell Q, W, E, E2, R;
        private static Menu Config, menuQ, menuE, menuR, menuM, menuP, menuL, menuK, menuD;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;
        private static readonly HpBarIndicator Indicator = new HpBarIndicator();
        private static int tick = 0;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Viktor")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 650f);
            Q.SetTargetted(0.25f, 2000f);
            W = new Spell(SpellSlot.W, 800f);
            W.SetSkillshot(0.25f, 300f, float.MaxValue, false, SpellType.Circle);
            E = new Spell(SpellSlot.E, 550f);
            E.SetSkillshot(0.25f, 60f, 1050f, false, SpellType.Line);
            E2 = new Spell(SpellSlot.E, 500f + 700f);
            E2.SetSkillshot(0.25f, 60f, 1050f, false, SpellType.Line);
            R = new Spell(SpellSlot.R, 700f);
            R.SetSkillshot(0.25f, 300f, float.MaxValue, false, SpellType.Circle);

            Config = new Menu("Viktor", "[ToxicAio Reborn]: Viktor", true);

            menuQ = new Menu("Qsettings", "Q settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo", true));
            Config.Add(menuQ);

            menuE = new Menu("Esettings", "E settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo", true));
            Config.Add(menuE);
            
            menuR = new Menu("Rsettings", "R settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo", true));
            Config.Add(menuR);

            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("EPred", "E Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal Settings", true);
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal", true));
            menuK.Add(new MenuBool("KsE", "use E to Killsteal", true));
            Config.Add(menuK);
            
            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuBool("Int", "Interrupter", true));
            menuM.Add(new MenuBool("AG", "Antigapcloser", true));
            Config.Add(menuM);
            
            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuSeparator("Lane", "Lane clear"));
            menuL.Add(new MenuBool("LcQ", "use Q to Last Hit", true));
            menuL.Add(new MenuBool("LcE", "use E to Lane Clear", true));
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
            Drawing.OnEndScene += DrawingOnEnd;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }
        
        private static void OnGameUpdate(EventArgs args)
        {
            var target = TargetSelector.GetTarget(1200, DamageType.Magical);
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicE(target);
                LogicQ();
                LogicR();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Laneclear();
                LastHit();
                Jungle();
            }
            Killsteal();
            Storm();
        }

        private static void LogicQ()
        {
            var qtarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");
            if (qtarget == null) return;

            if (qtarget.InRange(Q.Range))
            {
                if (Q.IsReady() && qtarget.IsValidTarget(Q.Range) && useQ.Enabled)
                {
                    Q.Cast(qtarget);
                }
            }
        }

        private static void LogicE(AIHeroClient target)
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            var FromPos = Vector3.Zero;
            var ToPos = Vector3.Zero;
            var e2target = TargetSelector.GetTarget(E2.Range, DamageType.Magical);
            
            switch (comb(menuP, "EPred"))
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
            
            if (E.IsReady() && useE.Enabled && e2target.IsValidTarget(E2.Range))
            {
                var E2pred = E2.GetPrediction(e2target, false);
                if (E2pred.Hitchance >= hitchance)
                {
                    ToPos = E2pred.CastPosition;
                }

                FromPos = target.ServerPosition;
            }
            
            if (ToPos == Vector3.Zero)
            {
                ToPos = FromPos.Extend(FromPos, 700f);
            }

            if (FromPos != Vector3.Zero && ToPos != Vector3.Zero)
            {
                if (FromPos.DistanceToPlayer() > E.Range)
                {
                    FromPos = Me.ServerPosition.Extend(FromPos, E.Range);
                }

                if (ToPos.DistanceToPlayer() > E.Range + 100f)
                {
                    ToPos = FromPos.Extend(ToPos, E.Range);
                }

                E.Cast(FromPos, ToPos);
            }
        }

        private static void LogicR()
        {
            var rtarget = TargetSelector.GetTarget(R.Range, DamageType.Magical);
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            if (rtarget == null) return;

            if (R.IsReady() && useR.Enabled && rtarget.IsValidTarget(R.Range) && rtarget.HealthPercent > 5 && rtarget.Health <= R.GetDamage(rtarget))
            {
                R.Cast(rtarget.ServerPosition);
            }
            
            if (R.IsReady() && Q.IsReady() && E.IsReady() && useR.Enabled && rtarget.IsValidTarget(R.Range) && rtarget.HealthPercent > 5 && rtarget.Health <= R.GetDamage(rtarget) + E.GetDamage(rtarget) + Q.GetDamage(rtarget))
            {
                R.Cast(rtarget.ServerPosition);
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
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob);
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range) E.Cast(mob.Position);
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
                    var eFarmLoaction = E.GetLineFarmLocation(minions);
                    if (eFarmLoaction.Position.IsValid())
                    {
                        E.Cast(eFarmLoaction.Position);
                        return;
                    }
                }
            }
        }
        
        private static void LastHit()
        {
            if (Config["Clear"].GetValue<MenuBool>("LcQ").Enabled)
            {
                var allMinions = GameObjects.EnemyMinions.Where(x => x.IsMinion() && !x.IsDead)
                    .OrderBy(x => x.Distance(ObjectManager.Player.Position));

                foreach (var min in allMinions.Where(x => x.IsValidTarget(Q.Range) && x.Health < Q.GetDamage(x)))
                {
                    Orbwalker.ForceTarget = min;
                    Q.Cast(min);
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
                            if (target.Health + target.AllShield <= OktwCommon.GetKsDamage(target, Q) + Me.GetAutoAttackDamage(target, true))
                            {
                                Q.Cast(target);
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
                if (Me.Distance(sender.ServerPosition) < W.Range)
                {
                    W.Cast(sender);
                }
            }
        }
        
        private static void OnGapCloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("AG").Enabled)
            {
                if (OktwCommon.CheckGapcloser(sender, args))
                {
                    if (Me.Distance(args.EndPosition) <= 170)
                        W.Cast(Me);
                }
            }
        }

        private static void Storm()
        {
            var target = TargetSelector.GetTarget(2000, DamageType.Magical);
            if (target == null) return;

            if (Me.HasBuff("ViktorChaosStormTimer"))
            {
                R.CastOnUnit(target);
            }
        }
    }
}