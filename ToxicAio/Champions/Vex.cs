using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using System.Threading.Tasks;
using System.Text;
using SharpDX;
using Color = System.Drawing.Color;
using EnsoulSharp.SDK.MenuUI;
using System.Reflection;
using System.Net.Http;
using System.Net;
using System.Text.RegularExpressions;
using SebbyLib;

namespace ToxicAio.Champions
{
    public class Vex
    {
        private static Spell Q, W, E, R, R2;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuL, menuK, menuM, menuD, menuP;
        private static SpellSlot igniteSlot;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Vex")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 1200f);
            W = new Spell(SpellSlot.W, 475f);
            E = new Spell(SpellSlot.E, 800f);
            R = new Spell(SpellSlot.R, rRange);
            
            Q.SetSkillshot(0.15f, 160, 3200, false, SpellType.Line);
            E.SetSkillshot(0.25f, 300, 1300, false, SpellType.Circle);
            R.SetSkillshot(0.25f, 650, 1600, false, SpellType.Line);

            igniteSlot = Me.GetSpellSlot("SummonerDot");


            Config = new Menu("Vex", "[ToxicAio]: Vex", true);

            menuQ = new Menu("Qsettings", "Q settings");
            menuQ.Add(new MenuBool("UseQ", "Use Q in Combo"));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W settings");
            menuW.Add(new MenuBool("UseW", "use W in Combo"));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E settings");
            menuE.Add(new MenuBool("UseE", "use E in Combo"));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R settings");
            menuR.Add(new MenuBool("UseR", "use R in Combo"));
            menuR.Add(new MenuKeyBind("SemiR", "Semi R", Keys.T, KeyBindType.Press));
            Config.Add(menuR);

            menuP = new Menu("Psettings", "Pred settings");
            menuP.Add(new MenuBool("QPred", "Enable Q Prediction"));
            menuP.Add(new MenuBool("EPred", "Enable E Prediction"));
            menuP.Add(new MenuBool("RPred", "Enable R Prediction"));
            menuP.Add(new MenuList("qPred", "Q Prediction hitchance",
                new string[] {"Low", "Medium", "High", " Very High"}, 2));
            menuP.Add(new MenuList("ePred", "E Prediction hitchance",
                new string[] {"Low", "Medium", "High", " Very High"}, 2));
            menuP.Add(new MenuList("rPred", "R Prediction hitchance",
                new string[] {"Low", "Medium", "High", " Very High"}, 2));
            Config.Add(menuP);

            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuBool("LcQ", "use Q to Laneclear"));
            menuL.Add(new MenuBool("LcW", "use W to Laneclear"));
            menuL.Add(new MenuBool("LcE", "use E to Laneclear"));
            menuL.Add(new MenuBool("JcQ", "use Q to Jungleclear"));
            menuL.Add(new MenuBool("JcW", "use W to Jungleclear"));
            menuL.Add(new MenuBool("JcE", "use E to Jungleclear"));
            Config.Add(menuL);

