/*
 * NetEbics -- .NET Core EBICS Client Library
 * (c) Copyright 2018 Bjoern Kuensting
 *
 * This file is subject to the terms and conditions defined in
 * file 'LICENSE.txt', which is part of this source code package.
 */

using StatePrinting;
using StatePrinting.OutputFormatters;
using ebics = ebicsxml.H004;

namespace NetEbics.Config
{
    public class BankParams
    {
        private static readonly Stateprinter _printer;
        protected static string s_signatureAlg => "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
        protected static string s_digestAlg => "http://www.w3.org/2001/04/xmlenc#sha256";
        internal ebics.StaticHeaderTypeBankPubKeyDigests pubkeydigests { get
            {
                return new ebics.StaticHeaderTypeBankPubKeyDigests
                {
                    Authentication = new ebics.StaticHeaderTypeBankPubKeyDigestsAuthentication { Algorithm = s_digestAlg, Version = AuthKeys.Version.ToString(), Value = AuthKeys.Digest },
                    Encryption= new ebics.StaticHeaderTypeBankPubKeyDigestsEncryption{ Algorithm = s_digestAlg, Version = AuthKeys.Version.ToString(), Value = AuthKeys.Digest },
                    Signature=new ebics.StaticHeaderTypeBankPubKeyDigestsSignature{ Algorithm = s_digestAlg, Version = AuthKeys.Version.ToString(), Value = AuthKeys.Digest }
                };
            } }

        public AuthKeyPair AuthKeys { get; set; }
        public CryptKeyPair CryptKeys { get; set; }
        public SignKeyPair SignKeys { get; set; }

        static BankParams()
        {
            _printer = new Stateprinter();
            _printer.Configuration.SetNewlineDefinition("");
            _printer.Configuration.SetIndentIncrement(" ");
            _printer.Configuration.SetOutputFormatter(new JsonStyle(_printer.Configuration));
        }

        public override string ToString() => _printer.PrintObject(this);
    }
}