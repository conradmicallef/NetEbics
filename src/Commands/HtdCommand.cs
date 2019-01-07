/*
 * NetEbics -- .NET Core EBICS Client Library
 * (c) Copyright 2018 Bjoern Kuensting
 *
 * This file is subject to the terms and conditions defined in
 * file 'LICENSE.txt', which is part of this source code package.
 */

using ebics = ebicsxml.H004;

namespace NetEbics.Commands
{
    internal class HtdCommand : GenericEbicsDCommand<ebics.HTDReponseOrderDataType,ebics.StandardOrderParamsType>
    {
        internal override string OrderType => "HTD";
        internal override string OrderAttribute => "DZHNN";
        internal override TransactionType TransactionType => TransactionType.Download;
    }
}