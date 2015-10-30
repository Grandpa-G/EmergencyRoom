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
using TShockAPI.Net;

namespace EmergencyRoom
{
    [ApiVersion(1, 22)]
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

        private void ER(CommandArgs args)
        {
            bool addSubtract = false;
            int health = 0;
            int playerHealth = 0;
            int priorHealth = 0;
            int mana = 0;
            int playerMana = 0;
            int priorMana = 0;
            string playerName = "";
            bool noActionRequired = false;
            bool playerActive = false;
            TSPlayer player = null;

            string arg;
            if (args.Parameters.Count == 0)
            {
                args.Player.SendMessage("Syntax: /EmergencyRoom <user> [-help] ", Color.Red);
                args.Player.SendMessage("Flags: ", Color.LightSalmon);
                args.Player.SendMessage("   -help             this information", Color.LightSalmon);
                args.Player.SendMessage("   -health/-h <+/-n> sets the MaxHealth of player to <n>", Color.LightSalmon);
                args.Player.SendMessage("   -mana/-m <+/-n>   sets the MaxMana of player to <n>", Color.LightSalmon);
                return;
            }

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
            if (user == null)
            {
                args.Player.SendErrorMessage("Player " + playerName + " can't be found.");
                return;
            }

            playerActive = false;
            priorHealth = 0;
            priorMana = 0;
            var found = TShock.Utils.FindPlayer(playerName);
            if (found.Count == 1)
            {
                player = (TSPlayer)found[0];
                playerActive = true;
            }
            else
            {
                playerActive = false;
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

            //            Console.WriteLine(user.Name + user.ID + " " + priorHealth + ":" + priorMana);
            playerHealth = priorHealth;
            playerMana = priorMana;
            for (int i = 1; i < args.Parameters.Count; i++)
            {
                arg = args.Parameters[i];
                switch (arg)
                {
                    case "-x":
                        if (!playerActive)
                        {
                            args.Player.SendErrorMessage("Player " + playerName + " can't be found.");
                            break;
                        }
                        break;
                    case "-list":
                    case "-l":
                        if(playerActive)
                        args.Player.SendInfoMessage("Player " + playerName + " active");
                         args.Player.SendInfoMessage("Player " + playerName + " MaxHealth currently at " + priorHealth + " and MaxMana currently at " + priorMana);
                        noActionRequired = true;
                        break;
                    case "-h":
                        if (i + 1 >= args.Parameters.Count)
                        {
                            args.Player.SendErrorMessage("No value give for health");
                            noActionRequired = true;
                            break;
                        }
                        addSubtract = false;
                        if (args.Parameters[i + 1].StartsWith("+"))
                            addSubtract = true;
                        if (args.Parameters[i + 1].StartsWith("-"))
                            addSubtract = true;
                        if (Int32.TryParse(args.Parameters[i + 1], out health))
                        {
                            if (addSubtract)
                            {
                                if (playerHealth + health < 0)
                                {
                                    args.Player.SendErrorMessage("Health value may not be negative");
                                    noActionRequired = true;
                                    i++;
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
                            noActionRequired = true;
                            break;
                        }
                        i++;
                        break;

                    case "-mana":
                    case "-m":
                        if (i + 1 >= args.Parameters.Count)
                        {
                            args.Player.SendErrorMessage("No value give for mana");
                            noActionRequired = true;
                            break;
                        }
                        addSubtract = false;
                        if (args.Parameters[i + 1].StartsWith("+"))
                            addSubtract = true;
                        if (args.Parameters[i + 1].StartsWith("-"))
                            addSubtract = true;
                        if (Int32.TryParse(args.Parameters[i + 1], out mana))
                        {
                           if (addSubtract)
                            {
                                if (playerMana + mana < 0)
                                {
                                    args.Player.SendErrorMessage("Mana value may not be negative");
                                    noActionRequired = true;
                                    i++;
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
                            noActionRequired = true;
                            i++;
                            break;
                        }
                        i++;
                        break;

                    case "-help":
                        args.Player.SendMessage("Syntax: /EmergencyRoom <user> [-help] ", Color.Red);
                        args.Player.SendMessage("Flags: ", Color.LightSalmon);
                        args.Player.SendMessage("   -help             this information", Color.LightSalmon);
                        args.Player.SendMessage("   -health/-h <+/-n> sets the MaxHealth of player to <n>", Color.LightSalmon);
                        args.Player.SendMessage("   -mana/-m <+/-n>   sets the MaxMana of player to <n>", Color.LightSalmon);
                        return;

                    default:
                        args.Player.SendErrorMessage("Unkonown command argument:" + arg);
                        noActionRequired = true;
                        return;
                }
            }
            if (noActionRequired)
                return;

            if (playerActive)
            {
                player.TPlayer.statManaMax = playerMana;
                player.TPlayer.statLifeMax = playerHealth;
                player.SendData(PacketTypes.PlayerHp, player.Name, player.Index);
                //                player.SendData(PacketTypes.EffectMana, player.Name, player.Index);
                player.SendData(PacketTypes.PlayerMana, player.Name, player.Index);
                player.SendData(PacketTypes.PlayerUpdate, player.Name, player.Index);
            }

            try
            {
                using (var reader = TShock.DB.QueryReader("UPDATE tsCharacter SET  MaxHealth = @0, MaxMana = @1 WHERE Account = @2;", playerHealth, playerMana, user.ID))
                {
                }

            }
            catch (Exception ex)
            {
                args.Player.SendInfoMessage("Player " + playerName + " no database change.");
                TShock.Log.Error(ex.ToString());
                Console.WriteLine(ex.StackTrace);
                return;
            }
            string message = "";
            if (priorHealth != playerHealth)
                message = " MaxHealth changed from " + priorHealth + " to " + playerHealth;
            if (priorMana != playerMana)
                message += " MaxMana changed from " + priorMana + " to " + playerMana;
            args.Player.SendInfoMessage("Player " + playerName + message);
        }
    }

}
