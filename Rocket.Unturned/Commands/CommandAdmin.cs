﻿using System;
using Rocket.API.Commands;
using Rocket.API.Player;
using Rocket.Core.Commands;
using Rocket.Unturned.Player;

namespace Rocket.Unturned.Commands
{
    public class CommandAdmin : ICommand
    {
        public bool SupportsCaller(Type commandCaller)
        {
            return true; //anyone can use the command
        }

        public void Execute(ICommandContext context)
        {
            if(context.Parameters.Length != 1)
                throw new CommandWrongUsageException();

            ICommandCaller targetUser = context.Parameters.Get<IOnlinePlayer>(0);

            if (targetUser is UnturnedPlayer uPlayer && !uPlayer.IsAdmin)
            {
                uPlayer.Admin(true);
                return;
            }

            context.Caller.SendMessage($"Could not admin {targetUser.Name}" , ConsoleColor.Red);
        }

        public string Name => "Admin";
        public string Summary => "Gives admin to a player.";
        public string Description => null;
        public string Permission => "Rocket.Unturned.Admin";
        public string Syntax => "<target player>";
        public ISubCommand[] ChildCommands => null;
        public string[] Aliases => null;
    }
}