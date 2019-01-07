﻿/*
 * NetEbics -- .NET Core EBICS Client Library
 * (c) Copyright 2018 Bjoern Kuensting
 *
 * This file is subject to the terms and conditions defined in
 * file 'LICENSE.txt', which is part of this source code package.
 */

using ebics = ebicsxml.H004;

namespace NetEbics.Commands
{
    internal class HvzCommand : GenericEbicsDCommand<ebics.HVZResponseOrderDataType,ebics.HVZOrderParamsType>
    {
        internal override string OrderType => "HVZ";
        internal override string OrderAttribute => "DZNNN";
        internal override TransactionType TransactionType => TransactionType.Download;
    }
}