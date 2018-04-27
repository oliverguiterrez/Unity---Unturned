﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rocket.API;
using Rocket.API.Commands;
using Rocket.API.DependencyInjection;
using Rocket.API.Eventing;
using Rocket.API.I18N;
using Rocket.API.Player;
using Rocket.API.Plugin;
using Rocket.Core;
using Rocket.Core.Implementation.Events;
using Rocket.Core.Player.Events;
using Rocket.Unturned.Console;
using Rocket.Unturned.Player;
using Rocket.Unturned.Player.Events;
using Rocket.Unturned.Utils;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
using ILogger = Rocket.API.Logging.ILogger;
using Object = UnityEngine.Object;

namespace Rocket.Unturned
{
    public class UnturnedImplementation : IImplementation
    {
        private GameObject rocketGameObject;
        private ILogger logger;
        private IPlayerManager playerManager;
        private IEventManager eventManager;
        private IDependencyContainer container;
        public ITranslationLocator ModuleTranslations { get; private set; }

        public bool IsAlive => true;
        private IConsoleCommandCaller consoleCaller;

        public void Init(IRuntime runtime)
        {
            rocketGameObject = new GameObject();
            Object.DontDestroyOnLoad(rocketGameObject);

            container = runtime.Container;
            eventManager = container.Get<IEventManager>();
            playerManager = container.Get<IPlayerManager>("unturnedplayermanager");
            ModuleTranslations = container.Get<ITranslationLocator>();


            logger = container.Get<ILogger>();
            logger.LogInformation("Loading Rocket Unturned Implementation...");

            container.RegisterSingletonType<AutomaticSaveWatchdog, AutomaticSaveWatchdog>();
            container.Get<AutomaticSaveWatchdog>().Start();

            string rocketDirectory = $"Servers/{Dedicator.serverID}/Rocket/";
            if (!Directory.Exists(rocketDirectory))
                Directory.CreateDirectory(rocketDirectory);

            Directory.SetCurrentDirectory(rocketDirectory);
            LoadTranslations();

            Provider.onServerHosted += OnServerHosted;

            if (Environment.OSVersion.Platform == PlatformID.Unix
                || Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                var console = rocketGameObject.AddComponent<UnturnedConsole>();
                console.Logger = logger;
            }

            SteamChannel.onTriggerSend += TriggerSend;
            Provider.onCheckValid += OnCheckValid;
            Provider.onServerConnected += OnPlayerConnected;
            Provider.onServerDisconnected += OnPlayerDisconnected;
        }

        private void LoadTranslations()
        {
            ModuleTranslations.Load(new ConfigurationContext
            {
                ConfigurationName = "Rocket.Unturned.ModuleTranslations",
                WorkingDirectory = WorkingDirectory
            },
            new Dictionary<string, string>
            {
                { "command_compass_facing_private","You are facing {0}"},
                { "command_compass_north","N"},
                { "command_compass_east","E"},
                { "command_compass_south","S"},
                { "command_compass_west","W"},
                { "command_compass_northwest","NW"},
                { "command_compass_northeast","NE"},
                { "command_compass_southwest","SW"},
                { "command_compass_southeast","SE"},
                { "command_heal_success_me","{0} was successfully healed"},
                { "command_heal_success_other","You were healed by {0}"},
                { "command_heal_success","You were healed"},
                { "command_bed_no_bed_found_private","You do not have a bed to teleport to."},
                { "command_i_giving_private","Giving you item {0}x {1} ({2})"},
                { "command_i_giving_failed_private","Failed giving you item {0}x {1} ({2})"},
                { "command_more_dequipped", "No item being held in hands." },
                { "command_more_give", "Giving {0} of item: {1}." },
                { "command_generic_teleport_while_driving_error","You cannot teleport while driving or riding in a vehicle."},
                { "command_tp_failed_find_destination","Failed to find destination"},
                { "command_tphere_vehicle", "The player you are trying to teleport is in a vehicle"},
                { "command_tphere_teleport_from_private","Teleported {0} to you"},
                { "command_tphere_teleport_to_private","You were teleported to {0}"},
                { "command_v_giving_private","Giving you a {0} ({1})"},
                { "command_v_giving_failed_private","Failed giving you a {0} ({1})"},
            });
        }

        private void OnPlayerConnected(CSteamID steamid)
        {
            var player = playerManager.GetOnlinePlayerById(steamid.ToString());
            PlayerConnectedEvent @event = new PlayerConnectedEvent(player);
            eventManager.Emit(this, @event);
        }

        private void OnPlayerDisconnected(CSteamID steamid)
        {
            var player = playerManager.GetOnlinePlayerById(steamid.ToString());
            PlayerDisconnectedEvent @event = new PlayerDisconnectedEvent(player, null);
            eventManager.Emit(this, @event);
        }

