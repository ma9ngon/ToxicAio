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
    public static class Tristana
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuE, menuL, menuK, menuD, menuM;
        private static AIHeroClient Me = ObjectManager.Player;
        private static readonly HpBarIndicator Indicator = new HpBarIndicator();

        
        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Tristana")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 0f);
            W = new Spell(SpellSlot.W, 900f);
            E = new Spell(SpellSlot.E, Me.GetRealAutoAttackRange());
            R = new Spell(SpellSlot.R, Me.GetRealAutoAttackRange());

            W.SetSkillshot(0.25f, 350f, 1100f, false, SpellType.Circle);
            E.SetTargetted(0.25f, 2400f);
            R.SetTargetted(0.25f, 2000f);

            Config = new Menu("Tristana", "[ToxicAio Reborn]: Tristana", true);

            menuQ = new Menu("Qsettings", " Q Settings");
            menuQ.Add(new MenuBool("useQ", "use Q in Combo", true));
            Config.Add(menuQ);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "use E in Combo", true));
            Config.Add(menuE);

            menuK = new Menu("Killsteal", "Killsteal Settings", true);
            menuK.Add(new MenuBool("KsW", "use W to Killsteal", true));
            menuK.Add(new MenuBool("KsER", "use ER to Killsteal", true));
            menuK.Add(new MenuBool("KsR", "use R to Killsteal", true));
            Config.Add(menuK);
            
            menuM = new Menu("Misc", "Misc Settings");
            menuM.Add(new MenuBool("AG", "AntiGapcloser", true));
            menuM.Add(new MenuBool("Int", "Interrupter", true));
            Config.Add(menuM);

            menuL = new Menu("Clear", "Clear settings");
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
            Orbwalker.OnBeforeAttack += Orbwalker_OnBeforeAttack;
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
            var forcetarget =
                GameObjects.EnemyHeroes.FirstOrDefault(x =>
                    x.IsCharged() && x.IsValidTarget(Me.GetRealAutoAttackRange(x)));

            if (forcetarget != null && Orbwalker.ActiveMode == OrbwalkerMode.Combo &&
                Orbwalker.GetTarget() != forcetarget)
            {
                Orbwalker.ForceTarget = forcetarget;
            }
            
            E.Range = Me.GetCurrentAutoAttackRange();
            R.Range = Me.GetCurrentAutoAttackRange();
            
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Jungle();
            }
            
            Killsteal();
        }
        
        private static void Orbwalker_OnBeforeAttack(object sender, BeforeAttackEventArgs args)
        {
            LogicE(args);
            LogicQ(args);
        }

        private static void LogicQ(BeforeAttackEventArgs  args)
        {
            var qtarget = args.Target as AIHeroClient;
            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");
            if (qtarget == null) return;

            if (qtarget.InRange(Me.GetRealAutoAttackRange()))
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                {
                    if (qtarget.IsValidTarget(Me.GetRealAutoAttackRange()) && useQ.Enabled && Q.IsReady())
                    {
                        Q.Cast();
                    }
                }
            }
        }
        
        private static void LogicE(BeforeAttackEventArgs args)
        {
            var etarget = args.Target as AIHeroClient;
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            if (etarget == null) return;

            if (etarget.InRange(Me.GetRealAutoAttackRange()))
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                {
                    if (etarget.IsValidTarget(Me.GetRealAutoAttackRange()) && useE.Enabled && E.IsReady())
                    {
                        E.Cast(etarget);
                    }
                }
            }
        }

        private static void Jungle()
        {
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range) E.Cast(mob);
            }
        }

        private static void Killsteal()
        {
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;
            var ksER = Config["Killsteal"].GetValue<MenuBool>("KsER").Enabled;
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;
            foreach (var target in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(R.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage")))
            {
                if (ksW && W.IsReady() && target.IsValidTarget(W.Range))
                {
                    if (target != null)
                    {
                        if (target.DistanceToPlayer() <= W.Range)
                        {
                            if (target.Health + target.AllShield <= OktwCommon.GetKsDamage(target, W))
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
                
                if (ksER && R.IsReady() && target.IsValidTarget(R.Range))
                {
                    if (target != null)
                    {
                        if (target.DistanceToPlayer() <= R.Range)
                        {
                            if (target.Health + target.AllShield <= EDamage(target)+RDamage(target))
                            {
                                R.Cast(target);
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
                            if (target.Health + target.AllShield <= OktwCommon.GetKsDamage(target, R))
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
                Damage += RDamage(target);
            }
            
            return (float)Damage;
        }
        
        private static readonly float[] EBaseDamage = {0f, 70f, 80f, 90f, 100f, 110f, 110f};
        private static readonly float[] EMultiplier = {0f, .5f, .75f, 1f, 1.25f, 1.5f, 1.5f};
        private static readonly float[] EStack = {0f, 21f, 24f, 27f, 30f, 33f, 33f};
        private static readonly float[] EStackMultiplier = {0f, .15f, .225f, .30f, .375f, .45f, .45f};
        private static readonly float[] RBaseDamage = {0f, 300f, 400f, 500f, 500f};
        
        public static float EDamage(AIBaseClient target)
        {
            if (!target.IsCharged())
            {
                return 0;
            }

            var eLevel = E.Level;
            var eBaseDamage = EBaseDamage[eLevel] + 
                              EMultiplier[eLevel] * Me.GetBonusPhysicalDamage() +
                              .5f * Me.TotalMagicalDamage;
            var eBonusDamage = EStack[eLevel] + EStackMultiplier[eLevel] + Me.TotalAttackDamage +
                               .15 * Me.TotalMagicalDamage;
            var total = eBaseDamage + eBonusDamage * target.EBuffCount();
            return (float) Me.CalculateDamage(target, DamageType.Physical, total);
        }
        
        public static float RDamage(AIBaseClient target)
        {
            var rLevel = R.Level;
            var rBaseDamage = RBaseDamage[rLevel] + Me.TotalMagicalDamage;
            return (float) Me.CalculateDamage(target, DamageType.Magical, rBaseDamage);
        }
        
        private static bool IsCharged(this AIBaseClient target)
        {
            return target.HasBuff("TristanaECharge");
        }
        
        private static Boolean HasEBuff(AIBaseClient target)
        {
            return target.HasBuff("TristanaECharge");
        }

        private static int EBuffCount(this AIBaseClient target)
        {
            return target.GetBuffCount("TristanaECharge");
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
    }
}