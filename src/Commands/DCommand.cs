﻿/*
 * NetEbics -- .NET Core EBICS Client Library
 * (c) Copyright 2018 Bjoern Kuensting
 *
 * This file is subject to the terms and conditions defined in
 * file 'LICENSE.txt', which is part of this source code package.
 */

using System;
using System.Collections.Generic;
using System.IO;
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
    internal abstract class DCommand: Command
    {
        private static readonly ILogger s_logger = EbicsLogging.CreateLogger<DCommand>();
        private byte[] _transactionID;
        private int _numSegments;
        private int _initSegment;
        private bool _initLastSegment;
        private string[] _orderData;

        protected abstract object _Params {  get; }
        protected abstract string SecurityMedium { get; }

        internal override string OrderAttribute => "DZHNN";
        internal override TransactionType TransactionType => TransactionType.Download;
        internal override IList<XmlDocument> Requests => CreateRequests();
        internal override XmlDocument InitRequest => CreateInitRequest();
        internal override XmlDocument ReceiptRequest => CreateReceiptRequest();

        protected string ResponseData;

        public DCommand()
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

                    switch (dr.Phase)
                    {
                        case TransactionPhase.Initialisation:
                            _transactionID = ebr.header.@static.TransactionID;
                            _numSegments = dr.NumSegments;
                            _initSegment = dr.SegmentNumber;
                            _initLastSegment = dr.LastSegment;
                            _orderData = new string[_numSegments];
                            _orderData[dr.SegmentNumber - 1] =
                                Encoding.UTF8.GetString(Decompress(DecryptOrderData(ebr.body.DataTransfer)));
                            ResponseData = string.Join("", _orderData);
                            break;
                        case TransactionPhase.Transfer:
                            _orderData[dr.SegmentNumber - 1] =
                                Encoding.UTF8.GetString(Decompress(DecryptOrderData(ebr.body.DataTransfer)));
                            ResponseData = string.Join("", _orderData);
                            break;

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

        private IList<XmlDocument> CreateRequests()
        {
            using (new MethodLogger(s_logger))
            {
                try
                {
                    if (_initLastSegment)
                    {
                        s_logger.LogDebug("lastSegment is {lastSegment}. Not creating any transfer requests",
                            _initLastSegment);
                        return null;
                    }

                    var reqs = new List<XmlDocument>();

                    for (var i = 1; i < _numSegments; i++)
                    {
                        s_logger.LogDebug("Creating transfer request {no}", i);
                        var req = new ebics.ebicsRequest
                        {
                            Version = "H004",
                            Revision = "1",
                            header = new ebics.ebicsRequestHeader
                            {
                                @static = new ebics.StaticHeaderType
                                {
                                    HostID = Config.User.HostId,
                                    ItemsElementName = new ebics.ItemsChoiceType3[] { ebics.ItemsChoiceType3.TransactionID },
                                    Items = new object[] { _transactionID }
                                },
                                mutable = new ebics.MutableHeaderType
                                {
                                    TransactionPhase = ebics.TransactionPhaseType.Transfer,
                                    SegmentNumber=new ebics.MutableHeaderTypeSegmentNumber
                                    {
                                        Value = (i + _initSegment).ToString(),
                                        lastSegment = i + _initSegment == _numSegments
                                    }
                                },
                            },
                            body = new ebics.ebicsRequestBody
                            {
                            }
                        };

                        reqs.Add(Authenticate(req));
                    }

                    return reqs;
                }
                catch (EbicsException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new CreateRequestException($"can't create {OrderType} requests", ex);
                }
            }
        }


        private XmlDocument CreateInitRequest()
        {
            using (new MethodLogger(s_logger))
            {
                try
                {
                    //XNamespace nsEBICS = Namespaces.Ebics;

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
                                        OrderParams=_Params,
                                    },
                                    Config.Bank.pubkeydigests,
                                    SecurityMedium,
                                }
                            },
                            mutable = new ebics.MutableHeaderType { TransactionPhase = ebics.TransactionPhaseType.Initialisation },

                        },
                        body = new ebics.ebicsRequestBody() { }
                    };


                    return Authenticate(initReq, _Params?.GetType());
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
