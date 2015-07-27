﻿using Rocket.Core;
using Rocket.Core.Logging;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Rocket.API;

namespace Rocket.Unturned.Chat
{
    public sealed class UnturnedChat : MonoBehaviour
    {
        private void Awake()
        {
            SDG.Unturned.ChatManager.OnChatted += handleChat;
        }

        private void handleChat(SteamPlayer steamPlayer, EChatMode chatMode, ref Color incomingColor, string message)
        {
            Color color = incomingColor;
            try
            {
                UnturnedPlayer player = UnturnedPlayer.FromSteamPlayer(steamPlayer);
                if (player.IsAdmin)
                {
                    color = Palette.Admin;
                }
                else
                {
                    string colorPermission = R.Permissions.GetPermissions(player).Where(permission => permission.ToLower().StartsWith("color.")).FirstOrDefault();
                    if (colorPermission != null)
                    {
                        color = GetColorFromName(colorPermission.ToLower().Replace("color.", ""), color);
                    }
                }

                color = UnturnedPlayerEvents.firePlayerChatted(player, chatMode, color, message);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            incomingColor = color;
        }

        public static Color GetColorFromName(string colorName, Color fallback)
        {
            switch (colorName.Trim().ToLower())
            {
                case "black": return Color.black;
                case "blue": return Color.blue;
                case "clear": return Color.clear;
                case "cyan": return Color.cyan;
                case "gray": return Color.gray;
                case "green": return Color.green;
                case "grey": return Color.grey;
                case "magenta": return Color.magenta;
                case "red": return Color.red;
                case "white": return Color.white;
                case "yellow": return Color.yellow;
                case "rocket": return GetColorFromRGB(90, 206, 205);
            }

            Color? color = GetColorFromHex(colorName);
            if (color.HasValue) return color.Value;

            return fallback;
        }

        public static Color? GetColorFromHex(string hexString)
        {
            hexString = hexString.Replace("#", "");
            if(hexString.Length ==3) hexString+=hexString;
            int argb;
            if (hexString.Length != 6 || !Int32.TryParse(hexString, System.Globalization.NumberStyles.HexNumber, null, out argb))
            {
                return null;
            }
            byte r = (byte)((argb >> 16) & 0xff);
            byte g = (byte)((argb >> 8) & 0xff);
            byte b = (byte)(argb & 0xff);
            return GetColorFromRGB(r, g, b);
        }
		public static Color GetColorFromRGB(byte R,byte G,byte B)
		{
			return GetColorFromRGB (R, G, B, 100);
		}
        public static Color GetColorFromRGB(byte R,byte G,byte B,short A)
        {
            return new Color((1f / 255f) * R, (1f / 255f) * G, (1f / 255f) * B,(1f/100f) * A);
        }

        public static void Say(string message)
        {
            Say(message, Palette.Server);
        }

        public static void Say(string message,Color color)
        {
            System.Console.ForegroundColor = ConsoleColor.Gray;
            System.Console.WriteLine(message);
            System.Console.ForegroundColor = ConsoleColor.White;
            Logger.Log("Broadcast: " + message,false);
            foreach (string m in wrapMessage(message))
            {
                ChatManager.Instance.SteamChannel.send("tellChat", ESteamCall.OTHERS, ESteamPacket.UPDATE_UDP_BUFFER, new object[] { CSteamID.Nil, (byte)EChatMode.GLOBAL,color, m });
            }
        }
       
        public static void Say(IRocketPlayer player, string message)
        {
            Say(player, message, Palette.Server);
        }

        public static void Say(IRocketPlayer player, string message, Color color)
        {
            if (player is ConsolePlayer)
            {
                System.Console.ForegroundColor = ConsoleColor.Gray;
                System.Console.WriteLine(message);
                System.Console.ForegroundColor = ConsoleColor.White;
                Logger.Log(message,false);
            }
            else
            {
                Say(new CSteamID(ulong.Parse(player.Id)), message, color);
            }
        }

        public static void Say(CSteamID CSteamID, string message)
        {
            Say(CSteamID, message, Palette.Server);
        }

        public static void Say(CSteamID CSteamID, string message, Color color)
        {
            if (CSteamID == null || CSteamID.ToString() == "0")
            {
                System.Console.ForegroundColor = ConsoleColor.Gray;
                System.Console.WriteLine(message);
                Logger.Log(message,false);
            }
            else
            {   
                foreach (string m in wrapMessage(message))
                {
                    ChatManager.Instance.SteamChannel.send("tellChat", CSteamID, ESteamPacket.UPDATE_UDP_BUFFER, new object[] { CSteamID.Nil, (byte)EChatMode.SAY,color, m });
                }
            }
        }

         public static List<string> wrapMessage(string text)
         {
             if (text.Length == 0) return new List<string>();
             string[] words = text.Split(' ');
             List<string> lines = new List<string>();
             string currentLine = "";
             int maxLength = 90;
             foreach (var currentWord in words)
             {
  
                 if ((currentLine.Length > maxLength) ||
                     ((currentLine.Length + currentWord.Length) > maxLength))
                 {
                     lines.Add(currentLine);
                     currentLine = "";
                 }
  
                 if (currentLine.Length > 0)
                     currentLine += " " + currentWord;
                 else
                     currentLine += currentWord;
  
             }
  
             if (currentLine.Length > 0)
                 lines.Add(currentLine);
                 return lines;
            }
    }
}