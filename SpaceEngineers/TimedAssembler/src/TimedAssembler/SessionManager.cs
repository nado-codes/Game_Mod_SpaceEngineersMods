using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using VRage.Game.Components;
using Nado.Commands;
using Nado.Logs;
using Nado.TimedBlocks;
using Sandbox.Engine.Physics;
using Sandbox.ModAPI;
using SpaceEngineers.Game.Entities.Blocks;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;
using VRage.ModAPI;

namespace Nado.TimedBlocks
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class SessionManager : MySessionComponentBase
    {
        private static bool _debug = true;

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
            _timedBlockControllers.Add(new TimedBlockController(_timedBlockControllers.Count,true));

            CommandsController.Init();
            CommandsController.SetUnknownCommandMessage("Type "+cmdPrefix+cmdModID+"Help for a list of available commands");

            CommandsController.CreateCommand(cmdModID+"Help", (cmdParams) =>
            {
                Cmd_Help();
            });

            #region Block Commands
            CommandsController.CreateCommand("tbAddBlock", (cmdParams) =>
            {
                Cmd_AddBlock();
            }, false,true);

            CommandsController.CreateCommand("tbUndoBlock", (cmdParams) =>
            {
                Cmd_UndoBlock();
            }, false, true);

            CommandsController.CreateCommand("tbListBlocks", (cmdParams) =>
            {
                Cmd_ListBlocks();
            },false,true);

            CommandsController.CreateCommand("tbClearBlocks", (cmdParams) =>
            {
                Cmd_ClearBlocks();
            }, true, true);
            #endregion

            #region Timed Block Commands
            CommandsController.CreateCommand("tbSetTimes", (cmdParams) =>
            {
                Cmd_SetTimes(cmdParams);
            }, false, true);

            CommandsController.CreateCommand("tbListTimes", (cmdParams) =>
            {
                Cmd_ListTimes();
            }, false, true);

            CommandsController.CreateCommand("tbClearTimes", (cmdParams) =>
            {
                Cmd_ClearTimes();
            }, true, true);

            CommandsController.CreateCommand("tbGetNextTime", (cmdParams) =>
            {
                Cmd_GetNextTime();
            }, false,true);

            CommandsController.CreateCommand("tbStatus", (cmdParams) =>
            {
                Cmd_GetStatus();
            }, false, true);

            CommandsController.CreateCommand("tbEnable", (cmdParams) =>
            {
                Cmd_Enable();
            });

            CommandsController.CreateCommand("tbDisable", (cmdParams) =>
            {
                Cmd_Disable();
            });

            CommandsController.CreateCommand("tbSetActiveMessage", (cmdParams) =>
            {
                Log.Write("That command hasn't been implemented yet!");
            }, false, true);

            CommandsController.CreateCommand("tbSetInactiveMessage", (cmdParams) =>
            {
                Log.Write("That command hasn't been implemented yet!");
            }, false, true);
            #endregion

            /*if(MyAPIGateway.Multiplayer.IsServer || debug)
                MyAPIGateway.Multiplayer.RegisterMessageHandler(adminMsg, HandleAdminCommand);

            if(!MyAPIGateway.Multiplayer.IsServer || debug)
                MyAPIGateway.Multiplayer.RegisterMessageHandler(serverMsg, HandleServerMessage);*/
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
                Log.Write("Invalid time pairs. Enter new times and try again");
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
                        Log.Write("Time values may only be integers (e.g. 1100,1300). Enter new times and try again");
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
                    _timedBlockControllers[0].SaveChanges();
                }

                Log.Write("New times set: "+ newTimeString);
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
            UpdateServerside();

            _timer++;
        }

        private static void UpdateServerside()
        {
            if (MyAPIGateway.Session.IsServer || _debug)
            {
                foreach (TimedBlockController controller in _timedBlockControllers)
                    controller.Update(_timer);
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
    }
}
