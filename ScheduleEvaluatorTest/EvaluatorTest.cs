using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace ScheduleEvaluator
{
    [TestClass]
    class EvaluatorTest
    {
        private DBConnection conn;
        [TestInitialize]
        public void Initialize() {
            // Do any 'Constructor' type stuff here.
            conn = new DBConnection();
        }

        [TestMethod]
        public void ExampleTest() {
            // Construct a few Schedule Models
            // Construct a preference set.

            // Evaluate against ONE criteria and make sure
            // Everything checks out

            // Include an Assert to signify the test passsing/failing.
            Assert.AreEqual(1, 1);
        }
    }
}