            menuK = new Menu("Killsteal", "Killsteal settings");
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal"));
            menuK.Add(new MenuBool("KsW", "use W to Killsteal"));
            menuK.Add(new MenuBool("KsE", "use E to Killsteal"));
            menuK.Add(new MenuBool("KsR", "use R to Killsteal"));
            Config.Add(menuK);

            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuSliderButton("Skin", "SkindID", 0, 0, 30, false));
            Config.Add(menuM);

            menuD = new Menu("Draw", "Draw settings");
            menuD.Add(new MenuBool("drawQ", "Q Range  (White)", true));
            menuD.Add(new MenuBool("drawW", "W Range  (White)", true));
            menuD.Add(new MenuBool("drawE", "E Range (White)", true));
            menuD.Add(new MenuBool("drawR", "R Range  (Red)", true));
            menuD.Add(new MenuBool("drawD", "Draw Combo Damage", true));
            Config.Add(menuD);

            Config.Attach();

            GameEvent.OnGameTick += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static float rRange => new[] {2000, 2000, 2500, 3000}[Me.Spellbook.GetSpell(SpellSlot.R).Level];

        public static void OnGameUpdate(EventArgs args)
        {

            if (R.Level > 0)
            {
                R.Range = rRange;
            }

            if (Config["Rsettings"].GetValue<MenuKeyBind>("SemiR").Active)
            {
                semiR();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicR();
                LogicW();
                LogicQ();
                LogicE();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Lanceclear();
                Jungle();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LastHit)
            {
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Harass)
            {

            }
            Killsteal();
            skind();
        }

        private static void skind()
        {
            if (Config["Misc"].GetValue<MenuSliderButton>("Skin").Enabled)
            {
                int skinnu = Config["Misc"].GetValue<MenuSliderButton>("Skin").Value;

                if (Me.SkinId != skinnu)
                    Me.SetSkin(skinnu);
            }
        }

        private static void semiR()
        {
            var target = R.GetTarget(rRange);
            if (target == null) return;

            if (R.IsReady() && target.IsValidTarget(rRange))
            {
                R.Cast(target);
            }
        }
        
        private static float ComboDamage(AIBaseClient enemy)
        {
            var damage = 0d;
            if (igniteSlot != SpellSlot.Unknown &&
                Me.Spellbook.CanUseSpell(igniteSlot) == SpellState.Ready)
                damage += Me.GetSummonerSpellDamage(enemy, SummonerSpell.Ignite);
            if (Q.IsReady())
                damage += Me.GetSpellDamage(enemy, SpellSlot.Q);
            if (W.IsReady())
                damage += Me.GetSpellDamage(enemy, SpellSlot.W);
            if (E.IsReady())
                damage += Me.GetSpellDamage(enemy, SpellSlot.E);
            if (R.IsReady())
                damage += Me.GetSpellDamage(enemy, SpellSlot.R);

            return (float) damage;
        }

        private static void OnDraw(EventArgs args)
        {
            if (Config["Draw"].GetValue<MenuBool>("drawQ").Enabled)
            {
                Render.Circle.DrawCircle(Me.Position, Q.Range, System.Drawing.Color.White);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawW").Enabled)
            {
                Render.Circle.DrawCircle(Me.Position, W.Range, System.Drawing.Color.White);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled)
            {
                Render.Circle.DrawCircle(Me.Position, E.Range, System.Drawing.Color.White);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled)
            {
                Render.Circle.DrawCircle(Me.Position, R.Range, System.Drawing.Color.Red);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawD").Enabled)
            {
                foreach (
                    var enemyVisible in
                    ObjectManager.Get<AIHeroClient>().Where(enemyVisible => enemyVisible.IsValidTarget()))
                {

                    if (ComboDamage(enemyVisible) > enemyVisible.Health)
                    {
                        Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                            Drawing.WorldToScreen(enemyVisible.Position)[1] - 40, Color.Red,
                            "Combo=Kill");
                    }
                    else if (ComboDamage(enemyVisible) +
                        Me.GetAutoAttackDamage(enemyVisible, true) * 2 > enemyVisible.Health)
                    {
                        Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                            Drawing.WorldToScreen(enemyVisible.Position)[1] - 40, Color.Orange,
                            "Combo + 2 AA = Kill");
                    }
                    else
                        Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                            Drawing.WorldToScreen(enemyVisible.Position)[1] - 40, Color.Green,
                            "Unkillable with combo + 2 AA");

                }
            }
        }

        private static void LogicQ()
        {
            var qtarget = Q.GetTarget(Q.Range);
            var useQ = Config["Qsettings"].GetValue<MenuBool>("UseQ");
            var pred = Config["Psettings"].GetValue<MenuBool>("QPred");
            var input = Q.GetPrediction(qtarget);
            if (qtarget == null) return;
            
            switch (comb(menuP, "qPred"))
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

            if (Q.IsReady() && useQ.Enabled && qtarget.IsValidTarget(Q.Range) && Q.IsInRange(qtarget) && pred.Enabled && input.Hitchance >= hitchance)
            {
                Q.Cast(input.CastPosition);
            }
            else if (Q.IsReady() && useQ.Enabled && qtarget.IsValidTarget(Q.Range) && Q.IsInRange(qtarget) && !pred.Enabled)
            {
                Q.Cast(qtarget);
            }
        }

        private static void LogicW()
        {
            var wtarget = W.GetTarget(W.Range);
            var useW = Config["Wsettings"].GetValue<MenuBool>("UseW");
            if (wtarget == null) return;

            if (W.IsReady() && useW.Enabled && wtarget.IsValidTarget(W.Range))
            {
                W.Cast();
            }
        }

        private static void LogicE()
        {
            var etarget = E.GetTarget(E.Range);
            var useE = Config["Esettings"].GetValue<MenuBool>("UseE");
            var input = E.GetPrediction(etarget);
            var pred = Config["Psettings"].GetValue<MenuBool>("EPred");
            if (etarget == null) return;
            
            switch (comb(menuP, "ePred"))
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

            if (E.IsReady() && useE.Enabled && etarget.IsValidTarget(E.Range) && pred.Enabled && input.Hitchance >= hitchance)
            {
                E.Cast(input.CastPosition);
            }
            else if (E.IsReady() && useE.Enabled && etarget.IsValidTarget(E.Range) && !pred.Enabled)
            {
                E.Cast(etarget);
            }
        }

        private static void LogicR()
        {
            var rtarget = R.GetTarget();
            var useR = Config["Rsettings"].GetValue<MenuBool>("UseR");
            var input = R.GetPrediction(rtarget);
            var pred = Config["Psettings"].GetValue<MenuBool>("RPred");
            
            switch (comb(menuP, "rPred"))
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

            if (R.IsReady() && useR.Enabled && rtarget.IsValidTarget() && input.Hitchance >= hitchance && pred.Enabled && Q.GetDamage(rtarget) + W.GetDamage(rtarget) + E.GetDamage(rtarget) + R.GetDamage(rtarget) >= rtarget.Health && Q.IsReady() && W.IsReady())
            {
                R.Cast(input.CastPosition);
            }

            if (R.IsReady() && useR.Enabled && rtarget.IsValidTarget() && !pred.Enabled &&
                R.GetDamage(rtarget) >= rtarget.Health)
            {
                R.Cast(rtarget);
            }
            else if (R.IsReady() && useR.Enabled && rtarget.IsValidTarget() && pred.Enabled &&
                     R.GetDamage(rtarget) >= rtarget.Health && input.Hitchance >= hitchance)
            {
                R.Cast(input.CastPosition);
            }
            else if (R.IsReady() && useR.Enabled && rtarget.IsValidTarget() && !pred.Enabled && Q.GetDamage(rtarget) + W.GetDamage(rtarget) + E.GetDamage(rtarget) + R.GetDamage(rtarget) >= rtarget.Health && Q.IsReady() && W.IsReady())
            {
                R.Cast(rtarget);
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
                if (JcWw.Enabled && W.IsReady() && Me.Distance(mob.Position) < W.Range) W.Cast();
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range) E.Cast(mob.Position);
            }
        }

        private static void Lanceclear()
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
                        E.Cast(eFarmLoaction.Position);
                        return;
                    }
                }
            }
            
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
            
            var lcw = Config["Clear"].GetValue<MenuBool>("LcW");
            if (lcw.Enabled && W.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion())
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
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;
            var Qtarget = Q.GetTarget(Q.Range);
            var Wtarget = W.GetTarget(W.Range);
            var Etarget = E.GetTarget(E.Range);
            var Rtarget = R.GetTarget(R.Range);
            
            if (Wtarget == null) return;
            if (Wtarget.IsInvulnerable) return;
            if (Rtarget == null) return;
            if (Rtarget.IsInvulnerable) return;
            
            if (!(Me.Distance(Qtarget.Position) <= Q.Range) ||
                !(QDamage(Qtarget) >= Qtarget.Health + OktwCommon.GetIncomingDamage(Qtarget))) return;
            if (Q.IsReady() && ksQ) Q.Cast(Qtarget);

            if (!(Me.Distance(Wtarget.Position) <= W.Range) ||
                !(WDamage(Wtarget) >= Wtarget.Health + OktwCommon.GetIncomingDamage(Wtarget))) return;
            if (W.IsReady() && ksW) W.Cast(Wtarget);
            
            if (!(Me.Distance(Etarget.Position) <= E.Range) ||
                !(EDamage(Etarget) >= Etarget.Health + OktwCommon.GetIncomingDamage(Etarget))) return;
            if (E.IsReady() && ksE) E.Cast(Etarget);

            if (!(Me.Distance(Rtarget.Position) <= R.Range) ||
                !(RDamage(Rtarget) >= Rtarget.Health + OktwCommon.GetIncomingDamage(Rtarget))) return;
            if (R.IsReady() && ksR) R.Cast(Rtarget);
        }

        private static readonly float[] QBaseDamage = {0f, 60f, 105f, 150f, 195f, 240f, 240f};
        private static readonly float[] WBaseDamage = {0f, 80f, 120f, 160f, 200f, 240f, 240f};
        private static readonly float[] EBaseDamage = {0f, 50f, 70f, 90f, 110f, 130f, 130f};
        private static readonly float[] EBonus = {0f, 40f, 45f, 50f, 55f, 60f, 60f};
        private static readonly float[] RbaseDamage = {0f, 75f, 125f, 175f, 175f};
        
        private static float QDamage(AIBaseClient Qtarget)
        {
            var qLevel = Q.Level;
            var qbaseDamage = QBaseDamage[qLevel] + 0.6f * Me.TotalMagicalDamage;
            return (float) GameObjects.Player.CalculateDamage(Qtarget, DamageType.Magical, qbaseDamage);
        }

        private static float WDamage(AIBaseClient wtarget)
        {
            var wlevel = W.Level;
            var wbaseDamage = WBaseDamage[wlevel] + 0.8f * Me.TotalMagicalDamage;
            return (float) GameObjects.Player.CalculateDamage(wtarget, DamageType.Magical, wbaseDamage);
        }

        private static float EDamage(AIBaseClient etarget)
        {
            var eLevel = E.Level;
            var ebaseDamage = EBaseDamage[eLevel] + EBonus[eLevel] * Me.TotalMagicalDamage;
            return (float) GameObjects.Player.CalculateDamage(etarget, DamageType.Magical, ebaseDamage);
        }

        private static float RDamage(AIBaseClient rtarget)
        {
            var rLevel = R.Level;
            var rbaseDamage = RbaseDamage[rLevel] + 0.2f * Me.TotalMagicalDamage;
            return (float) GameObjects.Player.CalculateDamage(rtarget, DamageType.Magical, rbaseDamage);
        }
    }
}