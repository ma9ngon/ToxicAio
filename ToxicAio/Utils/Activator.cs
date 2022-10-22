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
using System.Windows.Controls;
using SebbyLib;
using EnsoulSharp.SDK.Rendering;
using Menu = EnsoulSharp.SDK.MenuUI.Menu;
using EnsoulSharp.SDK.Utility;

namespace ToxicAio.Utils
{
    public class Activator
    {
        private static Menu Config, OffensiveItems, DefensiveItems, Summoners;
        private static AIHeroClient Me = ObjectManager.Player;
        private static Items.Item Goredrinker, GaleForce, EverFrost, Ironspike, Stride, Claw, Qss, Mercurial, Zhonyas, Rocketbelt;
        private static Spell ignite, cleanse;
        private static SpellSlot igniteSlot, cleanseSlot;
        public static void OnGameLoad()
        {
            Ironspike = new Items.Item(6029, 450f);
            Goredrinker = new Items.Item(6630, 450f);
            EverFrost = new Items.Item(6656, 900f);
            GaleForce = new Items.Item(6671, 425f);
            Stride = new Items.Item(6631, 450f);
            Claw = new Items.Item(6693, 500f);
            Qss = new Items.Item(3140, 0f);
            Mercurial = new Items.Item(3139, 0f);
            Zhonyas = new Items.Item(3157, 0f);
            Rocketbelt = new Items.Item(3152, 1000f);
            ignite = new Spell(SpellSlot.Summoner1, 600f);
            ignite = new Spell(SpellSlot.Summoner2, 600f);
            igniteSlot = Me.GetSpellSlot("SummonerDot");
            cleanse = new Spell(SpellSlot.Summoner1, 0f);
            cleanse = new Spell(SpellSlot.Summoner2, 0f);
            cleanseSlot = Me.GetSpellSlot("SummonerBoost");


            Config = new Menu("Activator", "[ToxicAio Reborn]: Activator", true);

            OffensiveItems = new Menu("OffensiveItems", "Offensive Items");
            OffensiveItems.Add(new MenuSeparator("Ever", "Everfrost"));
            OffensiveItems.Add(new MenuBool("useever", "Enable Everfrost"));
            OffensiveItems.Add(new MenuSeparator("force", "Galeforce"));
            OffensiveItems.Add(new MenuBool("usegale", "Enable Galeforce"));
            OffensiveItems.Add(new MenuSlider("galepercent", "Hp % to use Galeforce", 50, 1, 100));
            OffensiveItems.Add(new MenuSeparator("drinker", "Goredrinker"));
            OffensiveItems.Add(new MenuBool("usewhip", "Enable Ironspike Whip"));
            OffensiveItems.Add(new MenuBool("useDrinker", "Enable Goredrinker"));
            OffensiveItems.Add(new MenuSeparator("claw", "Prowlers Claw"));
            OffensiveItems.Add(new MenuBool("useclaw", "Enable Prowlers Claw"));
            OffensiveItems.Add(new MenuBool("clawsafe", "Enable Prowlers Claw Safe Mode"));
            OffensiveItems.Add(new MenuSeparator("breaker", "Stridebreaker"));
            OffensiveItems.Add(new MenuBool("usebreaker", " Enable Stridebreaker"));
            OffensiveItems.Add(new MenuSeparator("belt", "Hextech Rocketbelt"));
            OffensiveItems.Add(new MenuBool("useBelt", "Enable Hextech Rocketbelt"));
            Config.Add(OffensiveItems);

            DefensiveItems = new Menu("DefensiveItems", "Defensive Items");
            DefensiveItems.Add(new MenuSeparator("Qss", "Qss"));
            DefensiveItems.Add(new MenuBool("useqss", "Enable Qss"));
            DefensiveItems.Add(new MenuBool("qssStun", "Use Qss For Stun"));
            DefensiveItems.Add(new MenuBool("qssSnare", "Use Qss For Snare"));
            DefensiveItems.Add(new MenuBool("qssCharm", "Use Qss For Charm"));
            DefensiveItems.Add(new MenuSeparator("Zhonyas", "Zhonyas"));
            DefensiveItems.Add(new MenuBool("usezhonyas", "Enable Zhonyas for Dangerous Spells"));
            Config.Add(DefensiveItems);

            Summoners = new Menu("Summoners", "Summoner Settings");
            Summoners.Add(new MenuSeparator("Ignite", "Ignite"));
            Summoners.Add(new MenuBool("useignite", "use Ignite To Kill Target"));
            Summoners.Add(new MenuSeparator("Cleanse", "Cleanse"));
            Summoners.Add(new MenuBool("usecleanse", "Use Cleanse for CC"));
            Config.Add(Summoners);
            

            Config.Attach();

            GameEvent.OnGameTick += OnGameUpdate;
            AIBaseClient.OnDoCast += ZhonyasDanger;
        }

