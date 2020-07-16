using System;
using System.Collections.Generic;
using System.Text;

namespace TimedAssembler.Tests.Utils.Emulators
{
    public interface Em_IMyTerminalBlock { }
    public interface Em_IFunctionalBlock { }

    public class Em_FunctionalBlock : Em_IFunctionalBlock, Em_IMyTerminalBlock
    {

    }
}
