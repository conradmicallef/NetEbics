using System;

namespace NetEbics.Swift
{
        public class FixedStr
        {
            protected string operand;
            protected FixedStr(string op) { this.operand = op; }
            protected string Take(int i)
            {
                if (operand.Length <= i)
                {
                    string ret = operand;
                    operand = string.Empty;
                    return ret;
                }
                else
                {
                    string ret = operand.Substring(0, i);
                    operand = operand.Substring(i);
                    return ret;
                }
            }
            protected string TakeM(int i)
            {
                if (operand.Length < i)
                    throw new Exception("Mandatory Field Missing");
                if (operand.Length == i)
                {
                    string ret = operand;
                    operand = string.Empty;
                    return ret;
                }
                else
                {
                    string ret = operand.Substring(0, i);
                    operand = operand.Substring(i);
                    return ret;
                }
            }
        }
    }
