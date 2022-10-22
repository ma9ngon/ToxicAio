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
using ToxicAio.Utils;
using OktwCommon = SebbyLib.OktwCommon;
using HitChance = SebbyLibPorted.Prediction.HitChance;

namespace ToxicAio.Champions
{
    public class Veigar
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuP, menuL, menuK, menuD;
        private static AIHeroClient Me = ObjectManager.Player;
        private static Font thm;
        private static HitChance hitchance;
        private static readonly HpBarIndicator Indicator = new HpBarIndicator();

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Veigar")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 950f);
            W = new Spell(SpellSlot.W, 900f);
            E = new Spell(SpellSlot.E, 725f);
            R = new Spell(SpellSlot.R, 650f);

            Q.SetSkillshot(0.25f, 70f, 2200f, false, SpellType.Line);
            W.SetSkillshot(0.25f, 120f, float.MaxValue, false, SpellType.Circle);
            E.SetSkillshot(0.25f, 400f, float.MaxValue, false, SpellType.Circle);
            R.SetTargetted(0.25f, float.MaxValue);
            thm = new Font(Drawing.Direct3DDevice9,
                new FontDescription
                {
                    FaceName = "Tahoma",
                    Height = 22,
                    Weight = FontWeight.Bold,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearType
                });

            Config = new Menu("Veigar", "[ToxicAio Reborn]: Veigar", true);

