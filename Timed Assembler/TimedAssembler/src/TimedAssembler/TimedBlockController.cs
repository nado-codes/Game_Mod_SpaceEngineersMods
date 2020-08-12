using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sandbox.ModAPI;
using Nado.Logs;
using BlockID_Type = System.Int64;
using VRage.Game.ModAPI;
using VRage.Game;
using TimedAssembler.IO;
using VRage.Game.ModAPI.Ingame;

//using TimedAssembler.Emulators;

namespace Nado.TimedBlocks
{
    public class TimespanException : Exception { }

    public struct BlockIdentifier
    {
        public BlockID_Type GridID { get; set; }
        public BlockID_Type BlockID { get; set; }

        public BlockIdentifier(BlockID_Type gridId, BlockID_Type blockId)
        {
            GridID = gridId;
            BlockID = blockId;
        }
    }

    public class TimedBlockConfig
    {
        public List<TimePair> Times { get; set; }
        public List<BlockIdentifier> Blocks { get; set; }

        public TimedBlockConfig() { } //Parameterless constructor used for XML serialisation

        public TimedBlockConfig(List<TimePair> times, List<BlockIdentifier> blockIds)
        {
            Times = new List<TimePair>();
            Blocks = new List<BlockIdentifier>();

            foreach(TimePair pair in times)
                Times.Add(new TimePair(pair.StartHour, pair.FinishHour));

            foreach (BlockIdentifier BlockID in blockIds)
                Blocks.Add(BlockID);
        }
    }

    public class TimePair
    {
        public int StartHour;
        public int FinishHour;

        private TimePair() { } //Parameterless constructor used for XML serialisation

        public TimePair(int startHour, int finishHour)
        {
            StartHour = startHour;
            FinishHour = finishHour;
        }

        public override string ToString()
        {
            return "[" + StartHour + "-" + FinishHour + "]";
        }
    }

    public delegate void VoidFN();

    public class TimedBlockController
    {
        public string FILENAME_CFG => "TB_" + Id;

        private bool _testing = true;
        public int Id { get; private set; }

        List<TimePair> _todayTimes = new List<TimePair>();
        //private List<TimePair> _tomorrowTimes = new List<TimePair>();
        private int debugHour = -1;

        private bool _isActive = false;
        public TimePair nextBlock;
        private TimePair currentBlock;

        private const string activeMessage = "Timed blocks are active!";
        private const string inactiveMessage = "Timed blocks are inactive!";

        private List<IMyFunctionalBlock> _blocks = new List<IMyFunctionalBlock>();

        public TimedBlockController(int id, bool testing = false)
        {
            _testing = testing;
            Id = id;

            //..load the config file for this timed block controller (if there is one)
            //..if the config file is null, don't do anything
            TimedBlockConfig cfg = FileController.LoadFile<TimedBlockConfig>(FILENAME_CFG);

            if(cfg != null)
                LoadConfig(cfg);
        }

        #region Public Block Methods

        #region Setters
        public void AddBlock(IMyFunctionalBlock block)
        {
            if (!_blocks.Contains(block))
            {
                block.EnabledChanged += AssertValidActive;
                _blocks.Add(block);
            }
        }

        public void AddBlocksFromIds(long[] blockIds)
        {
            foreach (long blockId in blockIds)
            {
                VRage.ModAPI.IMyEntity ent = MyAPIGateway.Entities.GetEntityById(blockId);

                if (ent == null)
                    Log.Write("problem finding entity with Id");

                IMyFunctionalBlock block = ent as IMyFunctionalBlock;

                if (block == null)
                    Log.Write("the block isn't a functional one");

                if (block != null && !_blocks.Contains(block))
                {
                    block.EnabledChanged += AssertValidActive;
                    _blocks.Add(ent as IMyFunctionalBlock);
                    Log.Write("added block successfully!");
                }
                else
                {
                    Log.Write("the block is already there");
                }
            }
        }