        public static void OnGameUpdate(EventArgs args)
        {

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {

                Ironspikewhip();
                Goredrinkerr();
                Galeforcee();
                Stridebreakerr();
                Claww();
                Everfrostt();
                HextechBelt();
            }
            Qsss();
            ignitekill();
            cleanseCC();           
        }

        private static void Ironspikewhip()
        {
            var Ironspikewhip = TargetSelector.GetTarget(450f, DamageType.Physical);
            var usedwhipr = Config["OffensiveItems"].GetValue<MenuBool>("usewhip").Enabled;

            if (Ironspikewhip.IsValidTarget(450f) && usedwhipr && Ironspike.IsInRange(Ironspikewhip))
            {
                Ironspike.Cast();
            }
        }

        private static void Goredrinkerr()
        {
            var gordetarget = TargetSelector.GetTarget(450f, DamageType.Physical);
            var usedrinkerr = Config["OffensiveItems"].GetValue<MenuBool>("useDrinker").Enabled;
            if (gordetarget == null) return;

            if (gordetarget.IsValidTarget(450f) && usedrinkerr)
            {
                Goredrinker.Cast();
            }
        }

        private static void Galeforcee()
        {
            var galetarget = TargetSelector.GetTarget(750f, DamageType.Magical);
            var usegalee = Config["OffensiveItems"].GetValue<MenuBool>("usegale").Enabled;
            var galehealth = Config["OffensiveItems"].GetValue<MenuSlider>("galepercent").Value;
            if (galetarget == null) return;

            if (galetarget.IsValidTarget(750f) && usegalee && galetarget.HealthPercent <= galehealth)
            {
                GaleForce.Cast(galetarget.Position);
            }
        }

        private static void HextechBelt()
        {
            var belttarget = TargetSelector.GetTarget(1000f, DamageType.Magical);
            var usebelt = Config["OffensiveItems"].GetValue<MenuBool>("useBelt").Enabled;
            if (belttarget == null) return;

            if (belttarget.IsValidTarget(1000f) && usebelt && Me.HealthPercent >= belttarget.HealthPercent ||
                BeltDamage(belttarget) >= belttarget.Health)
            {
                Rocketbelt.Cast(belttarget.Position);
            }
        }

        private static void Stridebreakerr()
        {
            var stridetarget = TargetSelector.GetTarget(450f, DamageType.Physical);
            var usestride = Config["OffensiveItems"].GetValue<MenuBool>("usebreaker").Enabled;
            if (stridetarget == null) return;

            if (stridetarget.IsValidTarget(450f) && usestride)
            {
                Stride.Cast();
            }

        }

        private static void Claww()
        {
            var clawtarget = TargetSelector.GetTarget(500f, DamageType.Physical);
            var useclaw = Config["OffensiveItems"].GetValue<MenuBool>("useclaw");
            var safe = Config["OffensiveItems"].GetValue<MenuBool>("clawsafe");
            if (clawtarget == null) return;

            if (clawtarget.IsValidTarget(500f) && useclaw.Enabled && !safe.Enabled)
            {
                Claw.Cast(clawtarget);
            }
            else if (clawtarget.IsValidTarget(500f) && useclaw.Enabled && safe.Enabled)
            {
                if (Me.HealthPercent >= clawtarget.HealthPercent)
                {
                    Claw.Cast(clawtarget);
                }
            }
        }

        private static void Everfrostt()
        {
            var evertarget = TargetSelector.GetTarget(900f, DamageType.Magical);
            var useever = Config["OffensiveItems"].GetValue<MenuBool>("useever");
            if (evertarget == null) return;

            if (evertarget.IsValidTarget(900f) && useever.Enabled)
            {
                if (!evertarget.HasBuffOfType(BuffType.Berserk) || !evertarget.HasBuffOfType(BuffType.SpellImmunity) ||
                    !evertarget.HasBuffOfType(BuffType.SpellShield) || !evertarget.HasBuffOfType(BuffType.Stun) ||
                    !evertarget.HasBuffOfType(BuffType.Snare))
                {
                    EverFrost.Cast(evertarget.Position);
                }
            }
        }

        private static void Qsss()
        {
            var useqss = Config["DefensiveItems"].GetValue<MenuBool>("useqss");
            var useqssstun = Config["DefensiveItems"].GetValue<MenuBool>("qssStun");
            var useqssSnare = Config["DefensiveItems"].GetValue<MenuBool>("qssSnare");
            var useqssCharm = Config["DefensiveItems"].GetValue<MenuBool>("qssCharm");

            if (useqss.Enabled && Me.HasBuffOfType(BuffType.Charm) && useqssCharm.Enabled)
            {
                Qss.Cast();
                Mercurial.Cast();
            }
            
            if (useqss.Enabled && Me.HasBuffOfType(BuffType.Stun) && useqssstun.Enabled)
            {
                Qss.Cast();
                Mercurial.Cast();
            }
            
            if (useqss.Enabled && Me.HasBuffOfType(BuffType.Snare) && useqssSnare.Enabled)
            {
                Qss.Cast();
                Mercurial.Cast();
            }           
        }

