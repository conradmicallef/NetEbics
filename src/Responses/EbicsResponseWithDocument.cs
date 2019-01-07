/*
 * NetEbics -- .NET Core EBICS Client Library
 * (c) Copyright 2018 Bjoern Kuensting
 *
 * This file is subject to the terms and conditions defined in
 * file 'LICENSE.txt', which is part of this source code package.
 */

using System;
using System.Xml.Linq;
using NetEbics.Config;

namespace NetEbics.Responses
{
    public class EbicsResponseWithDocument<T> : Response
    {
        public T ebics;
        public XDocument document;
    }
}