        public void RemoveBlock(IMyFunctionalBlock block)
        {
            block.EnabledChanged -= AssertValidActive;
            _blocks.Remove(block);
        }

        public void ClearBlocks()
        {
            for (int i = 0; i < _blocks.Count; i++)
            {
                RemoveBlock(_blocks[i]);
            }
        }

        public void SetBlocksActive(bool isActive)
        {
            _isActive = isActive;

            foreach (IMyFunctionalBlock block in _blocks)
            {
                block.Enabled = isActive;
                Log.Write("Enabled " + block.GetType().FullName);
            }
        }
        #endregion

        #region Getters
        public List<IMyFunctionalBlock> GetBlocks()
        {
            return _blocks;
        }
        #endregion

        #endregion

        #region Public Time Methods

        public void AddTime(int startHour, int finishHour)
        {
            //..Make sure the start time is less than the finish time
            if(startHour >= finishHour)
                throw new TimespanException();
            else
            {
                TimePair newPair = new TimePair(startHour, finishHour);

                //..Only queue up the time block if the time hasn't already passed
                if (DateTime.Now.Hour <= finishHour)
                    _todayTimes.Add(newPair);
            }
        }

        public void ClearTimes()
        {
            _todayTimes.Clear();
            currentBlock = null;
            nextBlock = null;
        }

        public List<TimePair> GetTimes()
        {
            return _todayTimes;
        }

        public bool IsActive()
        {
            return _isActive;
        }

        public TimePair GetCurrentBlock()
        {
            return currentBlock;
        }

        public TimePair GetNextBlock()
        {
            return nextBlock;
        }

        public int GetNextActive()
        {
            if (nextBlock != null)
                return nextBlock.StartHour;
            else
                return -1;
        }

        public int GetNextInactive()
        {
            if (nextBlock != null)
                return nextBlock.StartHour;
            else
                return -1;
        }
        #endregion

        #region Public Utility Methods
        public TimedBlockConfig GetConfig()
        {
            //..create a temp list to store block Ids
            List<BlockIdentifier> temp = new List<BlockIdentifier>();

            foreach (IMyFunctionalBlock block in _blocks)
            {
                Log.Write("Grid: " + block.CubeGrid.EntityId);
                Log.Write("Block: " + block.EntityId);
                temp.Add(new BlockIdentifier(block.CubeGrid.EntityId, block.EntityId));
            }

            return new TimedBlockConfig(_todayTimes, temp);
        }

        public void LoadConfig(TimedBlockConfig cfg)
        {
            /*Dictionary<BlockID_Type, IMyGridTerminalSystem> gridTerminals = new Dictionary<BlockID_Type, IMyGridTerminalSystem>();

            //..Add all the blocks
            foreach (BlockIdentifier blockId in cfg.Blocks)
            {
                IMyCubeGrid grid = MyAPIGateway.Entities.GetEntityById(blockId.GridID) as IMyCubeGrid;

                Log.Write(" - Access grid: " + grid.EntityId + " (" + grid.DisplayName + ")");

                if (!gridTerminals.ContainsKey(grid.EntityId))
                    gridTerminals.Add(grid.EntityId, MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid));

                IMyGridTerminalSystem gts = gridTerminals[grid.EntityId];
                IMyFunctionalBlock block = gts.GetBlockWithId(blockId.BlockID) as IMyFunctionalBlock;

                Log.Write(" - Access terminal: " + gridTerminals[grid.EntityId]);
                Log.Write(" - Access block: " + gts.GetBlockWithId(blockId.BlockID).GetType().FullName);
                Log.Write(" - Access block (functional): " + (gts.GetBlockWithId(blockId.BlockID) as IMyFunctionalBlock).GetType().FullName);

                _blocks.Add(block);
            }

            //..Add all the times
            foreach(TimePair pair in cfg.Times)
            {
                _todayTimes.Add(pair);
            }*/
        }

