using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nado.Commands;

namespace TimedAssembler.Tests
{
    [TestClass]
    public class CommandsTests
    {
        private CmdAction EmptyCommand { get => (parms) => { }; }

        [TestMethod]
        public void TestCommandInitialisation()
        {
            CommandsController.Init(true);

            CommandsController.CreateCommand("cmd1", EmptyCommand, false, false);
            CommandsController.CreateCommand("cmd2", EmptyCommand, false, false);
            CommandsController.CreateCommand("cmd3", EmptyCommand, false, false);

            Assert.AreEqual(3, CommandsController.GetCommandCount());

            CommandsController.CheckUnload();

            CommandsController.Init(true);

            CommandsController.CreateCommand("cmd1", EmptyCommand, false, false);
            CommandsController.CreateCommand("cmd2", EmptyCommand, false, false);
            CommandsController.CreateCommand("cmd3", EmptyCommand, false, false);

            Assert.AreEqual(3, CommandsController.GetCommandCount());
        }

        [TestMethod]
        public void TestCommandProcessing()
        {
            TestCommandProcess();
        }

        private void TestCommandProcess()
        {
            CommandsController.CreateCommand("testValid", EmptyCommand, false, false);

            bool sendToOthers = true;
            
            CommandsController.DEBUG_HandleMessage("/testValid", ref sendToOthers);
            Assert.IsFalse(sendToOthers, "Command was not processed as expected");

            CommandsController.DEBUG_HandleMessage("testInvalid", ref sendToOthers);
            Assert.IsTrue(sendToOthers, "Invalid command was processed!");
        }

        [TestMethod]
        public void PromptsTest()
        {
            bool confirmedCommand = false;
            CommandsController.Init(true);

            CommandsController.CreateCommand("promptCmd", (cmdParams) => { confirmedCommand = true; }, true, false);

            //..test valid command
            TestCommandProcess();

            bool sendToOthers = true;
            CommandsController.DEBUG_HandleMessage("/promptCmd", ref sendToOthers);

            //..test prompt DENY
            CommandsController.DEBUG_HandleMessage("/n", ref sendToOthers);
            Assert.IsFalse(confirmedCommand,"Command was executed prematurely!");

            //..test prompt ACCEPT
            CommandsController.DEBUG_HandleMessage("/y", ref sendToOthers);
            Assert.IsTrue(confirmedCommand,"Command wasn't executed");
        }
    }
}
