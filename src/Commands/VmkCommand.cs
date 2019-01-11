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
    internal class VmkCommand : GenericEbicsDCommand<int,ebics.StandardOrderParamsType>
    {
        internal override string OrderType => "VMK";
        internal override string OrderAttribute => "DZHNN";
        internal override TransactionType TransactionType => TransactionType.Download;
    }
}