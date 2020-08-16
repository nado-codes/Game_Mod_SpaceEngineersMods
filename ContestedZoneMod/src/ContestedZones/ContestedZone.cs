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

namespace ND.ContestedZones
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class ContestedZone
    {
        private string _name => _gpsCenter.Split(':')[1];
        private string _gpsCenter;
        private Vector3D _contestOrigin;
        private BoundingSphereD _contestSphere;
        private int _contestRange;

        private readonly List<MyEntity> _knownEntities = new List<MyEntity>();
        private readonly List<IMyPlayer> _knownPlayers = new List<IMyPlayer>();
        private readonly List<IMyEntity> _knownBlacklistedEntities = new List<IMyEntity>();

        public ContestedZone(string gpsCenter, int contestRange)
        {
            _gpsCenter = gpsCenter;
            _contestRange = contestRange;

            MyWaypointInfo temp_waypoint;

            //..set up the contested zone
            if (MyWaypointInfo.TryParse(_gpsCenter, out temp_waypoint))
            {
                _contestOrigin = temp_waypoint.Coords;
                _contestSphere = new BoundingSphereD(_contestOrigin, _contestRange);
            }
        }

        public void Update(int timer)
        {
            if (MyAPIGateway.Session.IsServer || SessionManager.Debug)
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
                            MyAPIGateway.Multiplayer.SendMessageTo(SessionManager.PACKET_ID,
                                MyAPIGateway.Utilities.SerializeToBinary(SessionManager.MessageType.Blacklist), player.SteamUserId);
                        }
                    }
                }

                //..refresh the entities every second
                if (timer % (SessionManager.TIMER_MAX_ENTITY_UPDATE * 60) == 0)
                {
                    //..we'll check the nearby entities again, because some players could have left
                    _knownEntities.Clear();
                    _knownBlacklistedEntities.Clear();

                    //..get all nearby entities
                    MyGamePruningStructure.GetAllEntitiesInSphere(ref _contestSphere, _knownEntities);

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
                                float distance = Vector3.Distance(ent.WorldMatrix.Translation, _contestOrigin);

                                //..if they're within the zone, send a message
                                if (distance <= _contestRange && !_knownPlayers.Contains(player))
                                {
                                    //ServerMessage msg = new ServerMessage(, MessageType.Contest);
                                    MyAPIGateway.Multiplayer.SendMessageTo(SessionManager.PACKET_ID,
                                        MyAPIGateway.Utilities.SerializeToBinary(SessionManager.MessageType.Contest),
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
                                _contestOrigin);

                            //..if the player has left the safezone, we'll forget about them
                            //..next time they return, they'll be shown the warning message again
                            if (distance > _contestRange)
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
