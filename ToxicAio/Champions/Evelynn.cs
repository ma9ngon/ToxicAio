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
using EnsoulSharp.SDK.Utility;
using ToxicAio.Utils;
using HitChance = SebbyLibPorted.Prediction.HitChance;

namespace ToxicAio.Champions
{
    public class Evelynn
    {
        private static Spell Q, Q2, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuP, menuL, menuK, menuD;
        private static AIHeroClient Me = ObjectManager.Player;
        private static float LastW = 0;
        private static HitChance hitchance;
        private static readonly HpBarIndicator Indicator = new HpBarIndicator();
        
        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Evelynn")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 800f);
            Q2 = new Spell(SpellSlot.Q, 550f);
            W = new Spell(SpellSlot.W, 1200f);
            E = new Spell(SpellSlot.E, 290f);
            R = new Spell(SpellSlot.R, 500f);

            Q.SetSkillshot(0.25f, 60f, 2400f, true, SpellType.Line);
            W.SetTargetted(0.25f, 902f);
            E.SetTargetted(0.25f, 2400f);

            Config = new Menu("Evelynn", "[ToxicAio Reborn]: Evelynn", true);

            menuQ = new Menu("Qsettings", " Q Settings");
            menuQ.Add(new MenuBool("useQ", "use Q in Combo", true));
            Config.Add(menuQ);
            
            menuW = new Menu("Wsettings", " W Settings");
            menuW.Add(new MenuBool("useW", "use W in Combo", true));
            menuW.Add(new MenuSlider("WRange", "Max W Range", 750, 100, 1600));
            menuW.Add(new MenuList("Wmode", "W Mode",
                new string[] { "Slow", "Stun" }, 1));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "use E in Combo", true));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "use R in Combo", true));
            Config.Add(menuR);

            menuK = new Menu("Killsteal", "Killsteal Settings", true);
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal", true));
            menuK.Add(new MenuBool("KsE", "use E to Killsteal", true));
            menuK.Add(new MenuBool("KsR", "use R to Killsteal", true));
            Config.Add(menuK);

            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuBool("LcQ", "use Q to Lane clear"));
            menuL.Add(new MenuBool("LcE", "use E to Lane clear"));
            menuL.Add(new MenuBool("JcQ", "use Q to Jungle clear"));
            menuL.Add(new MenuBool("JcE", "use E to Jungle clear"));
            Config.Add(menuL);
            
            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));
            Config.Add(menuP);

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
            AIBaseClient.OnProcessSpellCast += OnProcessSpellCastt;
            Drawing.OnEndScene += DrawingOnEnd;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void OnGameUpdate(EventArgs args)
        {
            W.Range = rRange;

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                switch (comb(menuW, "Wmode"))
                {
                    case 0:
                        LogicW();
                        LogicR();
                        LogicE();
                        LogicQ();
                        break;
                    
                    case 1:
                        LogicW();
                        if (Variables.GameTimeTickCount - LastW > 2550)
                        {
                            LogicR();
                            LogicE();
                            LogicQ(); 
                        }

                        break;
                }
                
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                LastHit();
                Laneclear();
                Jungle();
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

            if (Q.Name == "EvelynnQ")
            {
                Q.Collision = true;
            }
            else if (Q.Name == "EvelynnQ2")
            {
                Q.Collision = false;
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
            var wtarget = TargetSelector.GetTarget(W.Range, DamageType.Magical);
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");
            var range = Config["Wsettings"].GetValue<MenuSlider>("WRange");
            if (wtarget == null) return;

            if (wtarget.InRange(range.Value))
            {
                if (W.IsReady() && wtarget.IsValidTarget(W.Range) && useW.Enabled)
                {
                    W.Cast(wtarget);
                }
            }
        }

        private static void LogicE()
        {
            var etarget = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            if (etarget == null) return;

            if (useE.Enabled && E.IsReady() && etarget.IsValidTarget(E.Range))
            {
                E.Cast(etarget);
            }
        }

        private static void LogicR()
        {
            var rtarget = TargetSelector.GetTarget(R.Range, DamageType.Magical);
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            if (rtarget == null) return;

            if (!rtarget.HasBuff("UndyingRage") && !rtarget.IsInvulnerable &&
                !rtarget.HasBuffOfType(BuffType.UnKillable) && !rtarget.IsDead &&
                !rtarget.HasBuffOfType(BuffType.Invulnerability))
            {
                if (R.IsReady() && useR.Enabled && rtarget.IsValidTarget(R.Range))
                {
                    if (rtarget.HealthPercent <= 29)
                    {
                        R.Cast(rtarget);
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
                    var qFarmLoaction = Q.GetLineFarmLocation(minions);
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
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;

            var enemies = GameObjects.EnemyHeroes.Where(x => x != null && x.IsVisibleOnScreen && x.IsValidTarget() && !x.HasBuff("UndyingRage") && !x.IsInvulnerable && !x.HasBuffOfType(BuffType.UnKillable) && !x.IsDead && !x.HasBuffOfType(BuffType.Invulnerability));
            foreach (AIHeroClient enemyHero in enemies)
            {
                //Q
                if (Q.IsReady() && ksQ && enemyHero.DistanceToPlayer() <= Q.Range &&
                  OktwCommon.GetKsDamage(enemyHero, Q) >= enemyHero.Health)
                {
                    var Qpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(Q, enemyHero);
                    if (Qpred.Hitchance >= SebbyLibPorted.Prediction.HitChance.High)
                    {
                        Q.Cast(Qpred.UnitPosition);
                    }
                }

                //W
                if (E.IsReady() && ksE && enemyHero.DistanceToPlayer() <= E.Range &&
                    OktwCommon.GetKsDamage(enemyHero, E) >= enemyHero.Health)
                {
                    E.Cast(enemyHero);
                }
                
                if (R.IsReady() && ksR && enemyHero.DistanceToPlayer() <= R.Range &&
                    enemyHero.HealthPercent <= 29)
                {
                    R.Cast(enemyHero);
                }
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (Config["Draw"].GetValue<MenuBool>("drawQ").Enabled )
            {
                if (Q.Name == "EvelynnQ")
                {
                    Drawing.DrawCircle(Me.Position, Q.Range, 2, System.Drawing.Color.White);
                }
                else if (Q.Name == "EvelynnQ2")
                {
                    Drawing.DrawCircle(Me.Position, Q2.Range, 2, System.Drawing.Color.White);
                }
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

            if (E.IsReady())
            {
                Damage += E.GetDamage(target);
            }

            if (R.IsReady())
            {
                Damage += R.GetDamage(target, 0);
            }

            return (float)Damage;
        }

        private static float rRange =>
            new[] { 1200, 1200, 1300, 1400, 1500, 1600 }[Me.Spellbook.GetSpell(SpellSlot.W).Level];
        
        private static readonly float[] EBaseDamage = { 0f, 80f, 120f, 160f, 200f, 240f, 240f };
        
        private static void OnProcessSpellCastt(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.Slot == SpellSlot.W)
            {
                LastW = Variables.GameTimeTickCount;
            }
        }
    }
}