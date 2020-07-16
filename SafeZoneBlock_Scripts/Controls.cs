using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using SpaceEngineers.Game.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;
using VRage.Game;

namespace SafeZoneBlockLogic
{

    public static class Controls
    {

        public static bool controlsCreated = false;
        public static bool isEnabled;

        public static void CreateControlsNew(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {

            if (block as IMySafeZoneBlock != null)
            {

                ControlCreation.CreateControls(block, controls);
            }
        }

        public static bool ControlVisibility(IMyTerminalBlock block)
        {

            if (block as IMySafeZoneBlock != null)
            {

                var safeZoneBlock = block as IMySafeZoneBlock;
                if (safeZoneBlock.BlockDefinition.SubtypeName.Contains("SafeZoneBlock"))
                {

                    return true;
                }

            }

            return false;
        }

        public static bool HideControls(IMyTerminalBlock block)
        {
            if (block as IMySafeZoneBlock != null)
            {
                var safeZoneBlock = block as IMySafeZoneBlock;
                if (safeZoneBlock.BlockDefinition.SubtypeName.Contains("SafeZoneBlock"))
                {

                    return false;
                }
            }

            return true;
        }

        public static bool CheckEnabled(IMyTerminalBlock Block)
        {
            if (SafeZoneCore.delayControls)
            {
                return isEnabled;
            }

            var safezoneBlock = Block as IMySafeZoneBlock;
            if (safezoneBlock == null)
            {
                return isEnabled;
            }

            bool toggleEnabled = safezoneBlock.IsSafeZoneEnabled();
            if (toggleEnabled)
            {
                return true;
            }

            if(SafeZoneCore.IntermodConfig != null)
            {
                if (SafeZoneCore.IntermodConfig.spawnStalinRoid)
                {
                    if (Vector3D.Distance(Block.GetPosition(), SafeZoneCore.IntermodConfig.stalinRoidGPS) <= SafeZoneCore.IntermodConfig.MaxSpawnCoverage + 2000) return false;
                }

                if (SafeZoneCore.IntermodConfig.enableWarpLocation)
                {
                    if (Vector3D.Distance(Block.GetPosition(), SafeZoneCore.IntermodConfig.warpGateGPS) <= 5000) return false;
                }
                
            }

            var grid = Block.CubeGrid;
            var cubeGrid = grid as MyCubeGrid;
            bool inVoxel = false;

            List<IMySlimBlock> blockList = new List<IMySlimBlock>();
            List<MyVoxelBase> m_tmpVoxelList = new List<MyVoxelBase>();
            grid.GetBlocks(blockList);
           // MyVisualScriptLogicProvider.ShowNotification("BlockList = " + blockList.Count, 20000, "Red");

            foreach (var block in blockList)
            {
                if (block.CubeGrid.Physics == null)
                {
                    return false;
                }

                BoundingBoxD boundingBoxD;
                block.GetWorldBoundingBox(out boundingBoxD, false);
                m_tmpVoxelList.Clear();
                MyGamePruningStructure.GetAllVoxelMapsInBox(ref boundingBoxD, m_tmpVoxelList);
                float gridSize = block.CubeGrid.GridSize;
                BoundingBoxD aabb = new BoundingBoxD((double)gridSize * (block.Min - (int)0.5), (double)gridSize * (block.Max + (int)0.5));
                MatrixD worldMatrix = block.CubeGrid.WorldMatrix;
                using (List<MyVoxelBase>.Enumerator enumerator = m_tmpVoxelList.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.IsAnyAabbCornerInside(ref worldMatrix, aabb))
                        {
                            inVoxel = true;
                            break;
                        }
                    }
                }
            }

            SafeZoneCore.delayControls = true;
            RefreshControls(Block);

            if (inVoxel)
            {
                if (!CheckEnemies(Block))
                {
                    isEnabled = true;
                    return true;
                }
            }

            //MyVisualScriptLogicProvider.ShowNotification("Control Disabled - Not in voxel", 10000, "Red");
            isEnabled = false;
            return false;
        }

        private static bool CheckEnemies(IMyTerminalBlock Block)
        {
            double radius = 1000;
            BoundingSphereD sphere = new BoundingSphereD(Block.GetPosition(), radius);
            List<IMyEntity> entities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
            IMyFaction ownerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(Block.OwnerId);

            foreach (IMyEntity entity in entities)
            {
                if (entity == null || !MyAPIGateway.Entities.Exist(entity)) continue;

                var cubeGrid = entity as IMyCubeGrid;
                if (cubeGrid == null) continue;

                List<IMySlimBlock> blockList = new List<IMySlimBlock>();
                cubeGrid.GetBlocks(blockList);
                if (blockList.Count < 20) continue;

                List<IMyTerminalBlock> Blocks = new List<IMyTerminalBlock>();
                MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid).GetBlocksOfType(Blocks, x => x.IsFunctional);
                if (Blocks.Count == 0) continue;

                float totalPwr = 0;
                foreach (var slimBlock in Blocks)
                {

                    var bklPower = slimBlock as IMyPowerProducer;
                    if (bklPower == null) continue;

                    if (!Block.IsWorking) continue;

                    totalPwr += bklPower.CurrentOutput;
                }

                if (totalPwr == 0) continue;

                var owners = cubeGrid.BigOwners;
                IMyFaction faction = null;
                bool isEnemy = false;
                foreach (var owner in owners)
                {
                    if (owner == 0) continue;
                    faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(owner);
                    if (faction == null || ownerFaction == null)
                    {
                        if (owner != Block.OwnerId)
                        {
                            isEnemy = true;
                            break;
                        }

                        continue;
                    }
                    if (MyAPIGateway.Session.Factions.AreFactionsEnemies(ownerFaction.FactionId, faction.FactionId))
                    {
                        isEnemy = true;
                        break;
                    }

                }
               // MyVisualScriptLogicProvider.ShowNotification("Control disabled - Enemy Close", 10000, "Red");
                if (isEnemy) return true;
            }

            return false;
        }

        public static void RefreshControls(IMyTerminalBlock block)
        {

            if (MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel)
            {
                var myCubeBlock = block as MyCubeBlock;

                if (myCubeBlock.IDModule != null)
                {

                    var share = myCubeBlock.IDModule.ShareMode;
                    var owner = myCubeBlock.IDModule.Owner;
                    myCubeBlock.ChangeOwner(owner, share == MyOwnershipShareModeEnum.None ? MyOwnershipShareModeEnum.Faction : MyOwnershipShareModeEnum.None);
                    myCubeBlock.ChangeOwner(owner, share);
                }
            }
        }
    }
}