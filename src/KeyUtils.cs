﻿/*
 * NetEbics -- .NET Core EBICS Client Library
 * (c) Copyright 2018 Bjoern Kuensting
 *
 * This file is subject to the terms and conditions defined in
 * file 'LICENSE.txt', which is part of this source code package.
 */

using System;
using System.IO;
using NetEbics.Letters;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;

namespace NetEbics
{
    public static class KeyUtils
    {
        public static string MakeDN(string name, string email = null, string country = null, string organisation = null)
        {
            string s = "CN=" + name;
            if (!string.IsNullOrWhiteSpace(country))
                s += ",C=" + country.ToUpper();
            if (!string.IsNullOrWhiteSpace(organisation))
                s += ",O=" + organisation;
            if (!string.IsNullOrWhiteSpace(email))
                s += ",E=" + email;
            return s;
        }
        public enum KeyType
        {
            Signature=0,
            Authentication=1,
            Encryption=2
        }
        //TODO change to functors
        public static X509Certificate CreateX509Certificate2(AsymmetricCipherKeyPair kp, string issuer, DateTime notBefore, DateTime notAfter, KeyType keyUsage, string filename, string password)
        {
            var random = new SecureRandom();
            var sf = new Asn1SignatureFactory(HashAlg.SHA256withRSA.ToString(), kp.Private, random);

            var gen = new X509V3CertificateGenerator();
            var serial = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
            gen.SetSerialNumber(serial);
            var subject = new X509Name(issuer);
            gen.SetIssuerDN(subject);
            gen.SetSubjectDN(subject);
            gen.SetNotBefore(notBefore);
            gen.SetNotAfter(notAfter);
            gen.SetPublicKey(kp.Public);
            gen.AddExtension(X509Extensions.BasicConstraints, false, new BasicConstraints(true));
            var subjectKeyIdentifier = new SubjectKeyIdentifier(SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(kp.Public));
            gen.AddExtension(X509Extensions.SubjectKeyIdentifier, false, subjectKeyIdentifier);
            var authorityKeyIdentifier = new AuthorityKeyIdentifier(SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(kp.Public), new GeneralNames(new GeneralName(subject)), serial);
            gen.AddExtension(X509Extensions.AuthorityKeyIdentifier, false, authorityKeyIdentifier);
            ////gen.AddExtension(X509Extensions.ExtendedKeyUsage, false, new ExtendedKeyUsage(KeyPurposeID.IdKPEmailProtection));

            switch (keyUsage)
            {
                case KeyType.Signature://signature
                    gen.AddExtension(X509Extensions.KeyUsage, false, new KeyUsage(X509KeyUsage.NonRepudiation)); break;
                case KeyType.Authentication://authentication
                    gen.AddExtension(X509Extensions.KeyUsage, false, new KeyUsage(X509KeyUsage.DigitalSignature)); break;
                case KeyType.Encryption://encryption
                    gen.AddExtension(X509Extensions.KeyUsage, false, new KeyUsage(X509KeyUsage.KeyEncipherment | X509KeyUsage.KeyAgreement)); break;
                default:
                    gen.AddExtension(X509Extensions.KeyUsage, false, new KeyUsage(X509KeyUsage.KeyEncipherment | X509KeyUsage.DigitalSignature)); break;

            }

            var bouncyCert = gen.Generate(sf);
            bouncyCert.CheckValidity();
            bouncyCert.Verify(kp.Public);

            var store = new Pkcs12Store();
            var certificateEntry = new X509CertificateEntry(bouncyCert);
            store.SetCertificateEntry(bouncyCert.SubjectDN.ToString(), certificateEntry);
            store.SetKeyEntry(bouncyCert.SubjectDN.ToString(), new AsymmetricKeyEntry(kp.Private), new[] { certificateEntry });
            using (var sw = File.Create(filename))
                store.Save(sw, password.ToCharArray(), random);
            return bouncyCert;
        }

        public static AsymmetricCipherKeyPair GenerateRSAKeyPair(int strength)
        {
            var gen = GeneratorUtilities.GetKeyPairGenerator("RSA");
            gen.Init(new KeyGenerationParameters(new SecureRandom(), strength));
            return gen.GenerateKeyPair();
        }

