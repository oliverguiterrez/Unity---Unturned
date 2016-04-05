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
using Rocket.Core.Utils;

namespace Rocket.Unturned.Commands
{
    public sealed class UnturnedCommands : MonoBehaviour
    {
        private void Awake()
        {
            foreach(Command vanillaCommand in Commander.Commands)
            {
                R.Commands.Register(new UnturnedVanillaCommand(vanillaCommand));
            }
        }

        internal class UnturnedVanillaCommand : IRocketCommand
        {
            public Command command;

            public UnturnedVanillaCommand(Command command)
            {
                this.command = command;
            }

            public List<string> Aliases
            {
                get
                {
                   return new List<string>();
                }
            }

            public AllowedCaller AllowedCaller
            {
                get
                {
                    return AllowedCaller.Both;
                }
            }

            public string Help
            {
                get
                {
                    return command.help;
                }
            }

            public string Name
            {
                get
                {
                    return command.command;
                }
            }

            public List<string> Permissions
            {
                get
                {
                    return new List<string>() { "unturned."+command.command.ToLower() };
                }
            }

            public string Syntax
            {
                get
                {
                    return command.info;
                }
            }

            public void Execute(IRocketPlayer caller, string[] command)
            {
                string c = String.Join(" ", command);
                Logger.Log("Excecutin vanilla command: " + c);
                CSteamID id = CSteamID.Nil;
                if(caller is UnturnedPlayer)
                {
                    id = ((UnturnedPlayer)caller).CSteamID;
                }
                Commander.execute(id, c);
            }
        }




    }
}