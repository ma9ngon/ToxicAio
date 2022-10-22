using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnsoulSharp.SDK.Core;
using Newtonsoft.Json;

namespace ToxicAio.Utils
{
    using EnsoulSharp;
    using EnsoulSharp.SDK;
    using EnsoulSharp.SDK.MenuUI;
    using EnsoulSharp.SDK.Utility;

    using Color = System.Drawing.Color;

    public class SkinChanger
    {
        public static Menu menu;
        private static List<CharSkin> charSkins = new List<CharSkin>(); 
        public static void OnLoad()
        {
            try
            {

                menu = new Menu("skinhack", "Toxic SkinChanger", true);

                var allies = menu.Add(new Menu("Allies", "Allies"));
                var enemies = menu.Add(new Menu("Enemies", "Enemies"));

                foreach (var item in ObjectManager.Get<AIHeroClient>())
                {
                    var target = item;

                    CharSkin hisskin = new CharSkin(target.NetworkId, 0);

                    if (!charSkins.Contains(hisskin))
                    {
                        charSkins.Add(hisskin);

                        if (hisskin.IsAlly)
                        {
                            allies.Add(hisskin.Menu);
                        }
                        else
                        {
                            enemies.Add(hisskin.Menu);
                        }
                    }
                }

                menu.Attach();
            }
            catch
            {
                Console.WriteLine("Load Skin Changer Error");
            }
        }
    }
    class CharSkin
    {
        public int SkinID;
        public int UID;
        public bool IsAlly;
        public MenuSlider Menu;

        public CharSkin(int uid, int skinid)
        {
            SkinID = skinid;
            UID = uid;
            var target = ObjectManager.GetUnitByNetworkId<AIHeroClient>(uid);
            IsAlly = target.IsAlly;

            Menu = new MenuSlider(target.IsAlly + target.NetworkId.ToString(), target.CharacterName + " Skin ID", 0, 0, 50);

            Menu.ValueChanged += Menu_ValueChanged;
        }

        private void Menu_ValueChanged(MenuSlider menuItem, EventArgs args)
        {
            var target = ObjectManager.GetUnitByNetworkId<AIHeroClient>(this.UID);

            target.SetSkin(menuItem.Value);
        }

    }
}
