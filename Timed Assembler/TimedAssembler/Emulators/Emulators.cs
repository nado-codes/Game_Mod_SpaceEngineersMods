using System;
using System.Collections.Generic;
using System.Text;

using MyAPIGateway_Base = TimedAssembler.Tests.Utils.Emulators.Emulator_MyAPIGateway;
using IMyFunctionalBlock_Base = TimedAssembler.Tests.Utils.Emulators.Em_IFunctionalBlock;
using IMyTerminalBlock_Base = TimedAssembler.Tests.Utils.Emulators.Em_IMyTerminalBlock;
using IMyPlayer_Base = TimedAssembler.Tests.Utils.Emulators.EM_IMyPlayer;

namespace TimedAssembler.Emulators
{
    public class MyAPIGateway : MyAPIGateway_Base { }
    public interface IMyFunctionalBlock : IMyFunctionalBlock_Base { }
    public interface IMyTerminalBlock : IMyTerminalBlock_Base { }
    public interface IMyPlayer : IMyPlayer_Base { }
}
