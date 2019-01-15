/*
 * NetEbics -- .NET Core EBICS Client Library
 * (c) Copyright 2018 Bjoern Kuensting
 *
 * This file is subject to the terms and conditions defined in
 * file 'LICENSE.txt', which is part of this source code package.
 */

using ebics = ebicsxml.H004;

namespace NetEbics.Responses
{
    public class HtdResponse: Response
    {
        public ebics.HTDReponseOrderDataType Data { get; internal set; }
    }
}