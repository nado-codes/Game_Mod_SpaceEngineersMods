using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.ModAPI;
using VRageRender;

namespace TimedAssembler.Tests
{
    [TestClass]
    public class Controller_BlockTests
    {
        private IMyFunctionalBlock GenerateFunctionalBlock()
        {
            return new MyAssembler();
        }

        [TestMethod]
        public void AddBlock()
        {
            MyRenderSettings settings = new MyRenderSettings();
            //MyTerminalControl<MyAssembler> terminal = new MyTerminalControlButton<MyAssembler>();

            //..Generate a controller to use
            var controller = TestUtils.GenerateController(1000,1100);
            IMyFunctionalBlock assembler = GenerateFunctionalBlock();

            //..Add some block to the controller
            controller.AddBlock(assembler);
            controller.AddBlock(assembler);

            //..Make sure that we can only add the same entity once
            Assert.AreEqual(1,controller.GetBlocks().Count);

            //..Test if the "EnabledChanged" event has been initialised and triggers properly
            assembler.Enabled = true;

            Assert.IsFalse(assembler.Enabled,"\"IsEnabled\" was not initialised properly. The controller is inactive!");
        }
    }
}
