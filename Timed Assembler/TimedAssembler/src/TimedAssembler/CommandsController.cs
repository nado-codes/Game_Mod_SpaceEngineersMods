using System;
using ProtoBuf;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Nado.Logs;
using Nado.TimedBlocks;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Nado.Commands
{
    public class CommandsController
    {
        private static CommandsController _singleton;
        private static bool _debug = false;
        private static bool _debugIsAdmin = false;

        #region Static
        protected readonly Dictionary<string, CommandDefinition> _commands = new Dictionary<string, CommandDefinition>();
        private string _unknownCommandMSG = "";
        private static Command _waitingCommand;
        private static string[] _waitingParams;

        private const ushort MSG_CMD = 3320;

        public static void Init(bool debug = false, bool debugIsAdmin = true)
        {
            CheckUnload();

            _singleton = new CommandsController();
            _debug = debug;
            _debugIsAdmin = debugIsAdmin;

            if (!MyAPIGateway.Session.IsServer || _debug)
                MyAPIGateway.Utilities.MessageEntered += MessageHandler; //client listen to chat messages
            else
                MyAPIGateway.Utilities.RegisterMessageHandler(MSG_CMD, ServerMessageHandler);
        }

        public static void CheckUnload()
        {
            _singleton?._commands.Clear();
            _singleton = null;

            if(MyAPIGateway.Utilities != null)
                MyAPIGateway.Utilities.MessageEntered -= MessageHandler;
        }

        public static void CreateCommand(string cmd, CmdAction cb, bool requireConfirm = false, bool isAdminOnly = false)
        {
            if(!_singleton._commands.ContainsKey(cmd.ToLower()))
                _singleton._commands.Add(cmd.ToLower(), new CommandDefinition(cmd.ToLower(),cb,requireConfirm,isAdminOnly));
        }

        public static void SetUnknownCommandMessage(string msg)
        {
            if(_singleton != null)
                _singleton._unknownCommandMSG = msg;
        }

        public static int GetCommandCount()
        {
            return _singleton?._commands.Count() ?? 0;
        }

        private static void ProcessCommand(CommandDefinition cmdDef, string[] cmdParams = null)
        {
            if (_debug)
            {
                Log.Write("Processing command \"" + cmdDef.CmdString + "\"" +
                          (cmdParams != null ? " with params: " : ""));

                if (cmdParams != null)
                {
                    Log.WriteList(cmdParams);
                }
            }

            cmdDef.Callback?.Invoke(cmdParams);
        }

        //RUN ON SERVER
        private static void ServerMessageHandler(object data)
        {
            Log.Write("Received message from client");

            var convertedData = data as byte[];
            Command receivedCommand = MyAPIGateway.Utilities.SerializeFromBinary<Command>(convertedData);

            if (receivedCommand != null)
            {
                CommandDefinition cmdDef = _singleton._commands[receivedCommand.CmdString];
                ProcessCommand(cmdDef, receivedCommand.Params);

                Log.Write("Processed command");
            }
            else
            {
                Log.Write("received a message from the client");
            }
        }

        //RUN ON CLIENT
        private static void MessageHandler(string msg, ref bool sendToOthers)
        {
            Command cmd = Command.TryParse(msg);

            

            if (cmd != null)
            {
                sendToOthers = false;

                if (_debug)
                    Log.Write("Received command \"" + cmd.CmdString + "\"" + (cmd.Params != null ? " with params: " : ""));

                if (cmd.Params != null)
                {
                    Log.WriteList(cmd.Params);
                }

                if (_singleton._commands.ContainsKey(cmd.CmdString))
                {
                    CommandDefinition cmdDef = _singleton._commands[cmd.CmdString];

                    if (cmdDef.IsAdminOnly && !IsAdmin(MyAPIGateway.Session.Player))
                    {
                        Log.Write("You need to be an admin to do that");
                        return;
                    }

                    if (!cmdDef.RequireConfirm)
                    {
                        //ProcessCommand(cmdDef, cmd.Params); // PROCESS
                        byte[] cmdData = MyAPIGateway.Utilities.SerializeToBinary(cmd);
                        MyAPIGateway.Multiplayer.SendMessageToServer(MSG_CMD, cmdData);
                    }
                    else
                    {
                        Log.Write("Are you sure? (Enter /y or /n)");

                        _waitingCommand = cmd;
                    }
                }
                else
                {
                    switch (cmd.CmdString)
                    {
                        case "y":
                            //ProcessCommand(_waitingCommand, _waitingParams); // PROCESS
                            byte[] cmdData = MyAPIGateway.Utilities.SerializeToBinary(_waitingCommand);
                            MyAPIGateway.Multiplayer.SendMessageToServer(MSG_CMD, cmdData);
                            break;
                        case "n":
                            Log.Write("Cancelled last action");
                            break;
                        default:
                            Log.Write("Unknown command \"" + cmd.CmdString + "\" " + _singleton._unknownCommandMSG);
                            break;
                    }
                }
            }
            else
            {
                Log.Write("nothing happened");
                sendToOthers = true;
            }
        }

        private static bool IsAdmin(IMyPlayer player)
        {
            if (player != null)
            {
                switch (player.PromoteLevel)
                {
                    case MyPromoteLevel.Admin:
                    case MyPromoteLevel.Owner:
                        return true;
                    default:
                        return false;
                }
            }
            else if (_debug) //..If we're debugging (e.g. Unit Tests), we'll return whether "Debug admin" is enabled
                return _debugIsAdmin;
            else
                return false;
        }

        private static bool IsPlayer(IMySession session)
        {
            if (session?.Player != null || session?.OnlineMode == MyOnlineModeEnum.PRIVATE || session?.OnlineMode == MyOnlineModeEnum.OFFLINE)
                return true;
            
            return false;
        }
        #endregion

        #region DEBUG METHODS
        public static void DEBUG_HandleMessage(string msg, ref bool sendToOthers)
        {
            MessageHandler(msg, ref sendToOthers);
        }

        #endregion
    }

    public delegate void CmdAction(string[] cmdParams);

    public struct CommandDefinition
    {
        public readonly string CmdString;
        public readonly CmdAction Callback;
        public readonly bool RequireConfirm;
        public readonly bool IsAdminOnly;

        public CommandDefinition(string cmdString, CmdAction cb, bool requireConfirm = false, bool isAdminOnly = false)
        {
            CmdString = cmdString;
            Callback = cb;
            RequireConfirm = requireConfirm;
            IsAdminOnly = isAdminOnly;
        }
    }

    [ProtoContract]
    public class Command
    {
        [ProtoMember(1)]
        public readonly string CmdString;

        [ProtoMember(2)]
        public readonly string[] Params;
    
        public Command(string cmd, string[] cmdParams, bool requireConfirm = false)
        {
            CmdString = cmd.ToLower();
    
            if (cmdParams != null && cmdParams.Length > 0)
            {
                Params = new string[cmdParams.Length];
    
                for (int i = 0; i < cmdParams.Length; ++i)
                    Params[i] = cmdParams[i];
            }
        }

        public static Command TryParse(string str)
        {
            if (str.StartsWith("/") && str.Length > 1)
            {
                string cmdString = str.Split('/')[1];
                int paramStart = cmdString.IndexOf(' ');
                bool hasParams = (paramStart != -1);
                string cmd = cmdString.Substring(0,
                    hasParams ? paramStart : cmdString.Length).ToLower();
                string[] cmdParams = (hasParams
                    ? cmdString.Substring(paramStart+1, cmdString.Length - paramStart - 1).Split(' ')
                    : null);
    
                return new Command(cmd, cmdParams);
            }
            else
            {
                return null;
            } 
        }
    }
}
