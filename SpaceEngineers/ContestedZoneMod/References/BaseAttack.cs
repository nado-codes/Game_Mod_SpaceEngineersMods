using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;
using ProtoBuf;
using Sandbox.Game.Entities;
using VRage.Game.Entity;

namespace Klime.BaseAttack
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class BaseAttack : MySessionComponentBase
    {
        List<Attack> all_attacks = new List<Attack>();
        List<Attack> clear_attacks = new List<Attack>();
        Attack temp_attack;

        BoundingSphereD reuse_sphere = new BoundingSphereD();
        List<MyEntity> reuse_ents = new List<MyEntity>();
        IMySafeZoneBlock reuse_safezone;
        int seconds_timer = 0;
        int frames_timer = 0;
        bool waiting_accept = false;
        ushort netId = 29912;

        [ProtoInclude(1000, typeof(Attack))]
        [ProtoInclude(2000, typeof(HelpRequest))]
        [ProtoContract]
        public abstract class PacketBase
        {
            public PacketBase()
            {

            }
        }

        [ProtoContract]
        public class Attack : PacketBase
        {
            [ProtoMember(1)]
            public Vector3D position;
            [ProtoMember(2)]
            public float radius;
            [ProtoMember(3)]
            public int current_time;
            [ProtoMember(4)]
            public int max_time;
            [ProtoMember(5)]
            public string tag;

            public Attack()
            {

            }

            public Attack(Vector3D position, float radius, int current_time, int max_time, string tag)
            {
                this.position = position;
                this.radius = radius;
                this.current_time = current_time;
                this.max_time = max_time;
                this.tag = tag;
            }
        }

        [ProtoContract]
        public class HelpRequest : PacketBase
        {
            [ProtoMember(6)]
            public int request_id;
            [ProtoMember(7)]
            public ulong sender_id;
            [ProtoMember(8)]
            public string help_info;

            public HelpRequest()
            {

            }

            public HelpRequest(int request_id, ulong sender_id)
            {
                this.request_id = request_id;
                this.sender_id = sender_id;
            }
        }


        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            MyAPIGateway.Utilities.MessageEntered += Utilities_MessageEntered;
            MyAPIGateway.Multiplayer.RegisterMessageHandler(netId, handler);
        }

        private void handler(byte[] obj)
        {
            try
            {
                PacketBase new_packet = MyAPIGateway.Utilities.SerializeFromBinary<PacketBase>(obj);
                if (new_packet != null)
                {
                    if (MyAPIGateway.Session.IsServer)
                    {
                        if (new_packet is HelpRequest)
                        {
                            HelpRequest new_help = new_packet as HelpRequest;
                            if (new_help.request_id == 0)
                            {
                                HandlerHelpRequest(new_help);
                            }
                        }
                        if (new_packet is Attack)
                        {
                            var new_attack = new_packet as Attack;
                            all_attacks.Add(new_attack);
                        }
                    }
                    if (new_packet is HelpRequest)
                    {
                        HelpRequest new_help = new_packet as HelpRequest;
                        if (new_help.sender_id == MyAPIGateway.Multiplayer.MyId)
                        {
                            if (new_help.request_id == 1)
                            {
                                if (!string.IsNullOrEmpty(new_help.help_info))
                                {
                                    MyAPIGateway.Utilities.ShowMessage("", new_help.help_info);
                                }
                            }
                        }
                    }
                } 
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowMessage("", e.Message);
            }
        }

        private void HandlerHelpRequest(HelpRequest request)
        {
            string send_back_string = "Base Attacks Active: " + "\n\n";
            foreach (var attack in all_attacks)
            {
                int remaining_time = attack.max_time - attack.current_time;
                send_back_string += "       - " + attack.tag + "   " + remaining_time + "s" + "\n";
            }
            HelpRequest send_back_request = new HelpRequest(1, request.sender_id);
            send_back_request.help_info = send_back_string;
            MyAPIGateway.Multiplayer.SendMessageTo(netId, MyAPIGateway.Utilities.SerializeToBinary<HelpRequest>(send_back_request), send_back_request.sender_id);
        }

        private void Utilities_MessageEntered(string messageText, ref bool sendToOthers)
        {
            try
            {
                if (waiting_accept)
                {
                    sendToOthers = false;
                    if (temp_attack != null)
                    {
                        var lower_text = messageText.ToLower();
                        if (lower_text == "yes" || lower_text == "y")
                        {
                            MyAPIGateway.Multiplayer.SendMessageToServer(netId, MyAPIGateway.Utilities.SerializeToBinary<Attack>(temp_attack));
                            MyAPIGateway.Utilities.ShowMessage("", "Base Attack ACTIVATED");
                            waiting_accept = false;
                            temp_attack = null;
                        }
                        else if (lower_text == "no" || lower_text == "n")
                        {
                            MyAPIGateway.Utilities.ShowMessage("", "Base Attack Cancelled");
                            waiting_accept = false;
                            temp_attack = null;
                        }
                        else
                        {
                            MyAPIGateway.Utilities.ShowMessage("", "Invalid Response: Must be Y or N");
                        }
                    }
                }
                else
                {
                    if (messageText.StartsWith("/baseattack"))
                    {
                        temp_attack = null;
                        sendToOthers = false;

                        string help_message = "Base Attack Valid Commands:" + "\n\n";
                        help_message += "/baseattack GPS RADIUS DURATION" + "\n";
                        help_message += "GPS cannot contain spaces. Duration in seconds" + "\n\n";
                        help_message += "/baseattack time" + "\n";
                        help_message += "Reports the remaining time of any active base attacks";

                        var command = messageText.Replace("/baseattack ", "");
                        if (command.StartsWith("GPS"))
                        {
                            if (MyAPIGateway.Session.Player != null
                                && (MyAPIGateway.Session.Player.PromoteLevel == MyPromoteLevel.Admin
                                || MyAPIGateway.Session.Player.PromoteLevel == MyPromoteLevel.Owner))
                            {
                                var words = command.Split(' ').ToList();
                                if (words != null && words.Count == 3)
                                {
                                    MyWaypointInfo waypoint = new MyWaypointInfo("", Vector3D.Zero);
                                    float temp_radius = 0f;
                                    int temp_max_time = 0;
                                    MyWaypointInfo.TryParse(words[0], out waypoint);
                                    float.TryParse(words[1], out temp_radius);
                                    int.TryParse(words[2], out temp_max_time);

                                    if (waypoint.Coords != Vector3D.Zero && temp_radius != 0f && temp_max_time != 0)
                                    {
                                        string confirm_message = "Base Attack:\n\n";

                                        confirm_message += "NAME: " + waypoint.Name + "\n";
                                        confirm_message += "POSITION: " + waypoint.Coords.ToString() + "\n";
                                        confirm_message += "RADIUS: " + temp_radius.ToString() + "m" + "\n";
                                        confirm_message += "DURATION: " + temp_max_time.ToString() + "s" + "\n\n";
                                        
                                        confirm_message += "Confirm attack? [Y/N]";
                                        MyAPIGateway.Utilities.ShowMessage("", confirm_message);

                                        temp_attack = new Attack(waypoint.Coords, temp_radius, seconds_timer, seconds_timer + temp_max_time, waypoint.Name);
                                        waiting_accept = true;
                                    }
                                    else
                                    {
                                        MyAPIGateway.Utilities.ShowMessage("", "ERROR: Invalid syntax\n" + help_message);
                                        return;
                                    }
                                }
                                else
                                {
                                    MyAPIGateway.Utilities.ShowMessage("", "ERROR: Invalid syntax\n" + help_message);
                                    return;
                                }
                            }
                            else
                            {
                                MyAPIGateway.Utilities.ShowMessage("", "ERROR: You must be an admin to use this command");
                            }
                        }
                        else if (command.ToLower() == "time")
                        {
                            HelpRequest new_help_request = new HelpRequest(0,MyAPIGateway.Multiplayer.MyId);
                            MyAPIGateway.Multiplayer.SendMessageToServer(netId, MyAPIGateway.Utilities.SerializeToBinary<HelpRequest>(new_help_request));
                        }
                        else if (command == "help" || string.IsNullOrEmpty(command))
                        {
                            MyAPIGateway.Utilities.ShowMessage("", help_message);
                        }
                        else
                        {
                            MyAPIGateway.Utilities.ShowMessage("", help_message);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowMessage("",e.Message);
            }
            
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                if (MyAPIGateway.Session == null || !MyAPIGateway.Session.IsServer)
                {
                    return;
                }
                if (frames_timer % 60 == 0)
                {
                    clear_attacks.Clear();
                    foreach (var attack in all_attacks)
                    {
                        if (attack.current_time < attack.max_time)
                        {
                            attack.current_time += 1;
                            reuse_sphere = new BoundingSphereD(attack.position, attack.radius);
                            MyGamePruningStructure.GetAllEntitiesInSphere(ref reuse_sphere, reuse_ents);
                            foreach (var ent in reuse_ents)
                            {
                                reuse_safezone = ent as IMySafeZoneBlock;
                                if (reuse_safezone != null)
                                {
                                    reuse_safezone.Enabled = false;
                                }
                            }
                        }
                        else
                        {
                            reuse_sphere = new BoundingSphereD(attack.position, attack.radius);
                            MyGamePruningStructure.GetAllEntitiesInSphere(ref reuse_sphere, reuse_ents);
                            foreach (var ent in reuse_ents)
                            {
                                reuse_safezone = ent as IMySafeZoneBlock;
                                if (reuse_safezone != null)
                                {
                                    reuse_safezone.Enabled = true;
                                }
                            }
                            clear_attacks.Add(attack);
                        }
                    }

                    foreach (var clearAttack in clear_attacks)
                    {
                        all_attacks.Remove(clearAttack);
                    }
                    seconds_timer += 1;
                }
                frames_timer += 1;
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowMessage("", e.Message);
            }         
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.MessageEntered -= Utilities_MessageEntered;
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(netId, handler);
        }
    }
}