using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Components;
using Nado.Commands;
using Nado.Logs;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;
using VRage.ModAPI;
using TimedAssembler.IO;

//using TimedAssembler.Emulators;

namespace Nado.TimedBlocks
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class SessionManager : MySessionComponentBase
    {
        private static bool _debug = false;
        private ushort SV_PING = 7821;

        //ADD BLOCK IDS HERE, SEPARATED BY COMMA
        private static readonly long[] BLOCK_IDS = new long[2] 
        {
            129231677836010872, //Assembler 
            132028052049562808 //Beacon
        };
        private bool _loadedBlocks = false;

        //ADD TIMES HERE. MUST BE IN PAIRS, AND SEPARATED BY WHITESPACE e.g. "0900 1000 1200 1300" has active blocks from 9am-10am, followed by 12pm-1pm.
        //TIMES ARE IN 24 HOURS
        private static readonly string BLOCK_TIMES = "0900 1000";

        private static List<TimedBlockController> _timedBlockControllers = new List<TimedBlockController>();
        private static int _timer = 0;

        public enum CommandId
        {
            AddBlock,UndoBlock,ListBlocks,ClearBlocks,
            SetTimes,ListTimes,ClearTimes,GetNextTime,
            Status,Enable,Disable, //DisableAuto
        }

        private static readonly string cmdPrefix = "/";
        private static readonly string cmdModID = "tb";

        private static readonly Dictionary<CommandId, string> _commandList = new Dictionary<CommandId, string>()
        {
            { CommandId.AddBlock, cmdModID+"AddBlock" }
        };


        public override void LoadData()
        {
            if (AllowedToRun())
            {
                _timedBlockControllers.Add(new TimedBlockController(_timedBlockControllers.Count, true));

                //..add blocks by Id, and add times

                Cmd_SetTimes(BLOCK_TIMES.Split(' '));
            }
            else
            {
                
            }

            CommandsController.Init();

            if (!MyAPIGateway.Multiplayer.IsServer)
            {
                CommandsController.CreateCommand("ping", (cmdParams) =>
                {
                    Log.Write("Sending ping to server");
                    byte[] msgData = MyAPIGateway.Utilities.SerializeToBinary("ping");
                    MyAPIGateway.Multiplayer.SendMessageToServer(Log.MSG_LOG, msgData, true);
                });

                MyAPIGateway.Multiplayer.RegisterMessageHandler(Log.MSG_LOG, ServerMessageHandler);
            }

            #region Block Commands


            CommandsController.CreateCommand("tbListBlocks", (cmdParams) => { Cmd_ListBlocks(); }, false, true);

            #endregion

            #region Timed Block Commands

            CommandsController.CreateCommand("tbListTimes", (cmdParams) => { Cmd_ListTimes(); }, false, true);

            CommandsController.CreateCommand("tbGetNextTime", (cmdParams) => { Cmd_GetNextTime(); }, false, true);

            CommandsController.CreateCommand("tbStatus", (cmdParams) => { Cmd_GetStatus(); }, false, true);

            CommandsController.CreateCommand("tbEnable", (cmdParams) => { Cmd_Enable(); });

            CommandsController.CreateCommand("tbDisable", (cmdParams) => { Cmd_Disable(); });

            #endregion
        }

        private void ServerMessageHandler(object data)
        {
            var convertedData = data as byte[];
            string serverMsg = MyAPIGateway.Utilities.SerializeFromBinary<string>(convertedData);

            Log.Write(serverMsg,false,"Server");
        }

        protected override void UnloadData()
        {
            CommandsController.CheckUnload();
        }

        private void Cmd_Help()
        {
            Log.Write("Here's a list of available commands: ");
            Log.Write("/tbSetTimes - Enter a list of times e.g. 1000 1100 1200 1300 = run from 10am-11am, then from 12am-1pm");
            Log.Write("/tbListTimes - List all the times stored");
        }

        private void Cmd_AddBlocksFromIDs()
        {
            _timedBlockControllers[0].AddBlocksFromIds(BLOCK_IDS);
        }

        private void Cmd_AddBlock()
        {
            IMyEntity ent = null;

            if (GetPlayer().Character != null && !GetPlayer().Character.IsDead)
            {
                IHitInfo hit;

                //TODO: Get the entity that the player is looking at

                Vector3 playerForward = GetPlayer().Character.WorldMatrix.Rotation.Forward;
                float distance = 50;

                MyAPIGateway.Physics.CastRay(GetPlayer().GetPosition(),
                    GetPlayer().GetPosition() + (playerForward * distance), out hit);

                if (hit != null && hit.HitEntity != null && hit.HitEntity is IMyCubeGrid)
                {
                    BoundingSphereD raySphere = new BoundingSphereD(hit.Position, 0.25);

                    List<IMyEntity> blocks = MyAPIGateway.Entities.GetEntitiesInSphere(ref raySphere);

                    ent = blocks.FirstOrDefault(b => (b as IMyFunctionalBlock) != null);
                }
            }
            else
            {
                Log.Write("You're not looking at anything");
            }

            if (ent != null)
            {
                if (_timedBlockControllers.Count == 1)
                {
                    if (!_timedBlockControllers[0].GetBlocks().Contains(ent))
                    {
                        Log.Write("Added block " + ent.GetType().FullName + " called \"" + ent.DisplayName + "\"");

                        _timedBlockControllers[0].AddBlock(ent as IMyFunctionalBlock);

                        _timedBlockControllers[0].SaveChanges();
                    }
                    else
                    {
                        Log.Write("The block " + ent.GetType().FullName + " called \"" + ent.DisplayName + "\" already exists!");
                    }
                }
            }
            else
            {
                Log.Write("You're not looking at anything");
            }
        }

        private void Cmd_UndoBlock()
        {
            if (_timedBlockControllers.Count == 1)
            {
                IMyFunctionalBlock lastBlock = _timedBlockControllers[0].GetBlocks().LastOrDefault();

                Log.Write("Removed block " + lastBlock.GetType().FullName + " called \"" + lastBlock.GetFriendlyName() + "\"");

                _timedBlockControllers[0].RemoveBlock(lastBlock);

                _timedBlockControllers[0].SaveChanges();
            }
        }

        private void Cmd_ListBlocks()
        {
            if (_timedBlockControllers.Count == 1)
            {
                TimedBlockController controller = _timedBlockControllers[0];

                if (_timedBlockControllers[0].GetBlocks().Count > 0)
                {
                    Log.Write("Timed Blocks: ");
                    Log.WriteList(controller.GetBlocks().Select(b => controller.GetBlockName(b)).ToArray());
                }
                else
                {
                    Log.Write("There are no timed blocks");
                }
            }
        }

        private void Cmd_ClearBlocks()
        {
            if (_timedBlockControllers.Count == 1)
            {
                Log.Write("Removed "+_timedBlockControllers[0].GetBlocks().Count+" blocks");
                _timedBlockControllers[0].ClearBlocks();

                _timedBlockControllers[0].SaveChanges();
            }
        }

        private void Cmd_SetTimes(string[] times)
        {
            bool valid = (int)(times.Length / 2) == Math.Round((float)times.Length / 2);

            if (!valid)
            {
                //Log.Write("Invalid time pairs. Enter new times and try again");
            }
            else
            {
                string newTimeString = "";

                for (int i = 0; i < times.Length / 2; i += 2)
                {
                    int startHour, finishHour;
                    valid = int.TryParse(times[i], out startHour);
                    valid = int.TryParse(times[i + 1], out finishHour);

                    if (!valid)
                    {
                        //Log.Write("Time values may only be integers (e.g. 1100,1300). Enter new times and try again");
                        return;
                    }

                    if (_timedBlockControllers.Count == 1)
                    {
                        _timedBlockControllers[0].AddTime(startHour,finishHour);
                        newTimeString += startHour + " " + finishHour + " ";
                    }
                }

                if(_timedBlockControllers.Count == 1)
                {
                    //_timedBlockControllers[0].SaveChanges();
                }

                //Log.Write("New times set: "+ newTimeString);
            }
        }

        private void Cmd_ListTimes()
        {
            if (_timedBlockControllers.Count == 1)
            {
                List<TimePair> tPairs = _timedBlockControllers[0].GetTimes();

                if (tPairs.Count > 0)
                {
                    string blockStr = "";

                    foreach (TimePair p in tPairs)
                        blockStr += p.ToString();

                    Log.Write("Times: " + blockStr);
                }
                else
                {
                    Log.Write("There are no times set. Use /tbSetTimes to set times");
                }
            }
        }

        private void Cmd_ClearTimes()
        {
            if (_timedBlockControllers.Count == 1)
            {
                if (_timedBlockControllers[0].GetTimes().Count > 0)
                {
                    Log.Write("Cleared (" + _timedBlockControllers[0].GetTimes().Count + ") time blocks");

                    Log.Write("The timed blocks are " + (_timedBlockControllers[0].IsActive() ? "active" : "inactive") 
                    + ", you'll need to enable/disable them manually until more times are set.");

                    Log.Write("Use /tbSetTimes to set new times");
                    _timedBlockControllers[0].ClearTimes();

                    _timedBlockControllers[0].SaveChanges();
                }
                else
                    Log.Write("There are no times set. Use /tbSetTimes to set times");
            }
        }

        private void Cmd_GetNextTime()
        {
            if (_timedBlockControllers.Count == 1)
            {
                Log.Write("The blocks will be enabled at " + _timedBlockControllers[0].GetNextActive());
            }
        }

        private void Cmd_GetStatus()
        {
            if (_timedBlockControllers.Count == 1)
            {
                Log.Write("Timed blocks are currently "+(_timedBlockControllers[0].IsActive() ? "active" : "inactive"));

                if (_timedBlockControllers[0].IsActive())
                {
                    Log.Write("They will be disabled at "+_timedBlockControllers[0].GetNextInactive());
                }
                else if (_timedBlockControllers[0].GetNextActive() != -1)
                {
                    Log.Write("They will be enabled at " + _timedBlockControllers[0].GetNextActive());
                }
                else
                {
                    Log.Write("There are no times set. Blocks can be enabled/disabled manually with /tbEnable or /tbDisable");
                }
            }
        }

        private void Cmd_Enable()
        {
            if (_timedBlockControllers.Count == 1)
            {
                if (!_timedBlockControllers[0].IsActive())
                {
                    Log.Write("Blocks enabled.");

                    if(_timedBlockControllers[0].GetTimes().Count == 0)
                    {
                        Log.Write("You'll need to disable them manually - no times are set.");
                    }
                    else
                    {
                        Log.Write("As there are times set, they will stay active until "+ _timedBlockControllers[0].GetCurrentBlock().FinishHour);
                    }

                    _timedBlockControllers[0].SetBlocksActive(true);
                }
                else
                {
                    Log.Write("Blocks are already active");
                }
            }
        }

        private void Cmd_Disable()
        {
            if (_timedBlockControllers.Count == 1)
            {
                if (_timedBlockControllers[0].IsActive())
                {
                    Log.Write("Blocks disabled.");

                    if (_timedBlockControllers[0].GetTimes().Count == 0)
                    {
                        Log.Write("You'll need to enable them manually - no times are set.");
                    }
                    else
                    {
                        Log.Write("As there are times set, they will stay inactive until " + _timedBlockControllers[0].GetCurrentBlock().StartHour);
                    }

                    _timedBlockControllers[0].SetBlocksActive(false);
                }
                else
                {
                    Log.Write("Blocks are already inactive");
                }
            }
        }


        public override void UpdateBeforeSimulation()
        {
            if (AllowedToRun())
            {
                if (!_loadedBlocks)
                {
                    _timedBlockControllers[0].AddBlocksFromIds(BLOCK_IDS);

                    Log.Write("Loading operation success");

                    _loadedBlocks = true;
                }

                foreach (TimedBlockController controller in _timedBlockControllers)
                    controller.Update(_timer);

                _timer++;
            }
        }

        private static IMyPlayer GetPlayer()
        {
            if (MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE || IsDebug())
                return MyAPIGateway.Session.LocalHumanPlayer;
            else
                return MyAPIGateway.Session.Player;
        }

        public static bool IsDebug()
        {
            return _debug;
        }

        private bool AllowedToRun()
        {
            return (MyAPIGateway.Session.IsServer /*|| _debug*/);
        }
    }
}
