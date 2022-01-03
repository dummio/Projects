using Microsoft.VisualStudio.TestTools.UnitTesting;
using FormulaEvaluator;
using System;

namespace formulaTester
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            Assert.IsTrue(Evaluator.Evaluate("5 + 3", null) == 8);
        }
    }
}