        public static X509Certificate Generate(string filename, KeyType type, string email, string password)
        {
            var kp = GenerateRSAKeyPair(2048);
            using (TextWriter sw = new StreamWriter(filename + ".key"))
            {
                var pw = new PemWriter(sw);
                pw.WriteObject(kp, "AES-256-CBC", password.ToCharArray(), new SecureRandom());
                sw.Flush();
            }
            var cert = CreateX509Certificate2(kp, email, DateTime.Now, DateTime.Now.AddYears(10), type, filename + ".p12", password);
            using (TextWriter sw = new StreamWriter(filename + ".cer"))
            {
                var pw = new PemWriter(sw);
                pw.WriteObject(cert);
                sw.Flush();
            }
            return cert;
        }
        //Todo CHANGE TO FUNCTORS
        public static String Letter<T>(string hostId, string bankName, string userId, string username, string partnerId, X509Certificate certificate, string filename) where T : Letter, new()
        {
            var letter = new T() { hostId = hostId, bankName = bankName, userId = userId, username = username, partnerId = partnerId };
            letter.Build(certificate.GetEncoded(), null, null);
            File.WriteAllText(filename, letter.sb.ToString());
            return letter.sb.ToString();
        }
        //Todo CHANGE TO FUNCTORS
        public static String LetterDE<T>(string hostId, string bankName, string userId, string username, string partnerId, X509Certificate certificate, string filename) where T : Letter, new()
        {
            var letter = new T() { hostId = hostId, bankName = bankName, userId = userId, username = username, partnerId = partnerId };
            var rsa = (Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters)certificate.GetPublicKey();

            letter.Build(certificate: null, modulus: rsa.Modulus.ToByteArray(), exponent: rsa.Exponent.ToByteArray());
            File.WriteAllText(filename, letter.sb.ToString());
            return letter.sb.ToString();
        }

        public static System.Security.Cryptography.X509Certificates.X509Certificate2 ReadCert(string fname, string pw)
        {
            return new System.Security.Cryptography.X509Certificates.X509Certificate2(fname, pw);
        }

        public static void GenerateDE(string directory, string dn, string password, string hostId, string bankName, string userId, string username, string partnerId)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            var sign = Generate(directory + "/sign", KeyType.Signature, dn, password);
            var auth = Generate(directory + "/auth", KeyType.Authentication, dn, password);
            var enc = Generate(directory + "/enc", KeyType.Encryption, dn, password);
            var sign2 = ReadCert(directory + "/sign.p12", password);
            var auth2 = ReadCert(directory + "/auth.p12", password);
            var enc2 = ReadCert(directory + "/enc.p12", password);
            LetterDE<A005Letter>(hostId, bankName, userId, username, partnerId, sign, directory + "/sign.txt");
            LetterDE<E002Letter>(hostId, bankName, userId, username, partnerId, enc, directory + "/enc.txt");
            LetterDE<X002Letter>(hostId, bankName, userId, username, partnerId, auth, directory + "/auth.txt");
        }



        /*        public static AsymmetricCipherKeyPair ReadPrivateKeyFromPem(string fileName)
                {
                    using (var sr = new StringReader(File.ReadAllText(fileName).Trim()))
                    {
                        var pr = new PemReader(sr);
                        return (AsymmetricCipherKeyPair) pr.ReadObject();
                    }
                }

                public static X509Certificate2 CreateX509Certificate2(AsymmetricCipherKeyPair kp)
                {
                    var random = new SecureRandom();
                    var sf = new Asn1SignatureFactory(HashAlg.SHA256withRSA.ToString(), kp.Private, random);

                    var gen = new X509V3CertificateGenerator();
                    gen.SetSerialNumber(BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random));
                    var subject = new X509Name("CN=" + "ebics.org");
                    gen.SetSubjectDN(subject);
                    gen.SetIssuerDN(subject);
                    var notBefore = DateTime.UtcNow.Date;
                    var notAfter = notBefore.AddYears(10);
                    gen.SetNotBefore(notBefore);
                    gen.SetNotAfter(notAfter); 
                    gen.SetPublicKey(kp.Public); 
                    var bouncyCert = gen.Generate(sf);            

                    var authorityKeyIdentifier = new AuthorityKeyIdentifier(SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(kp.Public), new GeneralNames(new GeneralName(subject)), bouncyCert.SerialNumber);
                    gen.AddExtension(X509Extensions.AuthorityKeyIdentifier.Id, false, authorityKeyIdentifier);
                    var subjectKeyIdentifier = new SubjectKeyIdentifier(SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(kp.Public));
                    gen.AddExtension(X509Extensions.SubjectKeyIdentifier.Id, false, subjectKeyIdentifier);
                    gen.AddExtension(X509Extensions.BasicConstraints.Id, true, new BasicConstraints(false));

                    var store = new Pkcs12Store();
                    var certificateEntry = new X509CertificateEntry(bouncyCert);
                    store.SetCertificateEntry(bouncyCert.SubjectDN.ToString(), certificateEntry);
                    store.SetKeyEntry(bouncyCert.SubjectDN.ToString(), new AsymmetricKeyEntry(kp.Private), new[] { certificateEntry });
                    const string pwd = "password";
                    var stream = new MemoryStream();
                    store.Save(stream, pwd.ToCharArray(), random);
                    var msCert = new X509Certificate2(stream.ToArray(), pwd, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

                    return msCert;
                }

                public static AsymmetricCipherKeyPair GenerateRSAKeyPair(int strength)
                {
                    var gen = GeneratorUtilities.GetKeyPairGenerator("RSA");
                    gen.Init(new KeyGenerationParameters(new SecureRandom(), strength));
                    return gen.GenerateKeyPair();            
                }*/


    }
}