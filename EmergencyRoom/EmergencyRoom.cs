using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Data;
using System.ComponentModel;
using System.Reflection;

using Terraria;
using TShockAPI;
using Newtonsoft.Json;
using TerrariaApi.Server;
using Newtonsoft.Json.Linq;
using TShockAPI.DB;

namespace EmergencyRoom
{
    [ApiVersion(1, 17)]
    public class EmergencyRoom : TerrariaPlugin
    {
        public static IDbConnection DB;

        public override string Name
        {
            get { return "EmergencyRoom"; }
        }
        public override string Author
        {
            get { return "Granpa-G"; }
        }
        public override string Description
        {
            get { return "Allows changing in-game players health and mana values."; }
        }
        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }
        public EmergencyRoom(Main game)
            : base(game)
        {
            Order = -1;
        }
        public override void Initialize()
        {
            //            ServerApi.Hooks.NetGetData.Register(this, GetData);

            Commands.ChatCommands.Add(new Command("EmergencyRoom.allow", ER, "emergencyroom"));
            Commands.ChatCommands.Add(new Command("EmergencyRoom.allow", ER, "er"));

        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
        }
        private static void OnLeave(LeaveEventArgs args)
        {
        }

        private void OnLogin(TShockAPI.Hooks.PlayerPostLoginEventArgs args)
        {
        }

        private void ER(CommandArgs args)
        {
            bool verbose = false;
            bool help = false;
            bool addSubtract = false;
            int health = 0;
            int playerHealth = 10;
            int priorHealth = 0;
            int mana = 0;
            int playerMana = 25;
            int priorMana = 0;
            string playerName = "";
            bool badArg = false;

            string arg;
            if (args.Parameters.Count == 0)
                help = true;

            arg = args.Parameters[0];
            {
                playerName = arg;
            }
            if (playerName.Length == 0)
            {
                args.Player.SendErrorMessage("No player name was given.");
                return;
            }

            TShockAPI.DB.User user = TShock.Users.GetUserByName(playerName);
            foreach (var player in TShock.Players.Where(p => null != p && p.UserAccountName == user.Name))
            {
                args.Player.SendErrorMessage("Player " + playerName + " active, properties may not be changed.");
                return;
            }

             try
            {
              using (var reader = TShock.DB.QueryReader("SELECT MaxHealth, MaxMana FROM tsCharacter where account =@0", user.ID))
                {
                    if (reader.Read())
                    {
                            priorHealth = reader.Get<Int32>("MaxHealth");
                         priorMana = reader.Get<Int32>("MaxMana");
                    }
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
                Console.WriteLine(ex.StackTrace);
                return;
            }

            for (int i = 1; i < args.Parameters.Count; i++)
            {
                arg = args.Parameters[i];
                if (arg.StartsWith("-l"))
                {
                }
                if (arg.StartsWith("-hp"))
                {
                    if (i + 1 >= args.Parameters.Count)
                    {
                        args.Player.SendErrorMessage("No value give for health");
                        badArg = true;
                        break;
                    }
                    addSubtract = false;
                    if (args.Parameters[i + 1].StartsWith("+"))
                        addSubtract = true;
                    if (args.Parameters[i + 1].StartsWith("-"))
                        addSubtract = true;
                    if (Int32.TryParse(args.Parameters[i + 1], out health))
                    {
                        if (health < 0)
                        {
                            args.Player.SendErrorMessage("Health value may not be negative");
                            badArg = true;
                            break;
                        }
                        if (addSubtract)
                        {
                            if (playerHealth + health < 0)
                            {
                                args.Player.SendErrorMessage("Health value may not be negative");
                                badArg = true;
                                break;
                            }
                            playerHealth = priorHealth + health;
                        }
                        else
                            playerHealth = health;
                    }
                    else
                    {
                        args.Player.SendErrorMessage("Health value not an integer");
                        badArg = true;
                        break;
                    }
                    i++;
                }
                if (arg.StartsWith("-mana"))
                {
                    if (i + 1 >= args.Parameters.Count)
                    {
                        args.Player.SendErrorMessage("No value give for mana");
                        badArg = true;
                        break;
                    }
                    addSubtract = false;
                    if (args.Parameters[i + 1].StartsWith("+"))
                        addSubtract = true;
                    if (args.Parameters[i + 1].StartsWith("-"))
                        addSubtract = true;
                    if (Int32.TryParse(args.Parameters[i + 1], out mana))
                    {
                        if (mana < 0)
                        {
                            args.Player.SendErrorMessage("Mana value may not be negative");
                            badArg = true;
                            break;
                        }
                        if (addSubtract)
                        {
                            if (playerMana + mana < 0)
                            {
                                args.Player.SendErrorMessage("Mana value may not be negative");
                                badArg = true;
                                break;
                            }
                            playerMana = priorMana + mana;
                        }
                        else
                            playerMana = mana;
                    }
                    else
                    {
                        args.Player.SendErrorMessage("Mana value not an integer");
                        badArg = true;
                        break;
                    }
                    i++;
                }
            }
            if (badArg)
                return;

            if (help)
            {
                args.Player.SendMessage("Syntax: /EmergencyRoom <user> [-help] ", Color.Red);
                args.Player.SendMessage("Flags: ", Color.LightSalmon);
                args.Player.SendMessage("   -help             this information", Color.LightSalmon);
                args.Player.SendMessage("   -health/-h <+/-n> sets the health of player to <n>", Color.LightSalmon);
                args.Player.SendMessage("   -mana/-h <+/-n>   sets the mana of player to <n>", Color.LightSalmon);
                return;
            }
 /*
            if (playerHealth >= TShock.ServerSideCharacterConfig.StartingHealth)
            {
                args.Player.SendErrorMessage("Player's health would be greater than max allowed, change rejected.");
                return;
            }
            if (playerMana >= TShock.ServerSideCharacterConfig.StartingMana)
            {
                args.Player.SendErrorMessage("Player's mana would be greater than max allowed, change rejected.");
                return;
            }
*/
            try
            {
                using (var reader = TShock.DB.QueryReader("UPDATE tsCharacter SET  MaxHealth = @0, MaxMana = @1 WHERE Account = @2;", playerHealth, playerMana, user.ID))
                {
                    args.Player.SendInfoMessage("Player " + playerName + " health changed from " + priorHealth + " to " + playerHealth + " and mana changed from " + priorMana + " to " + playerMana);
                }

            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
                Console.WriteLine(ex.StackTrace);
                return;
            }
            /*

                       player.Heal(85);
                       player.SendSuccessMessage(string.Format("{0} just healed you!", args.Player.Name));
                       NetMessage.SendData((int)PacketTypes.PlayerHealOther, -1, -1, "", player.TPlayer.whoAmi, playerHealth);
                       NetMessage.SendData((int)PacketTypes.PlayerMana, -1, -1, "", player.TPlayer.whoAmi, playerMana);
             * */
        }
    }

}
