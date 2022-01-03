using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpreadsheetUtilities;
using System.Collections.Generic;
using System;

namespace FormulaTests
{
    [TestClass]
    public class FormulaTests
    {
        private double delta = 1e-9;
        [TestMethod]
        public void testSimpleFormula()
        {
            Formula t = new Formula("2.0 + 5.7");
            Assert.AreEqual(7.7, t.Evaluate(s => 1));
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestBadOpeningFormula()
        {
            Formula t = new Formula(")");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestMismatchParenthesis()
        {
            Formula t = new Formula("(5.0 * 2.1))");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestMismatchParenthesis2()
        {
            Formula t = new Formula("(5.0 * 2.1)) + 3");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestMissingOperator()
        {
            Formula t = new Formula("(5.0 * 2.1)3");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestMissingValue()
        {
            Formula t = new Formula("(5.0 * + 2.1)");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestBadSymbol()
        {
            Formula t = new Formula("(5.0 * 2.1) & 3");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestInValidVariable()
        {
            Formula t = new Formula("(5.0 * 2.1) + a3", s => s.ToLower(), s => false);
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestInValidVariable2()
        {
            Formula t = new Formula("(5.0 * 2.1) + 3a", s => s.ToLower(), s => false);
        }

        [TestMethod]
        public void testSimpleMulti()
        {
            Formula t = new Formula("2.0 * 5.1");
            Assert.AreEqual(10.2, t.Evaluate(s => 1));
        }

        [TestMethod]
        public void testSimpleDiv()
        {
            Formula t = new Formula("3.6 / 1.2");
            Assert.AreEqual(3.0, t.Evaluate(s => 1));
        }

        [TestMethod]
        public void testSimpleSub()
        {
            Formula t = new Formula("3.6 - 1.2");
            Assert.AreEqual(2.4, (double)t.Evaluate(s => 1), delta);
        }

        [TestMethod]
        public void testVariableAdd()
        {
            Formula t = new Formula("A1 + 5.5");
            Assert.AreEqual(6.7, (double)t.Evaluate(s => 1.2), delta);
        }

        [TestMethod]
        public void testVariableSub()
        {
            Formula t = new Formula("5.6 - A1");
            Assert.AreEqual(4.4, (double)t.Evaluate(s => 1.2), delta);
        }

        [TestMethod]
        public void testVariableMulti()
        {
            Formula t = new Formula("5.6 * A1");
            Assert.AreEqual(6.72, (double)t.Evaluate(s => 1.2), delta);
        }

        [TestMethod]
        public void testVariableDiv()
        {
            Formula t = new Formula("5.6 / A1");
            Assert.AreEqual(4.66666666666667, (double)t.Evaluate(s => 1.2), delta);
        }

        [TestMethod]
        public void testDivByZero()
        {
            Formula t = new Formula("5.6 / 0.0");
            Assert.IsInstanceOfType(t.Evaluate(s => 1.7), typeof(FormulaError));
        }

        [TestMethod]
        public void testDivByZeroVariable()
        {
            Formula t = new Formula("5.6 / A1");
            Assert.IsInstanceOfType(t.Evaluate(s => 0.0), typeof(FormulaError));
        }

        [TestMethod]
        public void testbadVariable()
        {
            Formula t = new Formula("5.6 / A1");
            Assert.IsInstanceOfType(t.Evaluate(lookup), typeof(FormulaError));
        }

        private double lookup(string s)
        {
            if (s == "A2")
                return 1.0;
            throw new ArgumentException("Illegal variable");
        }

        [TestMethod]
        public void testLongEq()
        {
            Formula t = new Formula("(5.6 * (47.0 / 10.0) * 10 + 0.4) / 5.3 - 1.3");
            Assert.AreEqual(48.4358490566, (double)t.Evaluate(s => 1.2), delta);
        }

        //[TestMethod]
        //public void testToString()
        //{
        //    Formula t = new Formula("(5.6 * (47.0 / 10.0) * 10 + 0.4) / 5.3 - 1.3");
        //    string s = "(5.6*(47.0/10.0)*10+0.4)/5.3-1.3";
        //    Assert.AreEqual(s, t.ToString());
        //}

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void testOpAfterOpParenthesis()
        {
            Formula t = new Formula("(+5*9)");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void testEmptyFormula()
        {
            Formula t = new Formula("");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void testUnidentifiedSymbol()
        {
            Formula t = new Formula("5&7");
        }

        [TestMethod]
        public void TestEqualEqs()
        {
            Formula t = new Formula("(5.6 * (47.0 / 10.0) * 10 + 0.4) / 5.3 - 1.3");
            Formula t2 = new Formula(t.ToString());
            Assert.AreEqual(true, t.Equals(t2));
        }

        [TestMethod]
        public void testEqualsEqualsOverride()
        {
            Formula t = new Formula("2.0 * 5.1");
            Formula t2 = new Formula(t.ToString());
            Assert.IsTrue(t == t2);
        }

        [TestMethod]
        public void testNotEqualsOverride()
        {
            Formula t = new Formula("2.0 * 5.1");
            Formula t2 = new Formula(t.ToString());
            Assert.IsFalse(t != t2);
        }

        [TestMethod]
        public void testNullEqualsEqualsOverride()
        {
            Formula t = null;
            Formula t2 = null;
            Assert.IsTrue(t == t2);
        }

        [TestMethod]
        public void TestEqualNull()
        {
            Formula t = new Formula("(5.6 * (47.0 / 10.0) * 10 + 0.4) / 5.3 - 1.3");
            Formula t2 = null;
            Assert.IsFalse(t.Equals(t2));
        }

        [TestMethod]
        public void TestGetVariables()
        {
            Formula t = new Formula("(A1 * (A2 / 10.0) * 10 + A3) / 5.3 - A4", s => s.ToLower(), s => true);
            List<string> ans = new List<string>();
            ans.Add("a1");
            ans.Add("a2");
            ans.Add("a3");
            ans.Add("a4");
            int i = 0;
            foreach (string s in t.GetVariables())
            {
                Assert.AreEqual(ans[i], s);
                i++;
            }
        }

        [TestMethod]
        public void testGetHashCodeOverride()
        {
            Formula t = new Formula("2.0 * 5.1");
            Formula t2 = new Formula(t.ToString());
            Assert.IsTrue(t.GetHashCode() == t2.GetHashCode());
        }

        // Normalizer tests
        [TestMethod(), Timeout(2000)]
        [TestCategory("1")]
        public void TestNormalizerGetVars()
        {
            Formula f = new Formula("2+x1", s => s.ToUpper(), s => true);
            HashSet<string> vars = new HashSet<string>(f.GetVariables());

            Assert.IsTrue(vars.SetEquals(new HashSet<string> { "X1" }));
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("2")]
        public void TestNormalizerEquals()
        {
            Formula f = new Formula("2+x1", s => s.ToUpper(), s => true);
            Formula f2 = new Formula("2+X1", s => s.ToUpper(), s => true);

            Assert.IsTrue(f.Equals(f2));
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("3")]
        public void TestNormalizerToString()
        {
            Formula f = new Formula("2+x1", s => s.ToUpper(), s => true);
            Formula f2 = new Formula(f.ToString());

            Assert.IsTrue(f.Equals(f2));
        }

        // Validator tests
        [TestMethod(), Timeout(2000)]
        [TestCategory("4")]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestValidatorFalse()
        {
            Formula f = new Formula("2+x1", s => s, s => false);
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("5")]
        public void TestValidatorX1()
        {
            Formula f = new Formula("2+x", s => s, s => (s == "x"));
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("6")]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestValidatorX2()
        {
            Formula f = new Formula("2+y1", s => s, s => (s == "x"));
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("7")]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestValidatorX3()
        {
            Formula f = new Formula("2+x1", s => s, s => (s == "x"));
        }


        // Simple tests that return FormulaErrors
        [TestMethod(), Timeout(2000)]
        [TestCategory("8")]
        public void TestUnknownVariable()
        {
            Formula f = new Formula("2+X1");
            Assert.IsInstanceOfType(f.Evaluate(s => { throw new ArgumentException("Unknown variable"); }), typeof(FormulaError));
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("9")]
        public void TestDivideByZero()
        {
            Formula f = new Formula("5/0");
            Assert.IsInstanceOfType(f.Evaluate(s => 0), typeof(FormulaError));
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("10")]
        public void TestDivideByZeroVars()
        {
            Formula f = new Formula("(5 + X1) / (X1 - 3)");
            Assert.IsInstanceOfType(f.Evaluate(s => 3), typeof(FormulaError));
        }


        // Tests of syntax errors detected by the constructor
        [TestMethod(), Timeout(2000)]
        [TestCategory("11")]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestSingleOperator()
        {
            Formula f = new Formula("+");
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("12")]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestExtraOperator()
        {
            Formula f = new Formula("2+5+");
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("13")]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestExtraCloseParen()
        {
            Formula f = new Formula("2+5*7)");
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("14")]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestExtraOpenParen()
        {
            Formula f = new Formula("((3+5*7)");
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("15")]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestNoOperator()
        {
            Formula f = new Formula("5x");
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("16")]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestNoOperator2()
        {
            Formula f = new Formula("5+5x");
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("17")]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestNoOperator3()
        {
            Formula f = new Formula("5+7+(5)8");
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("18")]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestNoOperator4()
        {
            Formula f = new Formula("5 5");
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("19")]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestDoubleOperator()
        {
            Formula f = new Formula("5 + + 3");
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("20")]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestEmpty()
        {
            Formula f = new Formula("");
        }

        // Some more complicated formula evaluations
        [TestMethod(), Timeout(2000)]
        [TestCategory("21")]
        public void TestComplex1()
        {
            Formula f = new Formula("y1*3-8/2+4*(8-9*2)/14*x7");
            Assert.AreEqual(5.14285714285714, (double)f.Evaluate(s => (s == "x7") ? 1 : 4), 1e-9);
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("22")]
        public void TestRightParens()
        {
            Formula f = new Formula("x1+(x2+(x3+(x4+(x5+x6))))");
            Assert.AreEqual(6, (double)f.Evaluate(s => 1), 1e-9);
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("23")]
        public void TestLeftParens()
        {
            Formula f = new Formula("((((x1+x2)+x3)+x4)+x5)+x6");
            Assert.AreEqual(12, (double)f.Evaluate(s => 2), 1e-9);
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("53")]
        public void TestRepeatedVar()
        {
            Formula f = new Formula("a4-a4*a4/a4");
            Assert.AreEqual(0, (double)f.Evaluate(s => 3), 1e-9);
        }

        // Test of the Equals method
        [TestMethod(), Timeout(2000)]
        [TestCategory("24")]
        public void TestEqualsBasic()
        {
            Formula f1 = new Formula("X1+X2");
            Formula f2 = new Formula("X1+X2");
            Assert.IsTrue(f1.Equals(f2));
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("25")]
        public void TestEqualsWhitespace()
        {
            Formula f1 = new Formula("X1+X2");
            Formula f2 = new Formula(" X1  +  X2   ");
            Assert.IsTrue(f1.Equals(f2));
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("26")]
        public void TestEqualsDouble()
        {
            Formula f1 = new Formula("2+X1*3.00");
            Formula f2 = new Formula("2.00+X1*3.0");
            Assert.IsTrue(f1.Equals(f2));
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("27")]
        public void TestEqualsComplex()
        {
            Formula f1 = new Formula("1e-2 + X5 + 17.00 * 19 ");
            Formula f2 = new Formula("   0.0100  +     X5+ 17 * 19.00000 ");
            Assert.IsTrue(f1.Equals(f2));
        }


        [TestMethod(), Timeout(2000)]
        [TestCategory("28")]
        public void TestEqualsNullAndString()
        {
            Formula f = new Formula("2");
            Assert.IsFalse(f.Equals(null));
            Assert.IsFalse(f.Equals(""));
        }


        // Tests of == operator
        [TestMethod(), Timeout(2000)]
        [TestCategory("29")]
        public void TestEq()
        {
            Formula f1 = new Formula("2");
            Formula f2 = new Formula("2");
            Assert.IsTrue(f1 == f2);
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("30")]
        public void TestEqFalse()
        {
            Formula f1 = new Formula("2");
            Formula f2 = new Formula("5");
            Assert.IsFalse(f1 == f2);
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("31")]
        public void TestEqNull()
        {
            Formula f1 = new Formula("2");
            Formula f2 = new Formula("2");
            Assert.IsFalse(null == f1);
            Assert.IsFalse(f1 == null);
            Assert.IsTrue(f1 == f2);
        }


        // Tests of != operator
        [TestMethod(), Timeout(2000)]
        [TestCategory("32")]
        public void TestNotEq()
        {
            Formula f1 = new Formula("2");
            Formula f2 = new Formula("2");
            Assert.IsFalse(f1 != f2);
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("33")]
        public void TestNotEqTrue()
        {
            Formula f1 = new Formula("2");
            Formula f2 = new Formula("5");
            Assert.IsTrue(f1 != f2);
        }


        // Test of ToString method
        [TestMethod(), Timeout(2000)]
        [TestCategory("34")]
        public void TestString()
        {
            Formula f = new Formula("2*5");
            Assert.IsTrue(f.Equals(new Formula(f.ToString())));
        }


        // Tests of GetHashCode method
        [TestMethod(), Timeout(2000)]
        [TestCategory("35")]
        public void TestHashCode()
        {
            Formula f1 = new Formula("2*5");
            Formula f2 = new Formula("2*5");
            Assert.IsTrue(f1.GetHashCode() == f2.GetHashCode());
        }

        // Technically the hashcodes could not be equal and still be valid,
        // extremely unlikely though. Check their implementation if this fails.
        [TestMethod(), Timeout(2000)]
        [TestCategory("36")]
        public void TestHashCodeFalse()
        {
            Formula f1 = new Formula("2*5");
            Formula f2 = new Formula("3/8*2+(7)");
            Assert.IsTrue(f1.GetHashCode() != f2.GetHashCode());
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("37")]
        public void TestHashCodeComplex()
        {
            Formula f1 = new Formula("2 * 5 + 4.00 - _x");
            Formula f2 = new Formula("2*5+4-_x");
            Assert.IsTrue(f1.GetHashCode() == f2.GetHashCode());
        }


        // Tests of GetVariables method
        [TestMethod(), Timeout(2000)]
        [TestCategory("38")]
        public void TestVarsNone()
        {
            Formula f = new Formula("2*5");
            Assert.IsFalse(f.GetVariables().GetEnumerator().MoveNext());
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("39")]
        public void TestVarsSimple()
        {
            Formula f = new Formula("2*X2");
            List<string> actual = new List<string>(f.GetVariables());
            HashSet<string> expected = new HashSet<string>() { "X2" };
            Assert.AreEqual(actual.Count, 1);
            Assert.IsTrue(expected.SetEquals(actual));
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("40")]
        public void TestVarsTwo()
        {
            Formula f = new Formula("2*X2+Y3");
            List<string> actual = new List<string>(f.GetVariables());
            HashSet<string> expected = new HashSet<string>() { "Y3", "X2" };
            Assert.AreEqual(actual.Count, 2);
            Assert.IsTrue(expected.SetEquals(actual));
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("41")]
        public void TestVarsDuplicate()
        {
            Formula f = new Formula("2*X2+X2");
            List<string> actual = new List<string>(f.GetVariables());
            HashSet<string> expected = new HashSet<string>() { "X2" };
            Assert.AreEqual(actual.Count, 1);
            Assert.IsTrue(expected.SetEquals(actual));
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("42")]
        public void TestVarsComplex()
        {
            Formula f = new Formula("X1+Y2*X3*Y2+Z7+X1/Z8");
            List<string> actual = new List<string>(f.GetVariables());
            HashSet<string> expected = new HashSet<string>() { "X1", "Y2", "X3", "Z7", "Z8" };
            Assert.AreEqual(actual.Count, 5);
            Assert.IsTrue(expected.SetEquals(actual));
        }

        // Tests to make sure there can be more than one formula at a time
        [TestMethod(), Timeout(2000)]
        [TestCategory("43")]
        public void TestMultipleFormulae()
        {
            Formula f1 = new Formula("2 + a1");
            Formula f2 = new Formula("3");
            Assert.AreEqual(2.0, f1.Evaluate(x => 0));
            Assert.AreEqual(3.0, f2.Evaluate(x => 0));
            Assert.IsFalse(new Formula(f1.ToString()) == new Formula(f2.ToString()));
            IEnumerator<string> f1Vars = f1.GetVariables().GetEnumerator();
            IEnumerator<string> f2Vars = f2.GetVariables().GetEnumerator();
            Assert.IsFalse(f2Vars.MoveNext());
            Assert.IsTrue(f1Vars.MoveNext());
        }

        // Repeat this test to increase its weight
        [TestMethod(), Timeout(2000)]
        [TestCategory("44")]
        public void TestMultipleFormulaeB()
        {
            TestMultipleFormulae();
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("45")]
        public void TestMultipleFormulaeC()
        {
            TestMultipleFormulae();
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("46")]
        public void TestMultipleFormulaeD()
        {
            TestMultipleFormulae();
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("47")]
        public void TestMultipleFormulaeE()
        {
            TestMultipleFormulae();
        }

        // Stress test for constructor
        [TestMethod(), Timeout(2000)]
        [TestCategory("48")]
        public void TestConstructor()
        {
            Formula f = new Formula("(((((2+3*X1)/(7e-5+X2-X4))*X5+.0005e+92)-8.2)*3.14159) * ((x2+3.1)-.00000000008)");
        }

        // This test is repeated to increase its weight
        [TestMethod(), Timeout(2000)]
        [TestCategory("49")]
        public void TestConstructorB()
        {
            Formula f = new Formula("(((((2+3*X1)/(7e-5+X2-X4))*X5+.0005e+92)-8.2)*3.14159) * ((x2+3.1)-.00000000008)");
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("50")]
        public void TestConstructorC()
        {
            Formula f = new Formula("(((((2+3*X1)/(7e-5+X2-X4))*X5+.0005e+92)-8.2)*3.14159) * ((x2+3.1)-.00000000008)");
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("51")]
        public void TestConstructorD()
        {
            Formula f = new Formula("(((((2+3*X1)/(7e-5+X2-X4))*X5+.0005e+92)-8.2)*3.14159) * ((x2+3.1)-.00000000008)");
        }

        // Stress test for constructor
        [TestMethod(), Timeout(2000)]
        [TestCategory("52")]
        public void TestConstructorE()
        {
            Formula f = new Formula("(((((2+3*X1)/(7e-5+X2-X4))*X5+.0005e+92)-8.2)*3.14159) * ((x2+3.1)-.00000000008)");
        }

    }
}