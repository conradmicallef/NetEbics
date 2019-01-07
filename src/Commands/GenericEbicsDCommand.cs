/*
 * NetEbics -- .NET Core EBICS Client Library
 * (c) Copyright 2018 Bjoern Kuensting
 *
 * This file is subject to the terms and conditions defined in
 * file 'LICENSE.txt', which is part of this source code package.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using NetEbics.Config;
using NetEbics.Exceptions;
using NetEbics.Handler;
using NetEbics.Parameters;
using NetEbics.Responses;
using ebics = ebicsxml.H004;

namespace NetEbics.Commands
{
    internal abstract class GenericEbicsDCommand<ResponseType,RequestType> : Command 
    {
        private static readonly ILogger s_logger = EbicsLogging.CreateLogger<GenericCommand<EbicsResponseWithDocument<ResponseType>>>();

        protected EbicsResponseWithDocument<ResponseType> _response;

        internal EbicsResponseWithDocument<ResponseType> Response
        {
            get
            {
                if (_response == null)
                {
                    _response = Activator.CreateInstance<EbicsResponseWithDocument<ResponseType>>();
                }

                return _response;
            }
            set => _response = value;
        }

        //internal override DeserializeResponse Deserialize(string payload)
        //{
        //    using (new MethodLogger(s_logger))
        //    {
        //        var dr = base.Deserialize(payload);
        //        UpdateResponse(this.Response, dr);
        //        return dr;
        //    }
        //}

        private string _transactionId;

        internal EbicsParams<RequestType> Params { private get; set; }
        internal override IList<XmlDocument> Requests => CreateRequests();
        internal override XmlDocument InitRequest => null;
        internal override XmlDocument ReceiptRequest => null;

        public GenericEbicsDCommand()
        {
        }
        protected byte[] DecryptOrderData(ebics.DataTransferResponseType dt)
        {
            using (new MethodLogger(s_logger))
            {
                var encryptedOd = dt.OrderData.Value;
                var version = dt.DataEncryptionInfo.EncryptionPubKeyDigest.Version;

                if (!Enum.TryParse<CryptVersion>(version,
                    out var transKeyEncVersion))
                {
                    throw new DeserializationException(
                        string.Format("Encryption version {0} not supported",
                            version));
                }

                var encryptionPubKeyDigest = dt.DataEncryptionInfo.EncryptionPubKeyDigest.Value;
                var encryptedTransKey = dt.DataEncryptionInfo.TransactionKey;

                var transKey = DecryptRsa(encryptedTransKey);
                var decryptedOd = DecryptAES(encryptedOd, transKey);

                if (!System.Collections.StructuralComparisons.StructuralEqualityComparer.Equals(Config.User.CryptKeys.Digest,
                    encryptionPubKeyDigest))
                {
                    throw new DeserializationException("Wrong digest in xml");
                }

                return decryptedOd;
            }
        }
        internal override DeserializeResponse Deserialize(string payload)
        {
            try
            {
                using (new MethodLogger(s_logger))
                {
                    var dr = base.Deserialize_ebicsResponse(payload,out var ebr);
                    //var doc = XDocument.Parse(payload);

                    if (dr.HasError || dr.IsRecoverySync)
                    {
                        return dr;
                    }

                    if (dr.Phase == TransactionPhase.Initialisation)
                    {
                        _transactionId = dr.TransactionId;
                    }
                    var decryptedOd = DecryptOrderData(ebr.body.DataTransfer);
                    var deflatedOd = Decompress(decryptedOd);
                    var strResp = Encoding.UTF8.GetString(deflatedOd);
                    Response.document = XDocument.Parse(strResp);

                    return dr;
                }
            }
            catch (EbicsException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DeserializationException($"Can't deserialize {OrderType} response", ex, payload);
            }
        }
        private string FormatXml(XDocument doc)
        {
            var xmlStr = doc.ToString(SaveOptions.DisableFormatting);
            xmlStr = xmlStr.Replace("\n", "");
            xmlStr = xmlStr.Replace("\r", "");
            xmlStr = xmlStr.Replace("\t", "");
            return xmlStr;
        }

        private XElement CreateUserSigData(XDocument doc)
        {
            return SignData(doc, Config.User);
        }

        private List<XmlDocument> CreateRequests()
        {
            using (new MethodLogger(s_logger))
            {
                try
                {
                    XNamespace nsEBICS = Namespaces.Ebics;

                    var initReq = new ebics.ebicsRequest
                    {
                        Version = "H004",
                        Revision = "1",
                        header=new ebics.ebicsRequestHeader
                        {
                            authenticate=true,
                            @static=new ebics.StaticHeaderType
                            {
                                HostID=Config.User.HostId,
                                ItemsElementName=new ebics.ItemsChoiceType3[]
                                {
                                    ebics.ItemsChoiceType3.Nonce,
                                    ebics.ItemsChoiceType3.Timestamp,
                                    ebics.ItemsChoiceType3.PartnerID,
                                    ebics.ItemsChoiceType3.UserID,
                                    ebics.ItemsChoiceType3.SecurityMedium,
                                    
                                    ebics.ItemsChoiceType3.OrderDetails,
                                    //ebics.ItemsChoiceType3.BankPubKeyDigests
                                },
                                Items=new object[]
                                {
                                    CryptoUtils.GetNonceBinary(),
                                    DateTime.UtcNow,
                                    Config.User.PartnerId,
                                    Config.User.UserId,
                                    Params.SecurityMedium,
                                   
                                    new ebics.StaticHeaderOrderDetailsType
                                    {
                                        OrderType=new ebics.StaticHeaderOrderDetailsTypeOrderType{Value=OrderType},
                                        OrderAttribute=(ebics.OrderAttributeType)Enum.Parse(typeof(ebics.OrderAttributeType),this.OrderAttribute),
                                        OrderParams=Params.ebics,
                                    },
                                    //Config.Bank.pubkeydigests
                                }
                            },
                            mutable=new ebics.MutableHeaderType { TransactionPhase=ebics.TransactionPhaseType.Initialisation}
                        },
                    };


                    return new List<XmlDocument> { Authenticate(initReq,initReq.GetType()) };
                }
                catch (EbicsException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new CreateRequestException($"can't create {OrderType} init request", ex);
                }
            }
        }
    }
}