        private void OnCheckValid(ValidateAuthTicketResponse_t callback, ref bool isValid)
        {
            var pendingPlayer = Provider.pending.FirstOrDefault(c => c.playerID.steamID.Equals(callback.m_SteamID));
            if (pendingPlayer == null) return;

            PreConnectUnturnedPlayer player = new PreConnectUnturnedPlayer(container, pendingPlayer);
            UnturnedPlayerPreConnectEvent @event = new UnturnedPlayerPreConnectEvent(player, callback);
            eventManager.Emit(this, @event);

            if (@event.UnturnedRejectionReason != null)
            {
                Provider.reject(callback.m_SteamID, @event.UnturnedRejectionReason.Value);
                isValid = false;
                return;
            }

            if (@event.IsCancelled)
            {
                Provider.reject(callback.m_SteamID, ESteamRejection.PLUGIN);
                isValid = false;
                return;
            }

            isValid = true;
        }

        private void OnServerHosted()
        {
            //proxied
            var pluginManager = container.Get<IPluginManager>();
            pluginManager.Init();

            ImplementationReadyEvent @event = new ImplementationReadyEvent(this);
            eventManager.Emit(this, @event);
        }

        internal void TriggerSend(SteamPlayer player, string method, ESteamCall steamCall, ESteamPacket steamPacket, params object[] data)
        {
            try
            {
                if (player == null
                    || player.player == null
                    || player.playerID.steamID == CSteamID.Nil
                    || player.player.transform == null
                    || data == null) return;

                UnturnedPlayer unturnedPlayer =
                    (UnturnedPlayer)playerManager.GetPlayer(player.playerID.steamID.ToString());

                if (method.StartsWith("tellWear"))
                {
                    //PlayerWearEvent method.Replace("tellWear", ""), (ushort)data[0], data.Count() > 1 ? (byte?)data[1] : null)
                    return;
                }

                switch (method)
                {
                    case "tellBleeding":
                        //PlayerBleedingUpdateEvent (bool)data[0]
                        break;
                    case "tellBroken":
                        //PlayerUpdateBrokenEvent (bool)data[0]
                        break;
                    case "tellLife":
                        //PlayerUpdateLifeEvent (byte)data[0]
                        break;
                    case "tellFood":
                        //PlayerUpdateFoodEvent (byte)data[0]
                        break;
                    case "tellHealth":
                        //PlayerUpdateHealthEvent (byte)data[0]
                        break;
                    case "tellVirus":
                        //PlayerUpdateVirusEvent (byte)data[0]
                        break;
                    case "tellWater":
                        //PlayerUpdateWaterEvent (byte)data[0]
                        break;
                    case "tellStance":
                        //PlayerUpdateStanceEvent (byte)data[0]
                        break;
                    case "tellGesture":
                        //PlayerUpdateGestureEvent (byte)data[0].ToString()
                        break;
                    case "tellStat":
                        //PlayerUpdateStatEvent (EPlayerStat)(byte)data[0])
                        break;
                    case "tellExperience":
                        //PlayerUpdateExperienceEvent (uint)data[0])
                        break;
                    case "tellRevive":
                        //OnPlayerReviveEvent (Vector3)data[0], (byte)data[1]
                        break;
                    case "tellDead":
                        {
                            var position = (Vector3)data[0];
                            UnturnedPlayerDeadEvent @event = new UnturnedPlayerDeadEvent(unturnedPlayer, position);
                            eventManager.Emit(this, @event);
                            break;
                        }
                    case "tellDeath":
                        {
                            var deathCause = (EDeathCause)(byte)data[0];
                            var limb = (ELimb)(byte)data[1];
                            var killerId = data[2].ToString();

                            var killer = killerId != "0" ? playerManager.GetOnlinePlayerById(killerId) : null;
                            UnturnedPlayerDeathEvent @event =
                                new UnturnedPlayerDeathEvent(unturnedPlayer, limb, deathCause, killer);
                            eventManager.Emit(this, @event);
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to receive packet \"" + method + "\"", ex);
            }
        }

        public void Shutdown()
        {
            Provider.shutdown();
        }

        public void Reload() { }
        public IConsoleCommandCaller GetConsoleCaller()
        {
            if (consoleCaller == null)
                consoleCaller = new ConsoleCaller();

            return consoleCaller;
        }

        public IEnumerable<string> Capabilities => new List<string>();
        public string InstanceId => Provider.serverID;
        public string WorkingDirectory => Directory.GetCurrentDirectory();
        public string ConfigurationName => "Rocket.Unturned";
        public string Name => "Rocket.Unturned";
    }
}