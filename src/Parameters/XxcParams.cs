﻿/*
 * NetEbics -- .NET Core EBICS Client Library
 * (c) Copyright 2018 Bjoern Kuensting
 *
 * This file is subject to the terms and conditions defined in
 * file 'LICENSE.txt', which is part of this source code package.
 */

using System.Collections.Generic;
using System.Xml.Linq;

namespace NetEbics.Parameters
{
    public class XxcParams : Params
    {
        public XDocument document { get; set; }
    }
}