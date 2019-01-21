/*
 * NetEbics -- .NET Core EBICS Client Library
 * (c) Copyright 2018 Bjoern Kuensting
 *
 * This file is subject to the terms and conditions defined in
 * file 'LICENSE.txt', which is part of this source code package.
 */

using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using NetEbics.Config;
using NetEbics.Handler;
using NetEbics.Parameters;
using NetEbics.Responses;
using ebics = ebicsxml.H004;

namespace NetEbics
{
    public class EbicsClientFactory
    {
        private readonly Func<EbicsConfig, IEbicsClient> _ctor;

        internal EbicsClientFactory(Func<EbicsConfig, IEbicsClient> ctor)
        {
            _ctor = ctor;
        }

        internal IEbicsClient Create(EbicsConfig cfg)
        {
            return _ctor(cfg);
        }
        public enum LoadStage
        {
            None=0,
            Sign=1,
            Auth=2,
            Enc=3,
            Bank=4,
            All=4
        }
        public IEbicsClient Create( 
            Func<string,byte[]> readBytes,
            Action<string, byte[]> writeBytes,
            string password,string address, string hostId, string partnerId, string userId, 
            LoadStage loadStage=LoadStage.All)
        {
            var signCert= loadStage>=LoadStage.Sign?new System.Security.Cryptography.X509Certificates.X509Certificate2(readBytes("sign.p12"), password):null;
            var authCert= loadStage >= LoadStage.Auth ? new System.Security.Cryptography.X509Certificates.X509Certificate2(readBytes("auth.p12"), password):null;
            var encCert = loadStage >= LoadStage.Enc ? new System.Security.Cryptography.X509Certificates.X509Certificate2(readBytes("enc.p12"), password):null;
            var cfg=new EbicsConfig
            {
                readBytes=readBytes,
                writeBytes=writeBytes,

                Address = address,
                User = new UserParams
                {
                    HostId = hostId,
                    PartnerId = partnerId,
                    UserId = userId,
                    SignKeys = new SignKeyPair
                    {
                        Version = SignVersion.A005,
                        Certificate = signCert
                    },
                    AuthKeys = new AuthKeyPair
                    {
                        Version = AuthVersion.X002,
                        Certificate = authCert
                    },
                    CryptKeys = new CryptKeyPair
                    {
                        Version = CryptVersion.E002,
                        Certificate = encCert
                    }
                }
            };
            if (loadStage >= LoadStage.Bank)
            {
                cfg.LoadBank();
            }
            return _ctor(cfg);
        }
    }

    public class EbicsClient : IEbicsClient
    {
        private static readonly ILogger Logger = EbicsLogging.CreateLogger<EbicsClient>();
        private EbicsConfig _config;
        private HttpClient _httpClient;
        private readonly ProtocolHandler _protocolHandler;
        private readonly CommandHandler _commandHandler;

        internal EbicsConfig Config
        {
            get => _config;
            set
            {
                _config = value ?? throw new ArgumentNullException(nameof(Config));
                var handler = new HttpClientHandler { SslProtocols = System.Security.Authentication.SslProtocols.Tls12 };
                _httpClient = new HttpClient(handler) {BaseAddress = new Uri(_config.Address)};
                _commandHandler.Config = value;
                _protocolHandler.Client = _httpClient;
                _commandHandler.ProtocolHandler = _protocolHandler;
            }
        }

        public static EbicsClientFactory Factory()
        {
            return new EbicsClientFactory(x => new EbicsClient {Config = x});
        }

        private EbicsClient()
        {
            _protocolHandler = new ProtocolHandler();
            _commandHandler = new CommandHandler();            
        }

        public HpbResponse HPB(HpbParams p)
        {
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<HpbResponse>(p);
                return resp;
            }
        }

        public PtkResponse PTK(PtkParams p)
        {
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<PtkResponse>(p);
                return resp;
            }
        }

        //public StaResponse STA(StaParams p)
        //{
        //    using (new MethodLogger(Logger))
        //    {
        //        var resp = _commandHandler.Send<StaResponse>(p);
        //        return resp;
        //    }
        //}

        public CctResponse CCT(CctParams p)
        {
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<CctResponse>(p);
                return resp;
            }
        }

        public IniResponse INI(IniParams p)
        {
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<IniResponse>(p);
                return resp;
            }
        }

        public HiaResponse HIA(HiaParams p)
        {
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<HiaResponse>(p);
                return resp;
            }
        }

        public SprResponse SPR(SprParams p)
        {
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<SprResponse>(p);
                return resp;
            }
        }

        public CddResponse CDD(CddParams p)
        {
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<CddResponse>(p);
                return resp;
            }
        }

        public HvzResponse HVZ(HvzParams p)
        {
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<HvzResponse>(p);
                return resp;
            }
        }
        public HvuResponse HVU(HvuParams p)
        {
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<HvuResponse>(p);
                return resp;
            }
        }
        public HvdResponse HVD(HvdParams p)
        {
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<HvdResponse>(p);
                return resp;
            }
        }
        public HvtResponse HVT(HvtParams p)
        {
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<HvtResponse>(p);
                return resp;
            }
        }
        public HtdResponse HTD(HtdParams p)
        {
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<HtdResponse>(p);
                return resp;
            }
        }
        public HpdResponse HPD(HpdParams p)
        {
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<HpdResponse>(p);
                return resp;
            }
        }
        public StaResponse STA(StaParams p)
        {
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<StaResponse>(p);
                return resp;
            }
        }
        public VmkResponse VMK(VmkParams p)
        {
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<VmkResponse>(p);
                return resp;
            }
        }
        public HaaResponse HAA(HaaParams p)
        {
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<HaaResponse>(p);
                return resp;
            }
        }
        public HevResponse HEV(HevParams p)
        {
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<HevResponse>(p);
                return resp;
            }
        }
        public HkdResponse HKD(HkdParams p)
        {
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<HkdResponse>(p);
                return resp;
            }
        }
        public XxcResponse XXC(XxcParams p)
        {
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<XxcResponse>(p);
                return resp;
            }
        }
    }
}