/*
 * NetEbics -- .NET Core EBICS Client Library
 * (c) Copyright 2018 Bjoern Kuensting
 *
 * This file is subject to the terms and conditions defined in
 * file 'LICENSE.txt', which is part of this source code package.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using NetEbics.Exceptions;
using NetEbics.Handler;
using NetEbics.Parameters;
using NetEbics.Responses;
using NetEbics.Xml;
using ebics = ebicsxml.H004;

namespace NetEbics.Commands
{
    internal class HvdCommand : GenericEbicsDCommand<ebics.HVDResponseOrderDataType,ebics.HVDOrderParamsType>
    {
        internal override string OrderType => "HVD";
        internal override string OrderAttribute => "OZHNN";
        internal override TransactionType TransactionType => TransactionType.Download;
    }
}