using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using System;
using System.Linq;
using SebbyLib;
using SharpDX;
using ToxicAio.Utils;
using HitChance = SebbyLibPorted.Prediction.HitChance;

namespace ToxicAio.Champions
{
    public class Jhin
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuP, menuL, menuK, menuD;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;
        private static readonly HpBarIndicator Indicator = new HpBarIndicator();

        
        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Jhin")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 550f);
            W = new Spell(SpellSlot.W, 2520f);
            E = new Spell(SpellSlot.E, 750f);
            R = new Spell(SpellSlot.R, 3500f);

            Q.SetTargetted(0.25f, 1800f);
            W.SetSkillshot(0.75f, 45f, float.MaxValue, false, SpellType.Line);
            E.SetSkillshot(0.25f, 160f, float.MaxValue, false, SpellType.Circle);
            R.SetSkillshot(0.25f, 80f, 5000f, false, SpellType.Line);

            Config = new Menu("Jhin", "[ToxicAio Reborn]: Jhin", true);

            menuQ = new Menu("Qsettings", " Q Settings");
            menuQ.Add(new MenuBool("useQ", "use Q in Combo", true));
            Config.Add(menuQ);
            
            menuW = new Menu("Wsettings", " W Settings");
            menuW.Add(new MenuBool("useW", "use W in Combo", true));
            menuW.Add(new MenuBool("Wstun", "Only use W when target is marked", true));
            menuW.Add(new MenuBool("Waa", "Only use W When target is not in AA Range", true));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "use E in Combo", true));

            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "use R in Combo", true));
            menuR.Add(new MenuSlider("Cusor", "Cusor Range", 400, 0, 2000));
            menuR.Add(new MenuKeyBind("semiR", "Semi R Key", Keys.T, KeyBindType.Press));
            Config.Add(menuR);

            menuK = new Menu("Killsteal", "Killsteal Settings", true);
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal", true));
            menuK.Add(new MenuBool("KsW", "use W to Killsteal", true));
            Config.Add(menuK);
            
            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("WPred", "W Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));
            menuP.Add(new MenuList("EPred", "E Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));
            menuP.Add(new MenuList("RPred", "R Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));
            Config.Add(menuP);

            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuSeparator("Lane", "Lane clear"));
            menuL.Add(new MenuBool("LcQ", "use Q to Lane clear"));
            menuL.Add(new MenuSeparator("jungler", "Jungle clear"));
            menuL.Add(new MenuBool("JcQ", "use Q to Jungle clear"));
            menuL.Add(new MenuBool("JcW", "use W to Jungle clear"));
            menuL.Add(new MenuBool("JcE", "use E to Jungle clear"));
            Config.Add(menuL);

            menuD = new Menu("Draw", "Draw settings");
            menuD.Add(new MenuBool("drawQ", "Q Range  (White)", true));
            menuD.Add(new MenuBool("drawW", "W Range  (Blue)", true));
            menuD.Add(new MenuBool("drawE", "E Range (Green)", true));
            menuD.Add(new MenuBool("drawR", "R Range  (Red)", true));
            menuD.Add(new MenuBool("drawIn", "Draw Damage Indicator", true));
            menuD.Add(new MenuBool("drawCusor", "R Cusor Range  (Red)", true));
            Config.Add(menuD);

            Config.Attach();
            GameEvent.OnGameTick += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            Orbwalker.OnAfterAttack += Orbwalker_OnAfterAttack;
            Drawing.OnEndScene += DrawingOnEnd;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void OnGameUpdate(EventArgs args)
        {

            if (R.Name.Equals("JhinRShot"))
            {
                Orbwalker.AttackEnabled = false;
                Orbwalker.MoveEnabled = false;
            }
            else if (R.Name.Equals("JhinR"))
            {
                Orbwalker.AttackEnabled = true;
                Orbwalker.MoveEnabled = true;
            }
            
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicW();
                LogicE();
                LogicR();
            }
            
            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                LastHit();
                Jungle();
            }
            
            if (Orbwalker.ActiveMode == OrbwalkerMode.LastHit)
            {
                LastHit();
            }
            
            Killsteal();

            if (R.Name.Equals("JhinRShot"))
            {
                switch (comb(menuP, "RPred"))
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

                if (Config["Rsettings"].GetValue<MenuKeyBind>("semiR").Active)
                {
                    var targets = GameObjects.EnemyHeroes.Where(i =>
                        i.Distance(Game.CursorPos) <= Config["Rsettings"].GetValue<MenuSlider>("Cusor").Value &&
                        !i.IsDead).OrderBy(i => i.Health);

                    if (targets != null)
                    {
                        var target = targets.Find(i =>
                            i.DistanceToCursor() <= Config["Rsettings"].GetValue<MenuSlider>("Cusor").Value);

                        if (target != null)
                        {
                            var rpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(R, target);
                            if (rpred.Hitchance >= hitchance)
                            {
                                R.Cast(rpred.UnitPosition);
                            }
                        }
                    }

                }
            }
        }

        private static void Orbwalker_OnAfterAttack(object sender, AfterAttackEventArgs args)
        {
            LogicQAfter(args.Target);
        }

        private static void LogicQAfter(AttackableUnit target)
        {
            var qtarget = target as AIBaseClient;
            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");
            if (qtarget == null) return;

            if (qtarget.InRange(Q.Range))
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                {
                    if (qtarget.IsValidTarget(Q.Range) && useQ.Enabled && Q.IsReady())
                    {
                        Q.Cast(qtarget);
                    }
                }
            }
        }

        private static void LogicW()
        {
            var wtarget = TargetSelector.GetTarget(W.Range, DamageType.Physical);
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");
            var stun = Config["Wsettings"].GetValue<MenuBool>("Wstun");
            var aa = Config["Wsettings"].GetValue<MenuBool>("Waa");
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
                if (W.IsReady() && useW.Enabled && wtarget.IsValidTarget() && !R.Name.Equals("JhinRShot"))
                {
                    if (aa.Enabled && Me.CountEnemyHeroesInRange(Me.GetRealAutoAttackRange()) == 0)
                    {
                        if (stun.Enabled && wtarget.HasBuff("jhinespotteddebuff"))
                        {
                            var wpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(W, wtarget);
                            if (wpred.Hitchance >= hitchance)
                            {
                                W.Cast(wpred.UnitPosition);
                            }
                        }
                    }
                }
                
                if (W.IsReady() && useW.Enabled && wtarget.IsValidTarget())
                {
                    if (!aa.Enabled)
                    {
                        if (stun.Enabled && wtarget.HasBuff("jhinespotteddebuff") && !R.Name.Equals("JhinRShot"))
                        {
                            var wpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(W, wtarget);
                            if (wpred.Hitchance >= hitchance)
                            {
                                W.Cast(wpred.UnitPosition);
                            }
                        }
                    }
                }
                
                if (W.IsReady() && useW.Enabled && wtarget.IsValidTarget())
                {
                    if (!aa.Enabled)
                    {
                        if (!stun.Enabled && !R.Name.Equals("JhinRShot"))
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

            if (useE.Enabled && E.IsReady() && etarget.IsValidTarget(E.Range) && !R.Name.Equals("JhinRShot") && !Me.IsWindingUp)
            {
                var epred = SebbyLibPorted.Prediction.Prediction.GetPrediction(E, etarget);
                if (epred.Hitchance >= hitchance)
                {
                    if (etarget.HasBuffOfType(BuffType.Stun) || etarget.HasBuffOfType(BuffType.Snare))
                    {
                        E.Cast(epred.UnitPosition);
                    }
                }
            }

            
        }

        private static void LogicR()
        {
            if (R.Name.Equals("JhinRShot"))
            {
                Orbwalker.MoveEnabled = false;
                Orbwalker.AttackEnabled = false;
                switch (comb(menuP, "RPred"))
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

                if (R.IsReady())
                {
                    var targets = GameObjects.EnemyHeroes.Where(i =>
                        i.Distance(Game.CursorPos) <= Config["Rsettings"].GetValue<MenuSlider>("Cusor").Value &&
                        !i.IsDead).OrderBy(i => i.Health);

                    if (targets != null)
                    {
                        var target = targets.Find(i =>
                            i.DistanceToCursor() <= Config["Rsettings"].GetValue<MenuSlider>("Cusor").Value);

                        if (target != null)
                        {
                            var rpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(R, target);
                            if (rpred.Hitchance >= hitchance)
                            {
                                R.Cast(rpred.UnitPosition);
                            }
                        }
                    }
                }
            }

            if (R.Name.Equals("JhinR"))
            {
                Orbwalker.MoveEnabled = true;
                Orbwalker.AttackEnabled = true;
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
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob);
                if (JcWw.Enabled && W.IsReady() && Me.Distance(mob.Position) < W.Range) W.Cast(mob.Position);
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range) E.Cast(mob.Position);
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
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;

            var enemies = GameObjects.EnemyHeroes.Where(x => x != null && x.IsVisibleOnScreen && x.IsValidTarget() && !x.HasBuff("UndyingRage") && !x.IsInvulnerable && !x.HasBuffOfType(BuffType.UnKillable) && !x.IsDead && !x.HasBuffOfType(BuffType.Invulnerability));
            foreach (AIHeroClient enemyHero in enemies)
            {
                //Q
                if (Q.IsReady() && ksQ && enemyHero.DistanceToPlayer() <= Q.Range &&
                    QDamage(enemyHero) >= enemyHero.Health)
                {
                    Q.Cast(enemyHero);
                }

                if (W.IsReady() && ksW && enemyHero.DistanceToPlayer() <= W.Range &&
                    WDamage(enemyHero) >= enemyHero.Health)
                {
                    var wpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(W, enemyHero);
                    if (wpred.Hitchance >= HitChance.High)
                    {
                        W.Cast(wpred.UnitPosition);
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
            
            if (Config["Draw"].GetValue<MenuBool>("drawCusor").Enabled)
            {
                Drawing.DrawCircleIndicator(Game.CursorPos, Config["Rsettings"].GetValue<MenuSlider>("Cusor").Value, System.Drawing.Color.Red);
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
                Damage += QDamage(target);
            }

            if (W.IsReady())
            {
                Damage += WDamage(target);
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

        private static readonly float[] QBaseDamage = {0f, 45f, 70f, 95f, 120f, 145f, 145f};
        private static readonly float[] QMultiplier = {0f, .35f, .425f, .5f, .575f, .65f, .65f};
        private static readonly float[] WBaseDamage = {0f, 60f, 95f, 130f, 165f, 200f, 200f};

        public static float QDamage(AIBaseClient target)
        {
            var qLevel = Q.Level;
            var qBaseDamage = QBaseDamage[qLevel] + QMultiplier[qLevel] * Me.TotalAttackDamage +
                              .6f * Me.TotalMagicalDamage;
            return (float) Me.CalculateDamage(target, DamageType.Physical, qBaseDamage);
        }
        
        public static float WDamage(AIBaseClient target)
        {
            var wLevel = W.Level;
            var wBaseDamage = WBaseDamage[wLevel] + .5f * Me.TotalAttackDamage;
            return (float) Me.CalculateDamage(target, DamageType.Physical, wBaseDamage);
        }
    }
}