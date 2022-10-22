using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using SharpDX;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using EnsoulSharp.SDK.Utility;
using ToxicAio.Champions;
using ToxicAio.sebbylib;
using ToxicAio.Utils;
using Render = ToxicAio.Utils.Render;

namespace ToxicAio
{

    public class Program
    {
        private static readonly Render.Sprite ToxicSprite = new Render.Sprite(ToxicAio.Properties.Resources.Logo, new Vector2((Drawing.Width / 2) - 100, (Drawing.Height / 2) - 500));

        public static void Main(string[] args)
        {
            GameEvent.OnGameLoad += OnLoadingComplete;
            ProgramLoad();
            ToxicSprite.Add(0);
            ToxicSprite.OnDraw();
            DelayAction.Add(8000, () => ToxicSprite.Remove());
        }

        private static void OnLoadingComplete()
        {
            if (ObjectManager.Player == null)
                return;
            try
            {
                if (LoadChamps.Enabled)
                {
                    switch (GameObjects.Player.CharacterName)
                    {                        
                        case "Veigar":
                            Veigar.OnGameLoad();                           
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Thank you for Supporting Me ^^ </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + $"Expiry -> {User.Expiry}");
                            break;
                        
                        case "Evelynn":
                            Evelynn.OnGameLoad();
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Thank you for Supporting Me ^^ </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + $"Expiry -> {User.Expiry}");
                            break;
                        
                        case "Nilah":
                            Nilah.OnGameLoad();
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Thank you for Supporting Me ^^ </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + $"Expiry -> {User.Expiry}");
                            break;
                        
                        case "Xerath":
                            Xerath.OnGameLoad();
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Thank you for Supporting Me ^^ </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + $"Expiry -> {User.Expiry}");
                            break;
                        
                        case "Jhin":
                            Jhin.OnGameLoad();
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Thank you for Supporting Me ^^ </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + $"Expiry -> {User.Expiry}");
                            break;
                        
                        case "Olaf":
                            Olaf.OnGameLoad();
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Thank you for Supporting Me ^^ </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + $"Expiry -> {User.Expiry}");
                            break;
                        
                        case "Diana":
                            Diana.OnGameLoad();
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Thank you for Supporting Me ^^ </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + $"Expiry -> {User.Expiry}");
                            break;
                        
                        case "Tristana":
                            Tristana.OnGameLoad();
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Thank you for Supporting Me ^^ </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + $"Expiry -> {User.Expiry}");
                            break;
                        
                        case "Akali":
                            Akali.OnGameLoad();
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Thank you for Supporting Me ^^ </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + $"Expiry -> {User.Expiry}");
                            break;
                        
                        case "Cassiopeia":
                            Cassiopeia.OnGameLoad();
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Thank you for Supporting Me ^^ </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + $"Expiry -> {User.Expiry}");
                            break;
                        
                        case "Viktor":
                            Viktor.OnGameLoad();
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Thank you for Supporting Me ^^ </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + $"Expiry -> {User.Expiry}");
                            break;
                        
                        case "Kalista":
                            Kalista.OnGameLoad();
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Thank you for Supporting Me ^^ </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + $"Expiry -> {User.Expiry}");
                            break;
                        
                        case "Twitch":
                            Twitch.OnGameLoad();
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Thank you for Supporting Me ^^ </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + $"Expiry -> {User.Expiry}");
                            break;
                        
                        case "Ezreal":
                            Ezreal.OnGameLoad();
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Thank you for Supporting Me ^^ </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + $"Expiry -> {User.Expiry}");
                            break;
                        
                        case "LeeSin":
                            LeeSin.OnGameLoad();
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Thank you for Supporting Me ^^ </font>");
                            Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + $"Expiry -> {User.Expiry}");
                            break;

                        default:
                            Game.Print("<font color='#ff0000' size='25'>[ToxicAio] Does Not Support: " + ObjectManager.Player.CharacterName + "</font>");
                            Console.WriteLine("[ToxicAio] Does Not Support " + ObjectManager.Player.CharacterName);
                            break;
                    }                                   
                }

                if (LoadActivator.Enabled)
                {
                    Utils.Activator.OnGameLoad();
                    Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "Toxic Activator" + " Loaded");
                }

                if (LoadSkinChanger.Enabled)
                {
                    SkinChanger.OnLoad();
                    Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "Toxic SkinChanger" + " Loaded");
                }

                string stringg;
                string uri = "https://raw.githubusercontent.com/AkaneV2/ToxicAio/main/versionprivate.txt";
                using (WebClient client = new WebClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    stringg = client.DownloadString(uri);
                }
                string versionas = "2.0.26.4\n";
                if (versionas != stringg)
                {
                    Game.Print("<font color='#ff0000'> [ToxicAio]: </font> <font color='#ffe6ff' size='25'>You don't have the current version, please UPDATE !</font>");
                }
                else if (versionas == stringg)
                {
                    Game.Print("<font color='#ff0000' size='25'> [ToxicAio]: </font> <font color='#ffe6ff' size='25'>Is updated to the latest version!</font>");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in loading: " + ex);
            }
        }       

        private static Menu menuu = null;
        private static MenuBool LoadChamps = new MenuBool("Champs", "Load Champions");
        private static MenuBool LoadActivator = new MenuBool("Activator", "Load Activator");
        private static MenuBool LoadSkinChanger = new MenuBool("SkinChanger", "Load SkinChanger");
        private static MenuSeparator F5 = new MenuSeparator("F5", "Press F5 if you Changed Anything here");
        private static MenuSeparator credit = new MenuSeparator("creidt", "Made by Akane");
        
        private static void ProgramLoad()
        {
            var rad = ToxicAio.Properties.Resources.zahnrad;
            menuu = new Menu("Toxic", "ToxicAio Core", true);
            menuu.Add(LoadChamps);
            menuu.Add(LoadActivator);
            menuu.Add(LoadSkinChanger);
            menuu.Add(F5);
            menuu.Add(credit);
            menuu.Attach();
        }
    }
}