            menuQ = new Menu("Qsettings", " Q Settings");
            menuQ.Add(new MenuBool("useQ", "use Q in Combo", true));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("useW", "use W in Combo", true));
            menuW.Add(new MenuList("Wmode", "W Mode",
                new string[] { "CC", "Slow", "Always" }, 0));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "use E in Combo", true));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "use R in Combo", true));
            Config.Add(menuR);

            menuK = new Menu("Killsteal", "Killsteal Settings", true);
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal", true));
            menuK.Add(new MenuBool("KsW", "use W to Killsteal", true));
            Config.Add(menuK);
            
            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));
            menuP.Add(new MenuList("WPred", "W Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));
            menuP.Add(new MenuList("EPred", "E Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));
            Config.Add(menuP);

            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuBool("LcQ", "use Q to Lane clear"));
            menuL.Add(new MenuBool("LcW", "use W to Lane clear"));
            menuL.Add(new MenuBool("JcQ", "use Q to Jungle clear"));
            menuL.Add(new MenuBool("JcW", "use W to Jungle clear"));
            Config.Add(menuL);

            menuD = new Menu("Draw", "Draw settings");
            menuD.Add(new MenuBool("drawQ", "Q Range  (White)", true));
            menuD.Add(new MenuBool("drawW", "W Range  (Blue)", true));
            menuD.Add(new MenuBool("drawE", "E Range (Green)", true));
            menuD.Add(new MenuBool("drawR", "R Range  (Red)", true));
            menuD.Add(new MenuBool("drawIn", "Draw Damage Indicator", true));
            menuD.Add(new MenuBool("drawKill", "R Killable Message", true));
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
                LastHit();
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

            if (qtarget.InRange(Q.Range))
            
                if (qtarget.IsValidTarget(Q.Range) && Q.IsReady() && useQ.Enabled)
                {
                    var Qpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(Q, qtarget);
                    if (Qpred.Hitchance >= hitchance)
                    {
                        var col = Q.GetCollision(Me.Position.ToVector2(),
                            new List<Vector2> { Qpred.UnitPosition.ToVector2() }).Take(2).ToList();
                        if (col.Count <= 1)
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
            if (wtarget == null) return;
            
            switch (comb(menuP, "WPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            switch (comb(menuW, "Wmode"))
            {
                case 0:
                    if (wtarget.InRange(W.Range))
                    {
                        if (useW.Enabled && W.IsReady() && wtarget.IsValidTarget())
                        {
                            if (wtarget.HasBuffOfType(BuffType.Stun) || wtarget.HasBuffOfType(BuffType.Snare) || wtarget.HasBuffOfType(BuffType.Suppression))
                            {
                                var Wpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(W, wtarget);;
                                if (Wpred.Hitchance >= HitChance.Immobile)
                                {
                                    W.Cast(Wpred.UnitPosition);
                                }
                            }
                        }
                    }
                    break;

                case 1:
                    if (wtarget.InRange(W.Range))
                    {
                        if (useW.Enabled && W.IsReady() && wtarget.IsValidTarget())
                        {
                            if (wtarget.HasBuffOfType(BuffType.Slow))
                            {
                                var Wpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(W, wtarget);
                                if (Wpred.Hitchance >= hitchance)
                                {
                                    W.Cast(Wpred.UnitPosition);
                                }
                            }
                        }
                    }
                    break;

                case 2:
                    if (wtarget.InRange(W.Range))
                    {
                        if (useW.Enabled && W.IsReady() && wtarget.IsValidTarget())
                        {
                            if (!wtarget.IsFacing(Me) && !wtarget.HasBuffOfType(BuffType.Stun) ||
                                !wtarget.HasBuffOfType(BuffType.Stun))
                            {
                                var Wpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(W, wtarget);
                                if (Wpred.Hitchance >= hitchance)
                                {
                                    W.Cast(Wpred.UnitPosition.Extend(Me.Position, -125));
                                }
                            }
                            else if (wtarget.HasBuffOfType(BuffType.Stun) || wtarget.HasBuffOfType(BuffType.Snare))
                            {
                                var Wpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(W, wtarget);
                                if (Wpred.Hitchance >= hitchance)
                                {
                                    W.Cast(Wpred.UnitPosition.Extend(Me.Position, -100));
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private static void LogicE()
        {
            var etarget = E.GetTarget(E.Range);
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
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
                if (E.IsReady() && useE.Enabled && etarget.IsValidTarget())
                {
                    var Epred = SebbyLibPorted.Prediction.Prediction.GetPrediction(E, etarget);
                    if (Epred.Hitchance >= hitchance)
                    {
                        E.Cast(Epred.UnitPosition);
                    }
                }
            }
        }

        private static void LogicR()
        {
            var rtarget = TargetSelector.GetTarget(R.Range, DamageType.Magical);
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            if (rtarget == null) return;

            if (R.IsReady() && useR.Enabled && rtarget.IsValidTarget())
            {
                if (RDamage(rtarget) >= rtarget.Health)
                {
                    R.Cast(rtarget);
                }
            }
        }

        private static void Jungle()
        {
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var JcWw = Config["Clear"].GetValue<MenuBool>("JcW");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
                if (JcWw.Enabled && W.IsReady() && Me.Distance(mob.Position) < W.Range) W.Cast(mob.Position);
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
                    Q.Cast(min.Position);
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;
            
            foreach (var target in GameObjects.EnemyHeroes.Where(hero => hero.IsValidTarget(Q.Range) && !hero.HasBuff("JudicatorIntervention") && !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage")))
            {
                if (ksQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                {
                    if (target != null)
                    {
                        if (target.DistanceToPlayer() <= Q.Range)
                        {
                            if (target.Health + target.AllShield <= SebbyLib.OktwCommon.GetKsDamage(target, Q))
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
                
                if (ksW && W.IsReady())
                {
                    if (target != null)
                    {
                        if (target.DistanceToPlayer() <= W.Range)
                        {
                            if (target.Health + target.AllShield <= SebbyLib.OktwCommon.GetKsDamage(target, W))
                            {
                                var wpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(W, target);
                                if (wpred.Hitchance >= HitChance.High)
                                {
                                    W.Cast(wpred.UnitPosition);
                                }
                            }
                        }
                    }
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

            if (Config["Draw"].GetValue<MenuBool>("drawKill").Enabled)
            {
                var target = TargetSelector.GetTarget(R.Range, DamageType.Magical);
                if (target.IsValidTarget(R.Range) && R.IsInRange(target) && R.IsReady())
                {
                    if (RDamage(target) >= target.Health)
                    {
                        Vector2 ft = Drawing.WorldToScreen(Me.Position);
                        DrawFont(thm, "Target: " + target.CharacterName + "is Killable " + "HP Left: " + target.Health, (float)(ft[0] - 20),
                            (float)(ft[1] + 50), SharpDX.Color.Orange);
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
                Damage += RDamage(target);
            }
            
            return (float)Damage;
        }

        private static void DrawFont(Font vFont, string vText, float jx, float jy, ColorBGRA jc)
        {
            vFont.DrawText(null, vText, (int)jx, (int)jy, jc);
        }

        public static double RDamage(AIHeroClient target)
        {
            if (target == null || !target.IsValidTarget())
            {
                return 0;
            }

            var rLevel = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level;
            if (rLevel <= 0)
            {
                return 0;
            }

            var baseDamage = new [] {0, 175, 250, 325}[rLevel];
            var apdamage = new[] {0, 175, 250, 325}[rLevel] + 0.75f * ObjectManager.Player.TotalMagicalDamage + ((100 - target.HealthPercent * 1.5f) / 100);
            var resultDamage =
                ObjectManager.Player.CalculateDamage(target, DamageType.Magical, baseDamage + apdamage);
            if (ObjectManager.Player.HasBuff("SummonerExhaust"))
            {
                resultDamage *= 0.6f;
            }

            return resultDamage - 60;

        }
    }
}
