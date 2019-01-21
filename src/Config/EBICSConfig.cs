/*
 * NetEbics -- .NET Core EBICS Client Library
 * (c) Copyright 2018 Bjoern Kuensting
 *
 * This file is subject to the terms and conditions defined in
 * file 'LICENSE.txt', which is part of this source code package.
 */

using StatePrinting;
using StatePrinting.OutputFormatters;
using System;
using System.IO;

namespace NetEbics.Config
{
    internal class EbicsConfig
    {
        private static readonly Stateprinter _printer;

        public string Address { get; set; }
        public UserParams User { get; set; }
        public BankParams Bank { get; set; }
        //internal Func<string, Stream> readStream;
        internal Func<string, Byte[]> readBytes;
        //internal Func<string, Stream> writeStream;
        internal Action<string, Byte[]> writeBytes;
        public string Vendor = "BL Banking";
        //TODO move to helper
        public ebicsxml.H004.StaticHeaderTypeProduct StaticHeaderTypeProduct
        {
            get {
                return new ebicsxml.H004.StaticHeaderTypeProduct
                {
                    InstituteID = Vendor,
                    Language = "EN",
                    Value = Vendor
                };
            }
        }
        //TODO move to helper
        public ebicsxml.H004.ProductElementType ProductElementType
        {
            get {
                return new ebicsxml.H004.ProductElementType
                {
                    InstituteID = Vendor,
                    Language = "EN",
                    Value = Vendor
                };
            }
        }
        static EbicsConfig()
        {
            _printer = new Stateprinter();
            _printer.Configuration.SetNewlineDefinition("");
            _printer.Configuration.SetIndentIncrement(" ");
            _printer.Configuration.SetOutputFormatter(new JsonStyle(_printer.Configuration));
        }
        public void LoadBank()
        {
            Bank = new BankParams();
            Bank.Load(readBytes);
        }

        public override string ToString() => _printer.PrintObject(this);
    }
}