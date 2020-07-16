using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace TimedAssembler.Tests.Utils.Emulators
{
    public interface EM_IMyPlayer { }

    public partial class EM_Player : EM_IMyPlayer
    {
        public EM_Character Character => new EM_Character();
    }

    public class EM_Character
    {
        public Matrix3x3 WorldMatrix => new Matrix3x3();
    }
}
