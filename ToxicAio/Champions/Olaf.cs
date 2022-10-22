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
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp.SDK.Utility;
using ToxicAio.Utils;
using Color = System.Drawing.Color;
using HitChance = SebbyLibPorted.Prediction.HitChance;
using OktwCommon = SebbyLib.OktwCommon;

namespace ToxicAio.Champions
{

    public class Olaf
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuP, menuL, menuK, menuD;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;
        private static Font thm;
        private static int posX = 0;
        private static int posY = 0;
        private static readonly HpBarIndicator Indicator = new HpBarIndicator();

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Olaf")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 1000f);
            W = new Spell(SpellSlot.W, 0f);
            E = new Spell(SpellSlot.E, 325f);
            R = new Spell(SpellSlot.R, 0f);
            
            Q.SetSkillshot(0.25f, 90f, 1600f, false, SpellType.Line);
            E.SetTargetted(0.25f, float.MaxValue);
            thm = new Font(Drawing.Direct3DDevice9, new FontDescription { FaceName = "Tahoma", Height = 22, Weight = FontWeight.Bold, OutputPrecision = FontPrecision.Default, Quality = FontQuality.ClearType });

            Config = new Menu("Olaf", "[ToxicAio Reborn]: Olaf", true);

            menuQ = new Menu("Qsettings", " Q Settings");
            menuQ.Add(new MenuBool("useQ", "use Q in Combo", true));
            Config.Add(menuQ);
            
            menuW = new Menu("Wsettings", " W Settings");
            menuW.Add(new MenuBool("useW", "use W in Combo", true));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "use E in Combo", true));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Auto R on CC", true));
            Config.Add(menuR);

            menuK = new Menu("Killsteal", "Killsteal Settings", true);
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal", true));
            menuK.Add(new MenuBool("KsE", "use E to Killsteal", true));
            Config.Add(menuK);

            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuBool("LcQ", "use Q to Lane clear"));
            menuL.Add(new MenuBool("LhE", "use E to Last Hit"));
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
            menuD.Add(new MenuBool("drawA", "Draw Axe", true));
            menuD.Add(new MenuBool("drawB", "Draw Buff Time", true));
            Config.Add(menuD);

            Config.Attach();
            GameEvent.OnGameTick += OnGameUpdate;
            Game.OnUpdate += OlafR;
            Drawing.OnDraw += OnDraw;
            Orbwalker.OnAfterAttack += OnAfterAttack;
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
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Jungle();
                Laneclear();
                LastHit();
            }
            Killsteal();
        }

        private static void OlafR(EventArgs args)
        {
            LogicR();
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
                if (Q.IsReady() && useQ.Enabled && qtarget.IsValidTarget())
                {
                    var qpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(Q, qtarget);
                    if (qpred.Hitchance >= hitchance)
                    {
                        Q.Cast(qpred.UnitPosition);
                    }
                }
            }
            
        }

        private static void LogicW(AttackableUnit target)
        {
            var wtarget = target as AIBaseClient;
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");
            if (wtarget == null) return;

            if (wtarget.InRange(Me.GetRealAutoAttackRange()))
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                {
                    if (wtarget.IsValidTarget(Me.GetRealAutoAttackRange()) && useW.Enabled && W.IsReady())
                    {
                        W.Cast();
                    }
                }
            }
        }

        private static void LogicE()
        {
            var etarget = TargetSelector.GetTarget(E.Range, DamageType.Physical);
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            if (etarget == null) return;

            if (etarget.InRange(E.Range))
            {
                if (useE.Enabled && etarget.IsValidTarget(E.Range) && E.IsReady())
                {
                    E.Cast(etarget);
                }
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");

            if (R.IsReady() && useR.Enabled)
            {
                if (Me.HasBuffOfType(BuffType.Fear) || Me.HasBuffOfType(BuffType.Stun) ||
                    Me.HasBuffOfType(BuffType.Charm) || Me.HasBuffOfType(BuffType.Snare) ||
                    Me.HasBuffOfType(BuffType.Suppression) || Me.HasBuffOfType(BuffType.Taunt))
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
        
        private static void LastHit()
        {
            if (Config["Clear"].GetValue<MenuBool>("LhE").Enabled)
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
                //Q
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

                //E
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
            
            if (Config["Draw"].GetValue<MenuBool>("drawA").Enabled)
            {
                foreach (var axe in ObjectManager.Get<AIBaseClient>())
                {
                    if (axe.SkinName.ToLowerInvariant().Contains("olafaxe"))
                    {
                        Drawing.DrawCircleIndicator(axe.Position, 200, System.Drawing.Color.Red);
                    }
                }
            }

            if (Config["Draw"].GetValue<MenuBool>("drawB").Enabled)
            {
                Vector2 ft = Drawing.WorldToScreen(Me.Position);
                var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);
                var buff = Me.GetBuff("OlafFrenziedStrikes");
                var buff2 = Me.GetBuff("OlafRagnarok");
                if (buff != null)
                {
                    var timer = buff.EndTime - Game.Time;
                    DrawFont(thm, $"W Time: {timer:N1}" , (float)(ft[0] - 100), (float)(ft[1] + 50), SharpDX.Color.Red);
                }
                
                if (buff2 != null)
                {
                    var timer = buff2.EndTime - Game.Time;
                    DrawFont(thm, $"R Time: {timer:N1}" , (float)(ft[0] - 100), (float)(ft[1] + 75), SharpDX.Color.Red);
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

        private static void OnAfterAttack(object sender, AfterAttackEventArgs args)
        {
            LogicW(args.Target);
        }

        private static void DrawFont(Font vFont, string vText, float jx, float jy, ColorBGRA jc)
        {
            vFont.DrawText(null, vText, (int)jx, (int)jy, jc);
        }
    }
}