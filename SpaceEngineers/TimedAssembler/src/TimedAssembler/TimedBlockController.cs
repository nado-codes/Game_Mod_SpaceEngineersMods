using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sandbox.ModAPI;
using Nado.Logs;

namespace Nado.TimedBlocks
{
    public class TimespanException : Exception { }

    public class TimePair
    {
        public int StartHour;
        public int FinishHour;

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

    public class TimedBlockController
    {
        private bool _testing = true;

        List<TimePair> _todayTimes = new List<TimePair>();
        //private List<TimePair> _tomorrowTimes = new List<TimePair>();
        private int debugHour = -1;

        private bool _isActive = false;
        public TimePair nextBlock;
        private TimePair currentBlock;

        private const string activeMessage = "Timed blocks are active!";
        private const string inactiveMessage = "Timed blocks are inactive!";

        private List<IMyFunctionalBlock> _blocks = new List<IMyFunctionalBlock>();

        public TimedBlockController(bool testing = false)
        {
            _testing = testing;
        }

        #region Public Block Methods
        public void AddBlock(IMyFunctionalBlock block)
        {
            if (!_blocks.Contains(block))
            {
                block.EnabledChanged += AssertValidActive;
                _blocks.Add(block);
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

        public List<IMyFunctionalBlock> GetBlocks()
        {
            return _blocks;
        }
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
        }

        public List<TimePair> GetTimes()
        {
            return _todayTimes;
        }

        public bool IsActive()
        {
            return _isActive;
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
        public string GetBlockName(IMyFunctionalBlock block)
        {
            return block.GetType().Name + " called \"" + block.DisplayName + "\"";
        }

        public void Update(int timer)
        {
            
            //..only set the current/next block if times exist and current block is null. only check next block if the times count is > 1
            if (_todayTimes.Count > 0 && currentBlock == null)
            {
                if (_testing)
                    Log.Write("Current/Next not set, setting...");

                DateTime now = (debugHour == -1) ? DateTime.Now : new DateTime(2020, 1, 1, debugHour, 0, 0);

                Log.Write(" - Hour is: " + now.Hour);

                //..get the next block from the current hour e.g. 1200
                currentBlock = _todayTimes.FirstOrDefault(b => (now.Hour*100) <= b.StartHour);

                if (_testing && currentBlock != null)
                    Log.Write(" - Current block is: "+currentBlock);

                //..get the next block after the current block e.g. 1200 (current) -> 1300 (next)
                nextBlock = (currentBlock != null) ? _todayTimes.FirstOrDefault(b => b.StartHour >= currentBlock.FinishHour) : null;

                if (nextBlock == null)
                    nextBlock = _todayTimes.FirstOrDefault();

                if (_testing)
                    Log.Write(" - Next block is: " + nextBlock);
            }

            if ((timer % (60 ^ 3) == 0))
            {
                if (_testing)
                {
                    Log.Write("Today time count: " + _todayTimes.Count);
                    Log.Write("Current block : " + (currentBlock == null ? "Null" : currentBlock.StartHour + " - " + currentBlock.FinishHour));
                    Log.Write("Next block : " + (nextBlock == null ? "Null" : nextBlock.StartHour + " - " + nextBlock.FinishHour));
                }

                if (CanUpdate())
                    Update_Hour();
                else if(_testing)
                {
                    Log.Write("The timed block controller was unable to update. No times are set");
                }
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
                _isActive = true;
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

                _isActive = false;
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

        #region Private Block Active Methods
        private void SetBlocksActive(bool isActive)
        {
            foreach (IMyFunctionalBlock block in _blocks)
            {
                block.Enabled = isActive;
            }
        }

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
