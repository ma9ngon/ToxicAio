using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using System;
using System.Linq;
using EnsoulSharp.SDK.Rendering;
using SebbyLib;
using SharpDX;
using ToxicAio.Utils;
using HitChance = SebbyLibPorted.Prediction.HitChance;

namespace ToxicAio.Champions
{
    public class Nilah
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuP, menuL, menuK, menuD;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;
        private static readonly HpBarIndicator Indicator = new HpBarIndicator();
        
        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Nilah")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 600f);
            W = new Spell(SpellSlot.W, 0f);
            E = new Spell(SpellSlot.E, 550f);
            R = new Spell(SpellSlot.R, 400f);

            Q.SetSkillshot(0.25f, 75f, float.MaxValue, false, SpellType.Line);
            E.SetTargetted(0.25f, 2200f);

            Config = new Menu("Nilah", "[ToxicAio Reborn]: Nilah", true);

            menuQ = new Menu("Qsettings", " Q Settings");
            menuQ.Add(new MenuBool("useQ", "use Q in Combo", true));
            Config.Add(menuQ);
            
            menuW = new Menu("Wsettings", " W Settings");
            menuW.Add(new MenuBool("useW", "use W in Combo", true));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "use E in Combo", true));
            menuE.Add(new MenuKeyBind("Egap", "use E To Gapclose on Minions", Keys.T, KeyBindType.Toggle)).AddPermashow();
            menuE.Add(new MenuSlider("ETHP", "Target % HP To Gap Close with E", 50, 1, 100));
            menuE.Add(new MenuSlider("EPHP", "Your HP % To Gap Close with E", 50, 1, 100));
            menuE.Add(new MenuSlider("Etarget", "Max Targets to Gap Close", 2, 1, 5));
            menuE.Add(new MenuSlider("Escan", "E Target Scan Range", 500, 200, 1150));
            
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "use R in Combo", true));
            menuR.Add(new MenuSlider("RHP", "HP % to Use R", 50, 1, 100));
            Config.Add(menuR);

            menuK = new Menu("Killsteal", "Killsteal Settings", true);
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal", true));
            menuK.Add(new MenuBool("KsE", "use E to Killsteal", true));
            menuK.Add(new MenuBool("KsR", "use R to Killsteal", true));
            Config.Add(menuK);
            
            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));
            Config.Add(menuP);

            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuBool("LcQ", "use Q to Lane clear"));
            menuL.Add(new MenuBool("JcQ", "use Q to Jungle clear"));
            menuL.Add(new MenuBool("JcE", "use E to Jungle clear"));
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
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void OnGameUpdate(EventArgs args)
        {

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicQ();
                LogicE();
                LogicW();
                LogicR();
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
                if (qtarget.IsValidTarget(Q.Range) && useQ.Enabled && Q.IsReady())
                {
                    var Qpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(Q, qtarget);
                    if (Qpred.Hitchance >= hitchance)
                    {
                        Q.Cast(Qpred.UnitPosition);
                    }
                }
            }
        }

        private static void LogicW()
        {
            var wtarget = W.GetTarget(Me.GetCurrentAutoAttackRange());
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");
            if (wtarget == null) return;

            if (W.IsReady() && useW.Enabled && wtarget.IsValidTarget(Me.GetCurrentAutoAttackRange()) &&
                wtarget.InRange(Me.GetCurrentAutoAttackRange()))
            {
                W.Cast();
            }
        }

        private static void LogicE()
        {
            var etarget = TargetSelector.GetTarget(E.Range + Q.Range, DamageType.Magical);
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            var useEgap = Config["Esettings"].GetValue<MenuKeyBind>("Egap");
            var targethp = Config["Esettings"].GetValue<MenuSlider>("ETHP");
            var yourhp = Config["Esettings"].GetValue<MenuSlider>("EPHP");
            var targets = Config["Esettings"].GetValue<MenuSlider>("Etarget");
            var scan = Config["Esettings"].GetValue<MenuSlider>("Escan");
            var minion = ObjectManager.Get<AIMinionClient>().Where(i => !i.IsDead && i.IsValid() && !i.IsAlly && i.IsValidTarget(E.Range) && Me.Position.Extend(i.Position, E.Range).Distance(etarget) < Me.Distance(etarget)).OrderBy(i => Me.Position.Extend(i.Position, E.Range).Distance(etarget));
            if (etarget == null) return;

            if (useE.Enabled && E.IsReady() && etarget.IsValidTarget(E.Range))
            {
                E.Cast(etarget);
            }

            if (minion != null)
            {
                if (useEgap.Active)
                {
                    foreach (var min in minion)
                    {
                        if (min != null)
                        {
                            if (!UnderTower(Me.Position.Extend(min.Position, E.Range)))
                            {
                                if (Me.HealthPercent >= yourhp.Value && etarget.HealthPercent <= targethp.Value && min.CountEnemyHeroesInRange(scan.Value) <= targets.Value && !etarget.InRange(Me.GetRealAutoAttackRange()))
                                {
                                    E.Cast(min);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void LogicR()
        {
            var rtarget = TargetSelector.GetTarget(R.Range, DamageType.Magical);
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var useRHP = Config["Rsettings"].GetValue<MenuSlider>("RHP");
            if (rtarget == null) return;
            
                if (R.IsReady() && useR.Enabled && rtarget.IsValidTarget(R.Range))
                {
                    if (Me.HealthPercent <= useRHP.Value)
                    {
                        R.Cast();
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

            var enemies = GameObjects.EnemyHeroes.Where(x => x != null && x.IsVisibleOnScreen && x.IsValidTarget() && !x.HasBuff("UndyingRage") && !x.IsInvulnerable && !x.HasBuffOfType(BuffType.UnKillable) && !x.IsDead && !x.HasBuffOfType(BuffType.Invulnerability));
            foreach (AIHeroClient enemyHero in enemies)
            {
                //Q
                if (Q.IsReady() && ksQ && enemyHero.DistanceToPlayer() <= Q.Range &&
                    OktwCommon.GetKsDamage(enemyHero, Q, true) >= enemyHero.Health)
                {
                    var Qpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(Q, enemyHero);
                    if (Qpred.Hitchance >= SebbyLibPorted.Prediction.HitChance.High)
                    {
                        Q.Cast(Qpred.UnitPosition);
                    }
                }

                //W
                if (E.IsReady() && ksE && enemyHero.DistanceToPlayer() <= E.Range &&
                    EDamage(enemyHero) >= enemyHero.Health)
                {
                    E.Cast(enemyHero);
                }
                
                if (R.IsReady() && ksR && enemyHero.DistanceToPlayer() <= R.Range && OktwCommon.GetKsDamage(enemyHero, R, true) >= enemyHero.Health)
                {
                    R.Cast(enemyHero);
                }
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (Config["Draw"].GetValue<MenuBool>("drawQ").Enabled)
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
                Damage += EDamage(target);
            }

            if (R.IsReady())
            {
                Damage += R.GetDamage(target);
            }
            
            return (float)Damage;
        }

        private static bool UnderTower(SharpDX.Vector3 pos)
        {
            return 
                ObjectManager.Get<AITurretClient>()
                    .Any(i => i.IsEnemy && !i.IsDead && (i.Distance(pos) < 750 + ObjectManager.Player.BoundingRadius)) || GameObjects.EnemySpawnPoints.Any(i => i.Position.Distance(pos) < 750 + ObjectManager.Player.BoundingRadius);
        }
        
        private static readonly float[] EBaseDamage = { 0f, 65f, 90f, 115f, 140f, 165f, 165f };

        public static float EDamage(AIBaseClient target)
        {
            var eLevel = E.Level;
            var eBaseDamage = EBaseDamage[eLevel] + .2f * Me.TotalAttackDamage;
            return (float)Me.CalculateDamage(target, DamageType.Physical, eBaseDamage);
        }
    }
}