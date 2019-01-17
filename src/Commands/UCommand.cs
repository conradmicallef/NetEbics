﻿/*
 * NetEbics -- .NET Core EBICS Client Library
 * (c) Copyright 2018 Bjoern Kuensting
 *
 * This file is subject to the terms and conditions defined in
 * file 'LICENSE.txt', which is part of this source code package.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using NetEbics.Exceptions;
using NetEbics.Handler;
using NetEbics.Parameters;
using NetEbics.Responses;
using ebics = ebicsxml.H004;

namespace NetEbics.Commands
{
    internal abstract class UCommand : Command
    {
        private static readonly ILogger s_logger = EbicsLogging.CreateLogger<UCommand>();
        private readonly byte[] _transactionKey;
        private XmlDocument _initReq;
        private IList<string> _segments;
        private string _transactionId;

        protected abstract XDocument _Params { get; }
        protected abstract string SecurityMedium { get; }

        internal override string OrderAttribute => "OZHNN";
        internal override TransactionType TransactionType => TransactionType.Upload;
        internal override IList<XmlDocument> Requests => CreateUploadRequests(_segments);

        internal override XmlDocument InitRequest
        {
            get
            {
                (_initReq, _segments) = CreateInitRequest();
                return _initReq;
            }
        }

        internal override XmlDocument ReceiptRequest => null;

        public UCommand()
        {
            _transactionKey = CryptoUtils.GetTransactionKey();
            s_logger.LogDebug("Transaction Key: {key}", CryptoUtils.Print(_transactionKey));
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

                    if (dr.Phase == TransactionPhase.Initialisation)
                    {
                        _transactionId = dr.TransactionId;
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


        private XElement CreateUserSigData(XDocument doc)
        {
            return SignData(doc, Config.User);
        }

        private IList<XmlDocument> CreateUploadRequests(IList<string> segments)
        {
            using (new MethodLogger(s_logger))
            {
                try
                {
                    return segments.Select((segment, i) => new ebics.ebicsRequest
                    {
                        Version = "H004",
                        Revision = "1",
                        header = new ebics.ebicsRequestHeader
                        {
                            authenticate = true,
                            @static = new ebics.StaticHeaderType
                            {
                                HostID = Config.User.HostId,
                                ItemsElementName = new ebics.ItemsChoiceType3[] { ebics.ItemsChoiceType3.TransactionID },
                                Items = new string[] { _transactionId }
                            },
                            mutable = new ebics.MutableHeaderType
                            {
                                TransactionPhase = ebics.TransactionPhaseType.Transfer,
                                SegmentNumber = new ebics.MutableHeaderTypeSegmentNumber { Value = (i + 1).ToString(), lastSegment = (i + 1 == segments.Count) }
                            }
                        },
                        body = new ebics.ebicsRequestBody
                        {
                            Items = new object[]{new ebics.DataTransferRequestType{Items=new object[]{new ebics.DataTransferRequestTypeOrderData{
                                Value=Convert.FromBase64String(segment) } } } }
                        },
                    }
                    ).Select(req => Authenticate(req, req.GetType())).ToList();
                }
                catch (EbicsException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new CreateRequestException($"can't create {OrderType} upload requests", ex);
                }
            }
        }

        private (XmlDocument request, IList<string> segments) CreateInitRequest()
        {
            using (new MethodLogger(s_logger))
            {
                try
                {
                    //XNamespace nsEBICS = Namespaces.Ebics;

                    var hvdDoc = _Params;
                    s_logger.LogDebug("Created {OrderType} document:\n{doc}", OrderType, hvdDoc.ToString());

                    var userSigData = CreateUserSigData(hvdDoc);
                    s_logger.LogDebug("Created user signature data:\n{data}", userSigData.ToString());

                    var userSigDataXmlStr = userSigData.ToString(SaveOptions.DisableFormatting);
                    var userSigDataComp = Compress(Encoding.UTF8.GetBytes(userSigDataXmlStr));
                    var userSigDataEnc = EncryptAes(userSigDataComp, _transactionKey);

                    var hvdDocXmlStr = FormatXml(hvdDoc);
                    var hvdDocComp = Compress(Encoding.UTF8.GetBytes(hvdDocXmlStr));
                    var hvdDocEnc = EncryptAes(hvdDocComp, _transactionKey);
                    var hvdDocB64 = Convert.ToBase64String(hvdDocEnc);

                    var segments = Segment(hvdDocB64);

                    s_logger.LogDebug("Number of segments: {segments}", segments.Count);

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
                                    ebics.ItemsChoiceType3.SecurityMedium,
                                    ebics.ItemsChoiceType3.NumSegments
                                },
                                Items = new object[]
                                {
                                    CryptoUtils.GetNonceBinary(),
                                    DateTime.UtcNow,
                                    Config.User.PartnerId,
                                    Config.User.UserId,
                                    Config.StaticHeaderTypeProduct,
                                    new ebics.StaticHeaderOrderDetailsType
                                    {
                                        OrderType=new ebics.StaticHeaderOrderDetailsTypeOrderType{Value=OrderType},
                                        OrderAttribute=(ebics.OrderAttributeType)Enum.Parse(typeof(ebics.OrderAttributeType),this.OrderAttribute),
                                        OrderParams=new ebics.GenericOrderParamsType(),
                                    },
                                    Config.Bank.pubkeydigests,
                                    SecurityMedium,
                                    segments.Count.ToString(),
                                }
                            },
                            mutable = new ebics.MutableHeaderType { TransactionPhase = ebics.TransactionPhaseType.Initialisation }
                        },
                        body = new ebics.ebicsRequestBody
                        {
                            Items = new object[]{
                                new ebics.DataTransferRequestType
                                {
                                    Items=new object[]
                                    {
                                        new ebics.DataTransferRequestTypeDataEncryptionInfo
                                        {
                                            EncryptionPubKeyDigest=new ebics.DataEncryptionInfoTypeEncryptionPubKeyDigest
                                            {
                                                Algorithm=s_digestAlg,
                                                Value=Config.Bank.CryptKeys.Digest,
                                                Version=Config.Bank.CryptKeys.Version.ToString()
                                            },
                                            TransactionKey=EncryptRsa(_transactionKey)
                                        },
                                        new ebics.DataTransferRequestTypeSignatureData
                                        {
                                            Value=userSigDataEnc
                                        }
                                    }
                                }
                            }
                        }
                    };

                    return (request: Authenticate(initReq,typeof(ebics.GenericOrderParamsType)), segments: segments);
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