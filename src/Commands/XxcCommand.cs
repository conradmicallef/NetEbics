/*
 * NetEbics -- .NET Core EBICS Client Library
 * (c) Copyright 2018 Bjoern Kuensting
 *
 * This file is subject to the terms and conditions defined in
 * file 'LICENSE.txt', which is part of this source code package.
 */

using System.Xml.Linq;
using NetEbics.Parameters;
using NetEbics.Responses;
using ebics = ebicsxml.H004;

namespace NetEbics.Commands
{
    internal class XxcCommand : UCommand
    {
        internal XxcParams Params;
        //protected override object _Params => Params.ebics;
        protected override XDocument _Params => Params.document;

        protected override string SecurityMedium => Params.SecurityMedium;

        internal override string OrderType => "XXC";

        public XxcResponse Response = new XxcResponse();

        internal override DeserializeResponse Deserialize(string payload)
        {
            var ret = base.Deserialize(payload);
            UpdateResponse(Response, ret);
//            Response.Data = XMLDeserialize<ebics.HVUResponseOrderDataType>(ResponseData);
            return ret;
        }
       
    }
}