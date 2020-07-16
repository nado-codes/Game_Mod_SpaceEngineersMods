using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using VRageMath;
using VRage.ModAPI;
using VRage.Game.ModAPI;
using VRage.Game.Components;
using Sandbox.Game.Entities;
using SpaceEngineers.Game.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Entity;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GUI;
using VRage.Game;
using ProtoBuf;
using Sandbox.Engine.Analytics;
using Sandbox.Game.Gui;
using IMyFunctionalBlock = Sandbox.ModAPI.Ingame.IMyFunctionalBlock;
using IMyShipGrinder = Sandbox.ModAPI.IMyShipGrinder;
using IMyShipWelder = Sandbox.ModAPI.Ingame.IMyShipWelder;
using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;

namespace ContestedZoneMod
{
    public class ContestedZones
    {
		[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
		public class ContestedZoneMod : MySessionComponentBase
        {
            private static bool debug = false;

			//..MODIFY CONTESTED ZONE PARAMETERS HERE
			string gps_center = "GPS:Zone:40500:-5330:14510:";
            private string gps_debug = "GPS:Contested Zone:54655:-2117:47040:";

            private static int ContestRange = 10000; //..range of Contested Zone (in metres)

			//..SET THE MESSAGES TO USE WHEN ENTERING CONTESTED ZONES OR USING SAFE ZONES WITHIN THEM
			private static string ContestMessage = "WARNING: ENTERING CONTESTED ZONE";
			private static string SafeZoneMessage = "Notice: SafeZones & Welders are forbidden in contested zones.";

            private static int ContestMessage_DisplayTime = 5000;
            private static int SafeZoneMessage_DisplayTime = 5000;

			//..Set up the contested zone properties
            private static Vector3D ContestOrigin;
            private static BoundingSphereD ContestZone;

			private readonly List<MyEntity> _knownEntities = new List<MyEntity>();
			private readonly List<IMyPlayer> _knownPlayers = new List<IMyPlayer>();
            private readonly List<IMyEntity> _knownBlacklistedEntities = new List<IMyEntity>();

			private static int timer = 0;
			//..SET HOW LONG YOU WANT TO WAIT BETWEEN UPDATES (in seconds)
			private static int TimerMax_EntityUpdate = 1;

            ushort packetId = 20201;

            //..This is necessary due to case-sensitivity of colors in UI messages
            //..Use this rather than writing "Red" as a string
            public enum Color
            {
                Red
            }

            //..Tell the client which message to display when pinged by the server
            public enum MessageType
            {
				Contest,
				Blacklist
            }

            //..Store some info about a server message, including how long it should appear for, what color it is and what message to display
            //..We'll also track when the message was last shown, so we don't spam players every time they trigger the message event
			public class ServerMessage
            {
                private int _timer = 0;
                private string _message { get; }
                private Color _color { get; }
                private int _displayTime { get;}

                public ServerMessage(string message,int displayTime, Color color)
                {
                    _message = message;
                    _color = color;
                    _displayTime = (displayTime / 1000);
                    _timer = _displayTime * 60;
                }

                public void UpdateTimer()
                {
                    if(_timer < (_displayTime * 60))
                        _timer++;
                }

                public void TryShow()
                {
                    if (_timer >= (_displayTime * 60))
                    {
                        MyAPIGateway.Utilities.ShowNotification(_message, _displayTime * 1000, _color.ToString());

                        _timer = 0;
                    }
                }
            }

            //..We'll store some server messages in a dictionary for easy access
            //..(This is unique to each client)
            private readonly Dictionary<MessageType, ServerMessage> _serverMessages = new Dictionary<MessageType, ServerMessage>()
            {
                {MessageType.Contest,new ServerMessage(ContestMessage,ContestMessage_DisplayTime,Color.Red)},
                {MessageType.Blacklist,new ServerMessage(SafeZoneMessage,SafeZoneMessage_DisplayTime,Color.Red)}
            };


			public override void LoadData()
            {
                //..we only need to set up the contested zone for the server
                if (MyAPIGateway.Session.IsServer)
                {
                    MyWaypointInfo temp_waypoint;

                    //..set up the contested zone
                    if (MyWaypointInfo.TryParse((!debug) ? gps_center : gps_debug, out temp_waypoint))
                    {
                        ContestOrigin = temp_waypoint.Coords;
                        ContestZone = new BoundingSphereD(ContestOrigin, ContestRange);
                    }
                }

                //..we only need to handle messages for clients. the server doesn't need to know
                if (!MyAPIGateway.Session.IsServer || debug)
                    MyAPIGateway.Multiplayer.RegisterMessageHandler(packetId, HandleServerMessage);
			}

            protected override void UnloadData()
            {
                //..we only need to handle messages for clients. the server doesn't need to know
                if (!MyAPIGateway.Session.IsServer || debug)
                    MyAPIGateway.Multiplayer.UnregisterMessageHandler(packetId, HandleServerMessage);
			}

			public override void UpdateBeforeSimulation()
			{
                if (MyAPIGateway.Session.IsServer || debug)
                {
                    //..cycle through all active safezones and disable them
                    //..also send a message to the player who activated the safezone
                    foreach (IMyFunctionalBlock blacklistBlock in _knownBlacklistedEntities)
                    {
                        if (blacklistBlock != null && blacklistBlock.Enabled)
                        {
                            blacklistBlock.Enabled = false;

                            List<IMyPlayer> NearbyPlayers = GetNearbyPlayersTo(blacklistBlock, 10);

                            //..send the same messages to all players within a 10m radius of the safezone*
                            //**This is a bit inefficient. We'll replace this once we find a way to 
                            foreach (IMyPlayer player in NearbyPlayers)
                            {
                                MyAPIGateway.Multiplayer.SendMessageTo(packetId,
                                    MyAPIGateway.Utilities.SerializeToBinary(MessageType.Blacklist), player.SteamUserId);
                            }
                        }
                    }

                    //..refresh the entities every second
                    if (timer % (TimerMax_EntityUpdate * 60) == 0)
                    {
                        //..we'll check the nearby entities again, because some players could have left
                        _knownEntities.Clear();
                        _knownBlacklistedEntities.Clear();

                        //..get all nearby entities
                        MyGamePruningStructure.GetAllEntitiesInSphere(ref ContestZone, _knownEntities);

                        foreach (MyEntity ent in _knownEntities)
                        {
                            //..get nearby safezones
                            if (ent is IMySafeZoneBlock || ent is IMyShipWelder)
                            {
                                _knownBlacklistedEntities.Add(ent);

                                continue;
                            }

                            //..get players who entered the safezone and send them a message 
                            //..players that are already inside won't get the same message again
                            if (ent is IMyCharacter)
                            {
                                IMyPlayer player = GetPlayerFromCharacter(ent as IMyCharacter);

                                if (player != null)
                                {
                                    float distance = Vector3.Distance(ent.WorldMatrix.Translation, ContestOrigin);

                                    //..if they're within the zone, send a message
                                    if (distance <= ContestRange && !_knownPlayers.Contains(player))
                                    {
                                        //ServerMessage msg = new ServerMessage(, MessageType.Contest);
                                        MyAPIGateway.Multiplayer.SendMessageTo(packetId,
                                            MyAPIGateway.Utilities.SerializeToBinary(MessageType.Contest),
                                            player.SteamUserId);

                                        _knownPlayers.Add(player);
                                    }
                                }
                            }
                        }

                        //..cycle through all "known" players and check if they're still inside
                        for (int i = 0; i < _knownPlayers.Count(); ++i)
                        {
                            IMyPlayer player = _knownPlayers[i];

                            if (player != null && player.Character != null)
                            {
                                float distance = Vector3.Distance(player.Character.WorldMatrix.Translation,
                                    ContestOrigin);

                                //..if the player has left the safezone, we'll forget about them
                                //..next time they return, they'll be shown the warning message again
                                if (distance > ContestRange)
                                {
                                    _knownPlayers.Remove(player);
                                }
                            }
                            else
                            {
                                _knownPlayers.Remove(player);
                            }
                        }
                    }
                }
                
                //..only run this on the client
                if (MyAPIGateway.Session.OnlineMode != MyOnlineModeEnum.OFFLINE || debug)
                {
                    //..allow the server messages to be shown again after some time
                    //..applies to entering contested zones and activating safe zones
                    _serverMessages[MessageType.Contest].UpdateTimer();
                    _serverMessages[MessageType.Blacklist].UpdateTimer();
                }
                
				timer += 1;
			}

			/// <summary>
			/// Messages sent from the server which display a message in the HUD (Clientside)
			/// </summary>
			/// <param name="data"></param>
			private void HandleServerMessage(byte[] data)
            {
				MessageType msgType = MyAPIGateway.Utilities.SerializeFromBinary<MessageType>(data);

				switch (msgType)
                {
					case MessageType.Contest:
                        _serverMessages[MessageType.Contest].TryShow();
						break;
                    case MessageType.Blacklist:
                        _serverMessages[MessageType.Blacklist].TryShow();
                        break;
                }
				
			}

			#region Utility Methods

			/// <summary>
			/// Get the players associated with a list of entities
			/// </summary>
			/// <param name="entities"></param>
			/// <returns></returns>
			private List<IMyPlayer> GetPlayersFromEntities(IEnumerable<IMyEntity> entities)
            {
                IEnumerable<IMyCharacter> characters = entities.Where(e => e is IMyCharacter).Cast<IMyCharacter>();

				List<IMyPlayer> players = new List<IMyPlayer>();

                foreach (IMyCharacter character in characters.Where(ch => ch.IsPlayer))
                {
					players.Add(GetPlayerFromCharacter((character)));
                }

                return players;
            }

            private List<IMyPlayer> GetServerPlayers()
            {
                List<IMyPlayer> serverPlayers = new List<IMyPlayer>();
                MyAPIGateway.Multiplayer.Players.GetPlayers(serverPlayers);

                return serverPlayers;
            }

			/// <summary>
			/// Get the player associated with a character entity
			/// </summary>
			/// <param name="character"></param>
			/// <returns></returns>
            private IMyPlayer GetPlayerFromCharacter(IMyCharacter character)
            {
                return GetServerPlayers().FirstOrDefault(p => p.Character == character);
            }

			/// <summary>
			/// Get nearby players to [entity] within [range] metres
			/// </summary>
			/// <param name="entity"></param>
			/// <param name="range"></param>
			/// <returns></returns>
            private List<IMyPlayer> GetNearbyPlayersTo(IMyFunctionalBlock entity, int range)
            {
                List<MyEntity> NearbyEntities = new List<MyEntity>();
                BoundingSphereD nearbySphere = new BoundingSphereD(entity.GetPosition(), range);

                MyGamePruningStructure.GetAllEntitiesInSphere(ref nearbySphere, NearbyEntities);

                return GetPlayersFromEntities(NearbyEntities);
            }
			#endregion
		}
	}
}
