﻿/*
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
using NetEbics.Exceptions;
using NetEbics.Parameters;
using NetEbics.Responses;

namespace NetEbics.Commands
{
    internal class HiaCommand : GenericCommand<HiaResponse>
    {
        private static readonly ILogger s_logger = EbicsLogging.CreateLogger<HiaCommand>();
        
        internal HiaParams Params { private get; set; }
        internal override string OrderType => "HIA";
        internal override string OrderAttribute => "DZNNN";
        internal override TransactionType TransactionType => TransactionType.Upload;
        internal override IList<XmlDocument> Requests => CreateRequests();
        internal override XmlDocument InitRequest => null;
        internal override XmlDocument ReceiptRequest => null;

        internal override DeserializeResponse Deserialize(string payload)
        {
            var dr=Deserialize_ebicsKeyManagementResponse(payload);
            UpdateResponse(Response, dr);
            return dr;
        }

        private IList<XmlDocument> CreateRequests()
        {
            using (new MethodLogger(s_logger))
            {
                try
                {
                    var reqs = new List<XmlDocument>();
                    var h = new ebicsxml.H004.HIARequestOrderDataType
                    {
                        PartnerID = Config.User.PartnerId,
                        UserID = Config.User.UserId,
                        AuthenticationPubKeyInfo = new ebicsxml.H004.AuthenticationPubKeyInfoType
                        {
                            AuthenticationVersion = "A002",
                            PubKeyValue = new ebicsxml.H004.PubKeyValueType
                            {
                                TimeStamp = DateTime.UtcNow,
                                TimeStampSpecified = true,
                                RSAKeyValue = new ebicsxml.H004.RSAKeyValueType
                                {
                                    Modulus = Config.User.AuthKeys.Modulus,
                                    Exponent = Config.User.AuthKeys.Exponent
                                }
                            }
                        },
                        EncryptionPubKeyInfo = new ebicsxml.H004.EncryptionPubKeyInfoType
                        {
                            EncryptionVersion = "E002",
                            PubKeyValue = new ebicsxml.H004.PubKeyValueType
                            {
                                TimeStamp = DateTime.UtcNow,
                                TimeStampSpecified = true,
                                RSAKeyValue = new ebicsxml.H004.RSAKeyValueType
                                {
                                    Modulus = Config.User.CryptKeys.Modulus,
                                    Exponent = Config.User.CryptKeys.Exponent
                                }
                            }
                        }
                    };

                    //var hiaOrderData = new HiaRequestOrderData
                    //{
                    //    Namespaces = Namespaces,
                    //    PartnerId = Config.User.PartnerId,
                    //    UserId = Config.User.UserId,
                    //    AuthInfo = new AuthenticationPubKeyInfo
                    //    {
                    //        Namespaces = Namespaces,
                    //        AuthKeys = Config.User.AuthKeys,
                    //        UseEbicsDefaultNamespace = true
                    //    },
                    //    EncInfo = new EncryptionPubKeyInfo
                    //    {
                    //        Namespaces = Namespaces,
                    //        CryptKeys = Config.User.CryptKeys,
                    //        UseEbicsDefaultNamespace = true
                    //    }
                    //};

                    var compressed =
                        Compress(Encoding.UTF8.GetBytes(
                            XMLSerializeToDocument(h).OuterXml));
                    var b64Encoded = Convert.ToBase64String(compressed);

                    var req = new ebicsxml.H004.ebicsUnsecuredRequest
                    {
                        header=new ebicsxml.H004.ebicsUnsecuredRequestHeader
                        {
                            @static=new ebicsxml.H004.UnsecuredRequestStaticHeaderType
                            {
                                HostID=Config.User.HostId,
                                PartnerID=Config.User.PartnerId,
                                UserID=Config.User.UserId,
                                SecurityMedium=Params.SecurityMedium,
                                OrderDetails=new ebicsxml.H004.UnsecuredReqOrderDetailsType
                                {
                                    OrderType=OrderType,
                                    OrderAttribute=OrderAttribute
                                }
                            },
                            mutable=new ebicsxml.H004.EmptyMutableHeaderType { },
                        },
                        body=new ebicsxml.H004.ebicsUnsecuredRequestBody
                        {
                            DataTransfer=new ebicsxml.H004.ebicsUnsecuredRequestBodyDataTransfer
                            {
                                OrderData=new ebicsxml.H004.ebicsUnsecuredRequestBodyDataTransferOrderData
                                {
                                    Value=compressed
                                }
                            }
                            
                        },
                        Version="H004",
                        Revision="1"
                    };
                    //var req = new EbicsUnsecuredRequest
                    //{
                    //    StaticHeader = new StaticHeader
                    //    {
                    //        HostId = Config.User.HostId,
                    //        PartnerId = Config.User.PartnerId,
                    //        UserId = Config.User.UserId,
                    //        SecurityMedium = Params.SecurityMedium,
                    //        Namespaces = Namespaces,
                    //        OrderDetails = new OrderDetails
                    //        {
                    //            OrderType = OrderType,
                    //            OrderAttribute = OrderAttribute,
                    //            Namespaces = Namespaces
                    //        }
                    //    },
                    //    MutableHeader = new MutableHeader
                    //    {
                    //        Namespaces = Namespaces
                    //    },
                    //    Body = new Body
                    //    {
                    //        Namespaces = Namespaces,
                    //        DataTransfer = new DataTransfer
                    //        {
                    //            OrderData = b64Encoded,
                    //            Namespaces = Namespaces
                    //        }
                    //    },
                    //    Namespaces = Namespaces,
                    //    Version = Config.Version,
                    //    Revision = Config.Revision,
                    //};

                    reqs.Add(XMLSerializeToDocument(req));
                    return reqs;
                }
                catch (EbicsException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new CreateRequestException($"can't create requests for {OrderType}", ex);
                }
            }
        }
    }
}