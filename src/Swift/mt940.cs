using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NetEbics.Swift
{
    public class MT940
    {
        public static IEnumerable<string> LineJoiner(IEnumerable<string> source)
        {
            string prevLine = null;
            foreach (var line in source)
            {
                if (prevLine == null)
                {
                    if (string.IsNullOrEmpty(line))
                        continue;
                    if (!line.StartsWith(':'))
                        continue;
                    prevLine = line;
                    continue;
                }
                if (string.IsNullOrEmpty(line) || line.StartsWith(':'))
                {
                    yield return prevLine;
                    prevLine = line;
                }
                else
                    prevLine = prevLine + line;
            }
            if (prevLine != null)
                yield return prevLine;
        }
        public class F60
        {
            static Regex r60c = new Regex("^([CD])([0-9]{6})([A-Z]{3})(.*)$", RegexOptions.Compiled);
            public string sign { get; set; }
            public string date { get; set; }
            public string currency { get; set; }
            public string balance { get; set; }
            public F60(string operand)
            {
                var f60 = r60c.Match(operand);
                sign = f60.Groups[1].Value;
                date = f60.Groups[2].Value;
                currency = f60.Groups[3].Value;
                balance = f60.Groups[4].Value;
            }
        }
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
        public class F86
        {

            public string TRXCODE { get; private set; }
            public string journal_no { get; private set; }
            public string posting { get; private set; }
            public string RemInfo { get; private set; }
            public string BIC { get; private set; }
            public string IBAN { get; private set; }
            public string Payer_Name { get; private set; }
            public string SepaCode { get; private set; }
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
                            RemInfo += operand+"\r\n"; break;
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
        public class Record
        {
            public string TRN { get; set; }
            public string RELATEDREF { get; set; }
            public string Account { get; set; }
            public string f28_StatementNumber { get; set; }
            public string f28_SequenceNumber { get; set; }
            public F60 f60 { get; set; }
            public string L62 { get; set; }
            public List<F61> L61 { get; set; } = new List<F61>();
            public List<F86> L86 { get; set; } = new List<F86>();

            static Regex r28c = new Regex("^([^/]*)/(.*)$", RegexOptions.Compiled);

            public bool mandatory()
            {
                if (!string.IsNullOrEmpty(TRN)
                    && !string.IsNullOrEmpty(Account))
                    return true;
                return false;
            }
            public bool loadline(string line)
            {
                if (string.IsNullOrWhiteSpace(line))
                    return false;
                if (!line.StartsWith(':'))
                    throw new InvalidOperationException("Bad MT940 Format");
                var spl = line.Split(':', 3);
                string fno = spl[1].ToUpper();
                string operand = spl[2];
                switch (fno)
                {
                    case "20":
                        TRN = operand; break;
                    case "21":
                        RELATEDREF = operand; break;
                    case "25":
                        Account = operand; break;
                    case "28C":
                        var f28 = r28c.Match(operand);
                        f28_StatementNumber = f28.Groups[1].Value;
                        f28_SequenceNumber = f28.Groups[2].Value;
                        break;
                    case "60F":
                    case "60M":
                        f60 = new F60(operand);
                        break;
                    case "61":
                        L61.Add(new F61(operand));
                        break;
                    case "86":
                        L86.Add(new F86(operand));
                        break;
                    case "62F":
                    case "62M":
                        L62 = fno + ":" + operand;
                        break;
                    default:
                        throw new NotImplementedException();
                }
                return true;
            }
        }
    }
}