        public void SaveChanges()
        {
            FileController.SaveFile(FILENAME_CFG, GetConfig());
        }

        public string GetBlockName(IMyFunctionalBlock block)
        {
            return block.GetType().Name + " called \"" + block.DisplayName + "\"";
        }

        public void Update(int timer)
        {
            
            //..only set the current/next block if times exist and current block is null. only check next block if the times count is > 1
            if (_todayTimes.Count > 0 && currentBlock == null)
            {
                //if (_testing)
                    //Log.Write("Current/Next not set, setting...");

                DateTime now = (debugHour == -1) ? DateTime.Now : new DateTime(2020, 1, 1, debugHour, 0, 0);

                //Log.Write(" - Hour is: " + now.Hour);

                //..get the next block from the current hour e.g. 1200
                currentBlock = _todayTimes.FirstOrDefault(b => (now.Hour*100) <= b.StartHour);

                //if (_testing && currentBlock != null)
                    //Log.Write(" - Current block is: "+currentBlock);

                //..get the next block after the current block e.g. 1200 (current) -> 1300 (next)
                nextBlock = (currentBlock != null) ? _todayTimes.FirstOrDefault(b => b.StartHour >= currentBlock.FinishHour) : null;

                if (nextBlock == null)
                    nextBlock = _todayTimes.FirstOrDefault();

                //if (_testing)
                    //Log.Write(" - Next block is: " + nextBlock);
            }

            if ((timer % (60 ^ 3) == 0))
            {
                if (_testing)
                {
                    //Log.Write("Today time count: " + _todayTimes.Count);
                    //Log.Write("Current block : " + (currentBlock == null ? "Null" : currentBlock.StartHour + " - " + currentBlock.FinishHour));
                    //Log.Write("Next block : " + (nextBlock == null ? "Null" : nextBlock.StartHour + " - " + nextBlock.FinishHour));
                }

                if (CanUpdate())
                    Update_Hour();
                //else if(_testing)
                //{
                    //Log.Write("The timed block controller was unable to update. No times are set");
                //}
            }
        }

        public void DebugSetHour(int dbHour)
        {
            debugHour = dbHour;
        }

        private void Update_Hour()
        {
            DateTime now = (debugHour == -1) ? DateTime.Now : new DateTime(2020, 1, 1, debugHour, 0, 0);

            //..Once we've reached the next block, we'll activate everything
            if (!_isActive && now.Hour >= currentBlock.StartHour / 100 && now.Hour < currentBlock.FinishHour / 100)
            {
                if (_testing)
                    Log.Write("- Controller is now active @" + currentBlock.StartHour);

                SetBlocksActive(true);
                
            }
            else if (_isActive && now.Hour >= currentBlock.FinishHour / 100
            ) //..Once we reach the end, let's turn everything off and select the next block
            {
                if (_testing)
                    Log.Write("- Controller is now inactive @" + currentBlock.FinishHour);

                SetBlocksActive(false);

                if (_testing)
                {
                    //Log.Write("  - Today Times are now: " + _todayTimes.Select(t => t.ToString()));
                    //Log.Write("  - Tomorrow Times are now:" + _tomorrowTimes.Select(t => t.ToString()));
                    Log.Write("Switched to the next block: " + nextBlock);
                }

                currentBlock = nextBlock;
                nextBlock = null;
            }
            else if (now.Hour >= currentBlock.FinishHour / 100)
            {
                currentBlock = nextBlock;
                nextBlock = null;
            }
        }

        private bool CanUpdate()
        {
            return (currentBlock != null && _todayTimes.Count > 0);
        }
        #endregion

        #region Private Block Methods

        

        private void AssertValidActive(IMyTerminalBlock block)
        {
            IMyFunctionalBlock test = block as IMyFunctionalBlock;;

            if (test != null)
            {
                test.Enabled = _isActive;
            }
        }
        #endregion
    }
}
