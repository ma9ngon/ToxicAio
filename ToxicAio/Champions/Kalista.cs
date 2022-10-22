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
    public class Kalista
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuP, menuL, menuK, menuD;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;
        private static readonly HpBarIndicator Indicator = new HpBarIndicator();
        private static AIHeroClient SweetHeart = null;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Kalista")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 1200f);
            Q.SetSkillshot(0.25f, 40f, 2400, true, SpellType.Line);
            W = new Spell(SpellSlot.W, 5000f);
            E = new Spell(SpellSlot.E, 1000f);
            R = new Spell(SpellSlot.R, 1200f);

            Config = new Menu("Kalista", "[ToxicAio Reborn]: Kalista", true);

            menuQ = new Menu("Qsettings", "Q settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo", true));
            Config.Add(menuQ);
            
            menuW = new Menu("Wsettings", "W settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo", true));
            menuW.Add(new MenuSlider("Wrange", "Max W Range", 2000, 500, 5000));
            Config.Add(menuW);
            
            menuE = new Menu("Esettings", "E settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo", true));
            Config.Add(menuE);
            
            menuR = new Menu("Rsettings", "R settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo to Save ally", true));
            menuR.Add(new MenuSlider("RHP", "Ally HP % To use R", 50, 1, 100));
            Config.Add(menuR);

            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal Settings", true);
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal", true));
            menuK.Add(new MenuBool("KsE", "use E to Killsteal", true));
            Config.Add(menuK);

            menuL = new Menu("Clear", "Clear settings");
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
            Drawing.OnEndScene += DrawingOnEnd;
            Orbwalker.OnAfterAttack += LogicQ;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }
        
        private static void OnGameUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicE();
                LogicW();
                LogicR();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Jungle();
            }
            Killsteal();
        }

        private static void LogicQ(object sender, AfterAttackEventArgs Args)
        {
            var qtarget = (AIHeroClient)Args.Target;
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
            var wtarget = TargetSelector.GetTarget(W.Range, DamageType.Physical);
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");
            var wrange = Config["Wsettings"].GetValue<MenuSlider>("Wrange");
            if (wtarget == null) return;

            if (W.IsReady() && useW.Enabled && wtarget.IsValidTarget(wrange.Value) && wtarget.InRange(wrange.Value))
            {
                if (NavMesh.GetCollisionFlags(wtarget.ServerPosition) == CollisionFlags.Grass)
                {
                    W.Cast(wtarget.ServerPosition);
                }
            }
        }

        private static void LogicE()
        {
            var etarget = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            if (etarget == null) return;

            if (etarget.InRange(E.Range))
            {
                if (E.IsReady() && useE.Enabled && etarget.IsValidTarget(E.Range))
                {
                    if (etarget.Health + etarget.AllShield <= GetEDamage(etarget))
                    {
                        E.Cast();
                    }
                }
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var rhp = Config["Rsettings"].GetValue<MenuSlider>("RHP");
            if (SweetHeart == null)
            {
                SweetHeart = GameObjects.AllyHeroes.Find(h =>
                    h.Buffs.Any(b => b.Caster.IsMe && b.Name.Contains("kalistacoopstrikeally")));
            }
            else if (useR.Enabled && R.IsReady() && SweetHeart.HealthPercent < rhp.Value &&
                     SweetHeart.CountEnemyHeroesInRange(500) > 0)
            {
                R.Cast();
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
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range && mob.Health <= GetEDamage(mob)) E.Cast();
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
                            if (target.Health + target.AllShield <= GetEDamage(target))
                            {
                                E.Cast();
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
                var wrangee = Config["Wsettings"].GetValue<MenuSlider>("Wrange");
                Drawing.DrawCircle(Me.Position, wrangee.Value, 2, System.Drawing.Color.Blue);
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
                Damage += GetEDamage(target);
            }

            if (R.IsReady())
            {
                Damage += R.GetDamage(target, 0);
            }

            return (float)Damage;
        }
        
        
        private static readonly float[] EBaseDamage = {0, 20, 30, 40, 50, 60, 60};
        private static readonly float[] EStackBaseDamage = {0, 10, 14, 19, 25, 32, 32};
        private static readonly float[] EStackMultiplierDamage = {0, .198f, .23748f, .27498f, .31248f, .34988f};
        
        private static float GetEDamage(AIBaseClient target)
        {
            var eLevel = E.Level;
            var eBaseDamage = EBaseDamage[eLevel] + .7 * GameObjects.Player.TotalAttackDamage;
            var eStackDamage = EStackBaseDamage[eLevel] +
                               EStackMultiplierDamage[eLevel] * GameObjects.Player.TotalAttackDamage;
            var eStacksOnTarget = target.GetBuffCount("kalistaexpungemarker");
            if (eStacksOnTarget == 0)
            {
                return 0;
            }

            var total = eBaseDamage + eStackDamage * (eStacksOnTarget - 1);
            if (target is AIMinionClient minion && (minion.GetJungleType() & JungleType.Legendary) != 0)
            {
                total /= 2;
            }

            return (float) GameObjects.Player.CalculateDamage(target, DamageType.Physical, total);
        }

    }
}