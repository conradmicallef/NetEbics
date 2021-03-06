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
using Microsoft.Extensions.Logging;
using NetEbics.Exceptions;
using NetEbics.Parameters;
using NetEbics.Responses;
using ebics = ebicsxml.H004;

namespace NetEbics.Commands
{
    internal class HiaCommand : Command
    {
        private static readonly ILogger s_logger = EbicsLogging.CreateLogger<HiaCommand>();
        
        internal override string OrderType => "HIA";
        internal override string OrderAttribute => "DZNNN";
        internal override TransactionType TransactionType => TransactionType.Upload;
        internal override IList<XmlDocument> Requests => CreateRequests();
        internal override XmlDocument InitRequest => null;
        internal override XmlDocument ReceiptRequest => null;

        internal HiaParams Params;
        public HiaResponse Response=new HiaResponse();


        internal override DeserializeResponse Deserialize(string payload)
        {
            var dr=Deserialize_ebicsKeyManagementResponse(payload,out var ebr);
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
                    var h = new ebics.HIARequestOrderDataType
                    {
                        PartnerID = Config.User.PartnerId,
                        UserID = Config.User.UserId,
                        AuthenticationPubKeyInfo = new ebics.AuthenticationPubKeyInfoType
                        {
                            AuthenticationVersion = "X002",
                            PubKeyValue = Config.User.AuthKeys.PubKeyValueType,
                        },
                        EncryptionPubKeyInfo = new ebics.EncryptionPubKeyInfoType
                        {
                            EncryptionVersion = "E002",
                            PubKeyValue = Config.User.CryptKeys.PubKeyValueType,
                        }
                    };

                    var compressed =
                        Compress(Encoding.UTF8.GetBytes(
                            XMLSerializeToDocument(h).OuterXml));
                    var b64Encoded = Convert.ToBase64String(compressed);

                    var req = new ebics.ebicsUnsecuredRequest
                    {
                        header=new ebics.ebicsUnsecuredRequestHeader
                        {
                            @static=new ebics.UnsecuredRequestStaticHeaderType
                            {
                                HostID=Config.User.HostId,
                                PartnerID=Config.User.PartnerId,
                                UserID=Config.User.UserId,
                                Product=Config.ProductElementType,
                                SecurityMedium = Params.SecurityMedium,
                                OrderDetails=new ebics.UnsecuredReqOrderDetailsType
                                {
                                    OrderType=OrderType,
                                    OrderAttribute=OrderAttribute
                                }
                            },
                            mutable=new ebics.EmptyMutableHeaderType { },
                        },
                        body=new ebics.ebicsUnsecuredRequestBody
                        {
                            DataTransfer=new ebics.ebicsUnsecuredRequestBodyDataTransfer
                            {
                                OrderData=new ebics.ebicsUnsecuredRequestBodyDataTransferOrderData
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