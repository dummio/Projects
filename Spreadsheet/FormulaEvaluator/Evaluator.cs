using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
//@Author Kevin Xue
namespace FormulaEvaluator
{
    //Class that can handle basic integer equations
    public static class Evaluator
    {
        //delegate instance variable used for variable lookup
        public delegate int Lookup(String v);

        //method used to evaluate equations
        //parameters : string equation, delegate for lookup
        //returns integer answer
        public static int Evaluate(String exp, Lookup variableEvaluator)
        {
            //keeps track of all of the operators within equation
            Stack<string> equation = new Stack<string>();
            //keeps track of all integers within equation
            Stack<int> numbers = new Stack<int>();
            string[] substrings = Regex.Split(exp, "(\\()|(\\))|(-)|(\\+)|(\\*)|(/)");
            foreach (string s in substrings)
            {
                if(s == " " || s.Length == 0)
                {
                    continue;
                }
                if(IsOperator(s))
                {
                    OperationHandling(s, equation, numbers);
                }
                else
                {
                    int temp = 0;
                    if(int.TryParse(s, out temp))
                    {
                        IntHandling(temp, equation, numbers);
                    }
                    else
                    {
                        if(ValidVariable(s))
                        {
                            IntHandling(variableEvaluator(s), equation, numbers);
                        }
                        else
                        {
                            throw new ArgumentException("Invalid variable");
                        }
                    }
                }
            }
            //handles final operator within equation
            if(equation.Count > 0)
            {
                if((equation.Peek() != "+" || equation.Peek() != "-") && numbers.Count != 2)
                {
                    throw new ArgumentException("misplaced operator");
                }
                else
                {
                    OperationHandling(equation.Peek(), equation, numbers);
                }
            }
            if(numbers.Count != 1)
            {
                throw new ArgumentException("missing operator");
            }
            return numbers.Pop();
        }

        //checks if string is an operator
        private static bool IsOperator(string s)
        {
            return s == "+" || s == "-" || s == "*" || s == "/" || s == "(" || s == ")";
        }

        //method to do all of the operator stack checking
        //parameters : String s (operator), equation stack, numbers stack
        //Doesn't return anything becuase it pushes ints/operators to their respective stacks
        private static void OperationHandling(string s, Stack<string> eq, Stack<int> nums)
        {
            if(s == "+" || s == "-")
            {
                if(eq.Count != 0 && (eq.Peek() == "+" || eq.Peek() == "-"))
                {
                    if(nums.Count >= 2)
                    {
                        int temp = nums.Pop();
                        nums.Push(Calculate(nums.Pop(), temp, eq.Pop()));
                        eq.Push(s);
                    }
                    else
                    {
                        throw new ArgumentException("misplaced operator");
                    }
                }
                else
                {
                    eq.Push(s);
                }
            }
            if(s == "*" || s == "/" || s == "(")
            {
                eq.Push(s);
            }
            if(s == ")")
            {
                if(eq.Count > 0 && (eq.Peek() == "+" || eq.Peek() == "-"))
                {
                    if (nums.Count < 2)
                        throw new ArgumentException("misplaced operator");
                    if(nums.Count >= 2)
                    {
                        int temp = nums.Pop();
                        nums.Push(Calculate(nums.Pop(), temp, eq.Pop()));
                    }
                }
                if (eq.Count == 0 || eq.Peek() != "(")
                    throw new ArgumentException("unmatched parentheses");
                eq.Pop();
                if(eq.Count > 0 && (eq.Peek() == "*" || eq.Peek() == "/"))
                {
                    int temp = nums.Pop();
                    nums.Push(Calculate(nums.Pop(), temp, eq.Pop()));
                }
            }
        }

        private static bool ValidVariable(string s)
        {
            s = s.Trim(' ');
            return Char.IsLetter(s[0]) && Char.IsDigit(s[s.Length - 1]);
        }

        //handles integers
        //Parameters : Integer current, Operators stack, numbers stack
        //pushes values to the num stack
        private static void IntHandling(int i, Stack<string> eq, Stack<int> nums)
        {
            if (eq.Count != 0 && (eq.Peek() == "/" || eq.Peek() == "*"))
            {
                if (nums.Count > 0)
                    nums.Push(Calculate(nums.Pop(), i, eq.Pop()));
                else
                    throw new ArgumentException("Misplaced operation");
            }
            else
                nums.Push(i);
        }

        //handles calculations
        private static int Calculate(int num1, int num2, string op)
        {
            int ans = 0;
            //returns the operation between two integers
            if (op == "+")
                ans = num1 + num2;
            if (op == "-")
                ans = num1 - num2;
            if (op == "*")
                ans = num1 * num2;
            if (op == "/")
            {
                if (num2 == 0)
                    throw new ArgumentException("Divide by zero occurred");
                else
                    ans = num1 / num2;
            }
            return ans;
        }
    }
}
