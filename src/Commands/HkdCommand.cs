/*
 * NetEbics -- .NET Core EBICS Client Library
 * (c) Copyright 2018 Bjoern Kuensting
 *
 * This file is subject to the terms and conditions defined in
 * file 'LICENSE.txt', which is part of this source code package.
 */

using NetEbics.Parameters;
using NetEbics.Responses;
using ebics = ebicsxml.H004;

namespace NetEbics.Commands
{
    internal class HkdCommand : DCommand
    {
        internal HkdParams Params;
        protected override object _Params => new ebics.StandardOrderParamsType();

        protected override string SecurityMedium => Params.SecurityMedium;

        internal override string OrderType => "HKD";

        public HkdResponse Response = new HkdResponse();

        internal override DeserializeResponse Deserialize(string payload)
        {
            var ret = base.Deserialize(payload);
            UpdateResponse(Response, ret);
            Response.Data = XMLDeserialize<ebics.HKDResponseOrderDataType>(ResponseData);
            return ret;
        }

    }
}