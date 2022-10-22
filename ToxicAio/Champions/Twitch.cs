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
using System.Windows.Controls;
using EnsoulSharp.SDK.Utility;
using SebbyLib;
using SebbyLibPorted.Prediction;
using SharpDX.DXGI;
using ToxicAio.Utils;
using HitChance = SebbyLibPorted.Prediction.HitChance;
using Menu = EnsoulSharp.SDK.MenuUI.Menu;
using OktwCommon = SebbyLib.OktwCommon;

namespace ToxicAio.Champions
{
    public class Twitch
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuP, menuL, menuK, menuD, menuM;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;
        private static readonly HpBarIndicator Indicator = new HpBarIndicator();
        private static Font thm;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Twitch")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 0f);
            W = new Spell(SpellSlot.W, 950f);
            W.SetSkillshot(0.25f, 80f, 1400f, false, SpellType.Circle);
            E = new Spell(SpellSlot.E, 1200f);
            R = new Spell(SpellSlot.R, Me.GetRealAutoAttackRange() + 300f);
            
            thm = new Font(Drawing.Direct3DDevice9, new FontDescription { FaceName = "Tahoma", Height = 22, Weight = FontWeight.Bold, OutputPrecision = FontPrecision.Default, Quality = FontQuality.ClearType });

            Config = new Menu("Twitch", "[ToxicAio Reborn]: Twitch", true);

