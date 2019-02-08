using System;

namespace NetEbics.Swift
{
        public class F61 : FixedStr
        {

            public string value_date { get; set; }
            public string entry_date { get; set; }
            public string debit_credit { get; set; }
            public string funds_code { get; set; }
            public string amount { get; set; }
            public string trxcode { get; set; }
            public string refaccount { get; set; }
            public string srvaccount { get; set; }
            public string suppl { get; set; }
            public F61():base(string.Empty) { throw new NotImplementedException(); }
            public F61(string op) : base(op)
            {
                value_date = TakeM(6);
                entry_date = TakeM(4);
                debit_credit = TakeM(1);
                funds_code = TakeM(1);
                amount = TakeM(15);
                trxcode = TakeM(3);
                var sepofs = operand.IndexOf(@"//");
                if (sepofs < 0)
                {
                    refaccount = operand;
                    return;
                }
                refaccount = TakeM(sepofs);
                var separator = TakeM(2);
                if (separator != "//")
                    throw new Exception("Unknwon Format");
                srvaccount = Take(16);
                suppl = Take(34);
            }
        }
    }
