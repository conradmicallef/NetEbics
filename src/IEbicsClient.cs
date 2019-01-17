/*
 * NetEbics -- .NET Core EBICS Client Library
 * (c) Copyright 2018 Bjoern Kuensting
 *
 * This file is subject to the terms and conditions defined in
 * file 'LICENSE.txt', which is part of this source code package.
 */

using NetEbics.Config;
using NetEbics.Parameters;
using NetEbics.Responses;
using ebics = ebicsxml.H004;

namespace NetEbics
{
    public interface IEbicsClient
    {
        EbicsConfig Config { get; set; }
        HpbResponse HPB(HpbParams p);
        PtkResponse PTK(PtkParams p);
        CctResponse CCT(CctParams p);
        IniResponse INI(IniParams p);
        HiaResponse HIA(HiaParams p);
        SprResponse SPR(SprParams p);
        CddResponse CDD(CddParams p);

        HvzResponse HVZ(HvzParams p);
        HvuResponse HVU(HvuParams p);
        HvdResponse HVD(HvdParams p);
        HtdResponse HTD(HtdParams p);
        StaResponse STA(StaParams p);
        VmkResponse VMK(VmkParams p);
        HpdResponse HPD(HpdParams p);
        HvtResponse HVT(HvtParams p);

        HaaResponse HAA(HaaParams p);
        HkdResponse HKD(HkdParams p);
        HevResponse HEV(HevParams p);
        XxcResponse XXC(XxcParams p);
    }
}