            menuQ = new Menu("Qsettings", "Q settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo", true));
            Config.Add(menuQ);
            
            menuW = new Menu("Wsettings", "W settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo", true));
            menuW.Add(new MenuBool("Winvs", "Dont use W When invisible or R is Active", true));
            Config.Add(menuW);
            
            menuE = new Menu("Esettings", "E settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo", true));
            menuE.Add(new MenuSlider("Estacks", "E Stacks To Cast E (7 = Disabled)", 6, 1, 7));
            menuE.Add(new MenuBool("KillE", "Only use E When target Killable", true));
            Config.Add(menuE);
            
            menuR = new Menu("Rsettings", "R settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo", true));
            menuR.Add(new MenuSlider("Rtargets", "Min enemys in R Range", 2, 1, 5));
            Config.Add(menuR);

            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("WPred", "W Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal Settings", true);
            menuK.Add(new MenuBool("KsE", "use E to Killsteal", true));
            Config.Add(menuK);
            
            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuKeyBind("Stealth", "Stealth Recall", Keys.B, KeyBindType.Press));
            Config.Add(menuM);

            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuSeparator("Lane", "Lane clear"));
            menuL.Add(new MenuBool("LcW", "use W to Lane clear", true));
            menuL.Add(new MenuSeparator("jungler", "Jungle clear"));
            menuL.Add(new MenuBool("JcW", "use W to Jungle clear", true));
            menuL.Add(new MenuBool("JcE", "use E to Jungle clear", true));
            Config.Add(menuL);
            
            menuD = new Menu("Draw", "Draw settings");
            menuD.Add(new MenuBool("drawQ", "Q Range  (White)", true));
            menuD.Add(new MenuBool("drawW", "W Range  (Blue)", true));
            menuD.Add(new MenuBool("drawE", "E Range (Green)", true));
            menuD.Add(new MenuBool("drawR", "R Range  (Red)", true));
            menuD.Add(new MenuBool("drawB", "Draw Buff Time", true));
            menuD.Add(new MenuBool("drawIn", "Draw Damage Indicator", true));
            Config.Add(menuD);
            
            Config.Attach();
            Game.OnUpdate += Ekills;
            GameEvent.OnGameTick += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            Drawing.OnEndScene += DrawingOnEnd;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void Ekills(EventArgs args)
        {
            LogicE();
            Killsteal();
        }
        
        private static void OnGameUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicW();
                LogicQ();
                LogicR();
            }
            InvisBack();

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Jungle();
                Laneclear();
            }
        }

        private static void LogicQ()
        {
            var qtarget = Q.GetTarget(Me.GetRealAutoAttackRange());
            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");
            if (qtarget == null) return;

            if (qtarget.InRange(Me.GetRealAutoAttackRange()))
            {
                if (Q.IsReady() && qtarget.IsValidTarget(Me.GetRealAutoAttackRange()) && useQ.Enabled)
                {
                    Q.Cast();
                }
            }
        }

        private static void LogicW()
        {
            var wtarget = TargetSelector.GetTarget(W.Range, DamageType.Physical);
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");
            var noinv = Config["Wsettings"].GetValue<MenuBool>("Winvs");
            if (wtarget == null) return;

            switch (comb(menuP, "WPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (noinv.Enabled)
            {
                if (Me.HasBuff("TwitchFullAutomatic") || Me.HasBuff("TwitchHideInShadows"))
                {
                    return;
                }
            }

            if (wtarget.InRange(W.Range))
            {
                if (W.IsReady() && wtarget.IsValidTarget(W.Range) && useW.Enabled)
                {
                    var wpred = SebbyLibPorted.Prediction.Prediction.GetPrediction(W, wtarget);
                    if (wpred.Hitchance >= hitchance)
                    {
                        W.Cast(wpred.UnitPosition);
                    }
                }
            }
        }

        private static void LogicE()
        {
            var etarget = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            var Kill = Config["Esettings"].GetValue<MenuBool>("KillE");
            var stacks = Config["Esettings"].GetValue<MenuSlider>("Estacks");
            if (etarget == null) return;

            if (etarget.InRange(E.Range))
            {
                if (E.IsReady() && useE.Enabled && etarget.IsValidTarget(E.Range) && Kill.Enabled)
                {
                    if (etarget.Health + etarget.AllShield <= GetEDamage(etarget))
                    {
                        E.Cast();
                    }
                }
            }
            
            if (etarget.InRange(E.Range))
            {
                var eStacksOnTarget = etarget.GetBuffCount("TwitchDeadlyVenom");
                if (E.IsReady() && useE.Enabled && etarget.IsValidTarget(E.Range) && eStacksOnTarget >= stacks.Value)
                {
                    E.Cast();
                }
            }
        }

        private static void LogicR()
        {
            var rtarget = TargetSelector.GetTarget(R.Range, DamageType.Physical);
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var minR = Config["Rsettings"].GetValue<MenuSlider>("Rtargets");
            if (rtarget == null) return;

            if (rtarget.InRange(R.Range))
            {
                if (R.IsReady() && useR.Enabled && rtarget.IsValidTarget(R.Range))
                {
                    if (Me.CountEnemyHeroesInRange(R.Range) >= minR.Value)
                    {
                        R.Cast();
                    }
                }
            }
        }

        private static void Jungle()
        {
            var JcWw = Config["Clear"].GetValue<MenuBool>("JcW");
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(W.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcWw.Enabled && W.IsReady() && Me.Distance(mob.Position) < W.Range) W.Cast(mob.Position);
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range && mob.Health <= GetEDamage(mob)) E.Cast();
            }
        }
        
        private static void Laneclear()
        {
            var lcw = Config["Clear"].GetValue<MenuBool>("LcW");

            if (lcw.Enabled && W.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                
                if (minions.Any())
                {
                    var wFarmLoaction = W.GetCircularFarmLocation(minions);
                    if (wFarmLoaction.Position.IsValid())
                    {
                        W.Cast(wFarmLoaction.Position);
                        return;
                    }
                }
            }
        }

        private static void Killsteal()
        {
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;
            foreach (var target in GameObjects.EnemyHeroes.Where(hero => hero.IsValidTarget(E.Range) && !hero.HasBuff("JudicatorIntervention") && !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage")))
            {
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

            if (Config["Draw"].GetValue<MenuBool>("drawB").Enabled)
            {
                Vector2 ft = Drawing.WorldToScreen(Me.Position);
                var buff = Me.GetBuff("TwitchHideInShadows");
                var buff2 = Me.GetBuff("TwitchFullAutomatic");
                if (buff != null)
                {
                    var timer = buff.EndTime - Game.Time;
                    DrawFont(thm, $"Q Time: {timer:N1}" , (float)(ft[0] - 100), (float)(ft[1] + 50), SharpDX.Color.Red);
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
                Damage += GetEDamage(target);
            }

            if (R.IsReady())
            {
                Damage += R.GetDamage(target, 0);
            }

            return (float)Damage;
        }
        
        
        private static readonly float[] EBaseDamage = {0, 20, 30, 40, 50, 60, 60};
        private static readonly float[] EStackBaseDamage = {0, 15, 20, 25, 30, 35, 35};

        private static float GetEDamage(AIBaseClient target)
        {
            var eStacksOnTarget = target.GetBuffCount("TwitchDeadlyVenom");
            var eLevel = E.Level;
            var eBaseDamage = EBaseDamage[eLevel];
            var eStackDamage = EStackBaseDamage[eLevel] + .33 * Me.TotalMagicalDamage + .35 * (Me.TotalAttackDamage - Me.BaseAttackDamage);
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

        private static void InvisBack()
        {
            if (Config["Misc"].GetValue<MenuKeyBind>("Stealth").Active && Q.IsReady())
            {
                Q.Cast();
                Me.Spellbook.CastSpell(SpellSlot.Recall);
            }
        }
        
        private static void DrawFont(Font vFont, string vText, float jx, float jy, ColorBGRA jc)
        {
            vFont.DrawText(null, vText, (int)jx, (int)jy, jc);
        }
    }
}