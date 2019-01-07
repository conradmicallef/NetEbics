/*
 * NetEbics -- .NET Core EBICS Client Library
 * (c) Copyright 2018 Bjoern Kuensting
 *
 * This file is subject to the terms and conditions defined in
 * file 'LICENSE.txt', which is part of this source code package.
 */

using System;
using System.Collections.Generic;
using System.IO;
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
    internal abstract class GenericEbicsDCommand<ResponseType, RequestType> : GenericCommand<EbicsResponseWithDocument<ResponseType>>,IDisposable
    {
        private static readonly ILogger s_logger = EbicsLogging.CreateLogger<GenericCommand<EbicsResponseWithDocument<ResponseType>>>();
        private byte[] _transactionID;

        internal EbicsParams<RequestType> Params { private get; set; }
        internal override TransactionType TransactionType => TransactionType.Download;
        internal override IList<XmlDocument> Requests => null;
        internal override XmlDocument InitRequest => CreateInitRequest();
        internal override XmlDocument ReceiptRequest => CreateReceiptRequest();


        public readonly MemoryStream ms = new MemoryStream();

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
                    var dr = base.Deserialize_ebicsResponse(payload, out var ebr);

                    if (dr.HasError || dr.IsRecoverySync)
                    {
                        return dr;
                    }

                    //do signature validation here
                    var doc = new XmlDocument { PreserveWhitespace = true };
                    doc.LoadXml(payload);
                    //VerifySignature(doc, Config.Bank.AuthKeys.PublicKey);

                    if (dr.Phase == TransactionPhase.Initialisation)
                    {
                        _transactionID = ebr.header.@static.TransactionID;
                    }
                    if (dr.Phase == TransactionPhase.Receipt)
                        return dr;
                    var decryptedOd = DecryptOrderData(ebr.body.DataTransfer);
                    var deflatedOd = Decompress(decryptedOd);
                    ms.Write(deflatedOd);

                    if (dr.LastSegment)
                    {
                        //compute actual received data
                        Response.document = ms;
                    }

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
        private XmlDocument CreateReceiptRequest()
        {
            try
            {
                var receiptReq = new ebics.ebicsRequest
                {
                    Version = "H004",
                    Revision = "1",
                    header = new ebics.ebicsRequestHeader
                    {
                        authenticate = true,
                        @static = new ebics.StaticHeaderType
                        {
                            HostID = Config.User.HostId,
                            ItemsElementName = new ebics.ItemsChoiceType3[]
                            {
                                ebics.ItemsChoiceType3.TransactionID
                            },
                            Items = new object[]
                            {
                                _transactionID
                            },
                        },
                        mutable = new ebics.MutableHeaderType
                        {
                            TransactionPhase = ebics.TransactionPhaseType.Receipt
                        },
                    }
                    ,
                    body = new ebics.ebicsRequestBody
                    {
                        Items = new object[]{
                            new ebics.ebicsRequestBodyTransferReceipt
                    {
                        authenticate=true,
                        ReceiptCode="0"
                    }
                        }
                    }
                };

                return Authenticate(receiptReq);
            }
            catch (EbicsException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CreateRequestException($"can't create receipt request for {OrderType}", ex);
            }
        }


        private XmlDocument CreateInitRequest()
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
                        header = new ebics.ebicsRequestHeader
                        {
                            authenticate = true,
                            @static = new ebics.StaticHeaderType
                            {
                                HostID = Config.User.HostId,
                                ItemsElementName = new ebics.ItemsChoiceType3[]
                                {
                                    ebics.ItemsChoiceType3.Nonce,
                                    ebics.ItemsChoiceType3.Timestamp,
                                    ebics.ItemsChoiceType3.PartnerID,
                                    ebics.ItemsChoiceType3.UserID,
                                    ebics.ItemsChoiceType3.Product,
                                    ebics.ItemsChoiceType3.OrderDetails,
                                    ebics.ItemsChoiceType3.BankPubKeyDigests,
                                    ebics.ItemsChoiceType3.SecurityMedium
                                },
                                Items = new object[]
                                {
                                    CryptoUtils.GetNonceBinary(),
                                    DateTime.UtcNow,
                                    Config.User.PartnerId,
                                    Config.User.UserId,
                                    new ebics.StaticHeaderTypeProduct
                                    {
                                        InstituteID = "BL Banking",
                                        Language = "EN",
                                        Value = "BL Banking"
                                    },
                                    new ebics.StaticHeaderOrderDetailsType
                                    {
                                        OrderType=new ebics.StaticHeaderOrderDetailsTypeOrderType{Value=OrderType},
                                        OrderAttribute=(ebics.OrderAttributeType)Enum.Parse(typeof(ebics.OrderAttributeType),this.OrderAttribute),
                                        OrderParams=Params.ebics,
                                    },
                                    Config.Bank.pubkeydigests,
                                    Params.SecurityMedium,
                                }
                            },
                            mutable = new ebics.MutableHeaderType { TransactionPhase = ebics.TransactionPhaseType.Initialisation },

                        },
                        body = new ebics.ebicsRequestBody() { }
                    };


                    return Authenticate(initReq, Params.ebics?.GetType());
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

        public void Dispose()
        {
            ms.Dispose();
        }
    }
}
