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
using Microsoft.Extensions.Logging;
using NetEbics.Exceptions;
using NetEbics.Parameters;
using NetEbics.Responses;
using ebics = ebicsxml.H004;

namespace NetEbics.Commands
{
//    internal class IniCommand : GenericCommand<IniResponse>
    internal class IniCommand : Command
    {
        private static readonly ILogger s_logger = EbicsLogging.CreateLogger<IniCommand>();

        internal override string OrderType => "INI";
        internal override string OrderAttribute => "DZNNN";
        internal override TransactionType TransactionType => TransactionType.Upload;
        internal override IList<XmlDocument> Requests => CreateRequests();
        internal override XmlDocument InitRequest => null;
        internal override XmlDocument ReceiptRequest => null;

        internal IniParams Params;
        public IniResponse Response=new IniResponse();

        internal override DeserializeResponse Deserialize(string payload)
        {
            var dr = Deserialize_ebicsKeyManagementResponse(payload,out var ebr);
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
                    var userSigData = new ebics.SignaturePubKeyOrderDataType
                    {
                        PartnerID = Config.User.PartnerId,
                        UserID = Config.User.UserId,
                        SignaturePubKeyInfo = new ebics.SignaturePubKeyInfoType
                        {
                            SignatureVersion = "A005",
                            PubKeyValue = new ebics.PubKeyValueType1
                            {
                                TimeStamp = Config.User.SignKeys.TimeStamp.Value,
                                TimeStampSpecified = true,
                                RSAKeyValue = new ebics.RSAKeyValueType
                                {
                                    Modulus = Config.User.SignKeys.Modulus,
                                    Exponent = Config.User.SignKeys.Exponent
                                }
                            }
                        },
                    };
                    var doc = XMLSerializeToDocument(userSigData).OuterXml;
                    s_logger.LogDebug("User signature data:\n{data}", doc);

                    var compressed =
                        Compress(
                            Encoding.UTF8.GetBytes(doc));
                    var req = new ebics.ebicsUnsecuredRequest
                    {
                        header = new ebics.ebicsUnsecuredRequestHeader
                        {
                            @static = new ebics.UnsecuredRequestStaticHeaderType
                            {
                                HostID = Config.User.HostId,
                                PartnerID = Config.User.PartnerId,
                                UserID=Config.User.UserId,
#if DEBUG
                                Product=new ebics.ProductElementType
                                {
                                    InstituteID="BL Banking",
                                    Language="EN",
                                    Value="BL Banking"
                                },
#endif
                                SecurityMedium = Params.SecurityMedium,
                                OrderDetails = new ebics.UnsecuredReqOrderDetailsType
                                {
                                    OrderType = OrderType,
                                    OrderAttribute = OrderAttribute
                                }
                            },
                            mutable = new ebics.EmptyMutableHeaderType(),
                        },
                        body = new ebics.ebicsUnsecuredRequestBody
                        {
                            DataTransfer = new ebics.ebicsUnsecuredRequestBodyDataTransfer
                            {
                                OrderData = new ebics.ebicsUnsecuredRequestBodyDataTransferOrderData
                                {
                                    Value = compressed
                                }
                            }
                        },
                        Version="H004",
                        Revision="1"

                    };

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