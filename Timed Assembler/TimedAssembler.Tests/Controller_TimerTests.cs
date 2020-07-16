using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using Nado.TimedBlocks;
using TimedAssembler.Tests.Utils;

namespace TimedAssembler.Tests
{
    namespace ControllerTests
    {
        [TestClass]
        public class Controller_TimerTests
        {

            [TestMethod]
            [DataRow(1000, 1000, true)]
            [DataRow(1000, 1100, false)]
            public void TestActivation(int blockHour, int debugHour, bool expectIsActive)
            {
                //..Generate a controller to use
                var controller = TestUtils.GenerateController(blockHour, blockHour + 100);

                //..Set the "fake" hour to a specific time, so we can test active/inactive
                controller.DebugSetHour(debugHour / 100);

                //..Call the update method to trigger the activation
                controller.Update((60 ^ 3) * debugHour);

                //..Test for the expected results
                Assert.AreEqual(expectIsActive, controller.IsActive());
            }

            /// <summary>
            /// Test the controller over a total of (2) days with full activation 
            /// </summary>
            [TestMethod]
            public void FullSimulation()
            {
                //..Generate a controller to use
                var controller = TestUtils.GenerateEmptyController();

                //..Add some times to it (we'll try all of these)
                int[][] pairData = new[]
                {
                new int[] {1100, 1200},
                new int[] {1300, 1400},
                new int[] {1800, 1900},
            };

                //..Now let's add the times to the controller and test them out
                foreach (int[] pair in pairData)
                    controller.AddTime(pair[0], pair[1]);

                //..simulate activation/deactivation over a certain number of days
                for (int day = 0; day < 2; day++)
                {
                    for (int t = 0; t < 2400; t += 100)
                    {
                        Console.WriteLine("HOUR: " + t);

                        //..Get the current active block
                        TimePair pair = controller.GetTimes().FirstOrDefault(p => t >= p.StartHour && t < p.FinishHour);

                        controller.DebugSetHour(t / 100);
                        controller.Update((60 ^ 3) * t);
                        Assert.AreEqual((pair != null), controller.IsActive());
                    }
                }
            }
        }
    }
}
