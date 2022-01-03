using System;
using FormulaEvaluator;

namespace FormulaTest
{
    class Program
    {
        public static int lookUp(string s)
        {
            s = s.Trim(' ');
            if (s == "A1")
                return 10;
            if (s == "AA2")
                return 32;
            if (s == "AA3")
                return 2;
            else
            {
                throw new ArgumentException("Variable not found");
            }
        }
        static void Main(string[] args)
        {
            Console.WriteLine(Evaluator.Evaluate("5 + 3", null));
            Console.WriteLine(Evaluator.Evaluate("5 + 3 * (3 + 2)", null));
            Console.WriteLine(Evaluator.Evaluate("A1", lookUp));
            Console.WriteLine(Evaluator.Evaluate("    A1   ", lookUp));
            //Console.WriteLine(Evaluator.Evaluate("2/0", lookUp)); //throws divide by 0 exception
            //Console.WriteLine(Evaluator.Evaluate("4 3", lookUp)); //throws error at variable lookup
            //Console.WriteLine(Evaluator.Evaluate("3(4 + 5) * 7", lookUp)); //throws correct error
            Console.WriteLine(Evaluator.Evaluate("A1 * (5/2)", lookUp));
            Console.WriteLine(Evaluator.Evaluate("AA3-(43+21)*AA2", lookUp));
            Console.WriteLine(Evaluator.Evaluate("3", lookUp));
            Console.WriteLine(Evaluator.Evaluate("(1*1)-2/2", lookUp));
            //Console.WriteLine(Evaluator.Evaluate("AA3-(43+21)*AA4", lookUp)); //throws correct exception
            Console.WriteLine(Evaluator.Evaluate("(5*4+3)+2*9+7/7+2", lookUp));
            //Console.WriteLine(Evaluator.Evaluate(" 3a ", lookUp)); //throws invalid variable
            //Console.WriteLine(Evaluator.Evaluate(")", lookUp)); // throws correct exception
            //Console.WriteLine(Evaluator.Evaluate("", lookUp)); // throws correct exception
            Console.Read();
        }
    }
}
