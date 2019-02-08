using System;
using System.Collections.Generic;

namespace NetEbics.Swift
{
        public class F86
        {
            public string TRXCODE { get; private set; }
            public string journal_no { get; private set; }
            public string posting { get; private set; }
            public Dictionary<string,string> RemInfo { get; private set; }
            public string BIC { get; private set; }
            public string IBAN { get; private set; }
            public string Payer_Name { get; private set; }
            public string SepaCode { get; private set; }
            public F86() { throw new NotImplementedException(); }
            public F86(string data)
            {
                TRXCODE = data.Substring(0, 3);
                data = data.Substring(3);
                var lines = data.Split('?');
                foreach (var line in lines)
                {
                    if (line.Length == 0)
                        continue;
                    var fno = int.Parse(line.Substring(0, 2));
                    var operand = line.Substring(2);
                    switch (fno)
                    {
                        case 0:
                            posting = operand; break;
                        case 10:
                            journal_no = operand; break;
                        case int n1 when (n1 >= 20 && n1 <= 29):
                        case int n2 when (n2 >= 60 && n2 <= 63):
                            var o = operand.Split('+', 2);
                            RemInfo.Add(o[0],o[1]);
                            break;
                        case 30:
                            BIC = operand; break;
                        case 31:
                            IBAN = operand; break;
                        case 32:
                        case 33:
                            Payer_Name += operand; break;
                        case 34:
                            SepaCode = operand; break;
                        default:
                            throw new Exception("Unknown F86 subfield");
                    }
                }
            }
        }
}