        #region Zhonyas
        private static void ZhonyasDanger(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            var usezhonyass = Config["DefensiveItems"].GetValue<MenuBool>("usezhonyas");

            if (sender == null || !sender.IsValid() || sender.GetType() != typeof(AIHeroClient) || !sender.IsEnemy || !usezhonyass.Enabled)
            {
                return;
            }

            if (sender.CharacterName == "Malphite" && args.Slot == SpellSlot.R && args.Target != null &&
                args.Target.IsValid && args.Target.IsMe && args.End.Distance(Me.Position) < 325f)
            {
                Zhonyas.Cast();
            }

            if (sender.CharacterName == "Garen" && args.Slot == SpellSlot.R && args.Target != null &&
                args.Target.IsValid && args.Target.IsMe)
            {
                Zhonyas.Cast();
            }

            if (sender.CharacterName == "Leesin" && args.Slot == SpellSlot.R && args.Target != null &&
                args.Target.IsValid && args.Target.IsMe)
            {
                Zhonyas.Cast();
            }

            if (sender.CharacterName == "Darius" && args.Slot == SpellSlot.R && args.Target != null &&
                args.Target.IsValid && args.Target.IsMe)
            {
                Zhonyas.Cast();
            }

            if (sender.CharacterName == "Chogath" && args.Slot == SpellSlot.R && args.Target != null &&
                args.Target.IsValid && args.Target.IsMe)
            {
                Zhonyas.Cast();
            }

            if (sender.CharacterName == "Amumu" && args.Slot == SpellSlot.R && args.Target != null &&
                args.Target.IsValid && args.Target.IsMe)
            {
                Zhonyas.Cast();
            }

            if (sender.CharacterName == "Pyke" && args.Slot == SpellSlot.R && args.Target != null &&
                args.Target.IsValid && args.Target.IsMe)
            {
                Zhonyas.Cast();
            }

            if (sender.CharacterName == "Rengar" && args.Slot == SpellSlot.R && args.Target != null &&
                args.Target.IsValid && args.Target.IsMe)
            {
                Zhonyas.Cast();
            }

            if (sender.CharacterName == "Syndra" && args.Slot == SpellSlot.R && args.Target != null &&
                args.Target.IsValid && args.Target.IsMe)
            {
                Zhonyas.Cast();
            }

            if (sender.CharacterName == "Evelynn" && args.Slot == SpellSlot.R && args.Target != null &&
                args.Target.IsValid && args.Target.IsMe)
            {
                Zhonyas.Cast();
            }

            if (sender.CharacterName == "Veigar" && args.Slot == SpellSlot.R && args.Target != null &&
                args.Target.IsValid && args.Target.IsMe)
            {
                Zhonyas.Cast();
            }

            if (sender.CharacterName == "Annie" && args.Slot == SpellSlot.R && args.Target != null &&
                args.Target.IsValid && args.Target.IsMe)
            {
                Zhonyas.Cast();
            }

            if (sender.CharacterName == "Gragas" && args.Slot == SpellSlot.R && args.Target != null &&
                args.Target.IsValid && args.Target.IsMe)
            {
                Zhonyas.Cast();
            }

            if (sender.CharacterName == "Zed" && args.Slot == SpellSlot.R && args.Target != null &&
                args.Target.IsValid && args.Target.IsMe)
            {
                DelayAction.Add(300, () =>
                {
                    Zhonyas.Cast();
                });
            }
        }
        #endregion

        private static void ignitekill()
        {
            var useIgnite = Config["Summoners"].GetValue<MenuBool>("useignite");
            var hero = GameObjects.EnemyHeroes.FirstOrDefault(x => x.IsValidTarget(600f));
            var ignitedamage = Me.GetSummonerSpellDamage(hero, SummonerSpell.Ignite);
            if (hero == null) return;

            if (useIgnite.Enabled && hero.IsValidTarget(600f) && ignitedamage >= hero.Health && Me.Spellbook.CanUseSpell(igniteSlot) == SpellState.Ready)
            {
                Me.Spellbook.CastSpell(igniteSlot, hero);
            }
        }

        private static void cleanseCC()
        {
            var useCleanse = Config["Summoners"].GetValue<MenuBool>("usecleanse");

            if (useCleanse.Enabled && Me.Spellbook.CanUseSpell(cleanseSlot) == SpellState.Ready)
            {
                if (Me.HasBuffOfType(BuffType.Charm) || Me.HasBuffOfType(BuffType.Fear) ||
                    Me.HasBuffOfType(BuffType.Snare) || Me.HasBuffOfType(BuffType.Stun) ||
                    Me.HasBuffOfType(BuffType.Suppression))
                {
                    Me.Spellbook.CastSpell(cleanseSlot);
                }
            }
        }

        private static float BeltDamage(AIBaseClient target)
        {
            var baseDamage = 125f + .15f * Me.TotalMagicalDamage;
            return (float)Me.CalculateDamage(target, DamageType.Magical, baseDamage);
        }
    }
}