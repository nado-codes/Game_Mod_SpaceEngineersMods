using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace SafeZoneBlockLogic
{
    [ProtoContract(IgnoreListHandling = true)]
    public class IntermodSettings
    {
        [ProtoMember(1)]
        public bool spawnStalinRoid { get; set; }

        [ProtoMember(2)]
        public Vector3D stalinRoidGPS { get; set; }

        [ProtoMember(3)]
        public bool enableWarpLocation { get; set; }

        [ProtoMember(4)]
        public Vector3D warpGateGPS { get; set; }

        [ProtoMember(5)]
        public List<int> DaysToReset { get; set; }

        [ProtoMember(6)]
        public List<string> RoidNames { get; set; }

        [ProtoMember(7)]
        public int RoidSpawnDistance { get; set; }

        [ProtoMember(8)]
        public int MaxSpawnCoverage { get; set; }
    }
}
