using System;
using System.Linq;
using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Digests;

namespace Peach.Enterprise.Test.Publishers
{
    [TestFixture]
    class WifiPublisherTests
    {
        [Test]
        public void PskTest()
        {
            var password = new byte[] {0x6c, 0x69, 0x6e, 0x6b, 0x73, 0x79, 0x73, 0x35, 0x34, 0x67, 0x68 };
            var salt = new byte[] {0x72, 0x61, 0x64, 0x69, 0x75, 0x73, 0x74, 0x65, 0x73, 0x74 };
            var iterations = 4096;

            var key = new byte[] { 0x2c, 0xd0, 0x86, 0xa1, 0xdc, 0x95, 0xf2, 0xeb, 0xb2, 0xa5, 0x45, 0x6a, 0xb4, 0xa6, 0x1e, 0x62, 0xb8, 0x66, 0x78, 0x46, 0x56, 0x9f, 0x05, 0xbc, 0xc5, 0x75, 0x1a, 0x91, 0x12, 0x4b, 0x97, 0x32 };

            var wpa = new Wpa();
            var my_key = wpa.GeneratePmk(password, salt, iterations);

            Assert.AreEqual(32, my_key.Length);
            Assert.AreEqual(key, my_key);
        }

        [Test]
        public void Ptk512Test()
        {
            var pmk = new byte[] { 0x0d, 0xc0, 0xd6, 0xeb, 0x90, 0x55, 0x5e, 0xd6, 0x41, 0x97, 0x56, 0xb9, 0xa1, 0x5e, 0xc3, 0xe3, 0x20, 0x9b, 0x63, 0xdf, 0x70, 0x7d, 0xd5, 0x08, 0xd1, 0x45, 0x81, 0xf8, 0x98, 0x27, 0x21, 0xaf };
            var aa = new byte[] { 0xa0, 0xa1, 0xa1, 0xa3, 0xa4, 0xa5 };
            var spa = new byte[] { 0xb0, 0xb1, 0xb2, 0xb3, 0xb4, 0xb5 };
            var sNonce = new byte[] { 0xc0, 0xc1, 0xc2, 0xc3, 0xc4, 0xc5, 0xc6, 0xc7, 0xc8, 0xc9, 0xd0, 0xd1, 0xd2, 0xd3, 0xd4, 0xd5, 0xd6, 0xd7, 0xd8, 0xd9, 0xda, 0xdb, 0xdc, 0xdd, 0xde, 0xdf, 0xe0, 0xe1, 0xe2, 0xe3, 0xe4, 0xe5 };

            var aNonce = new byte[] { 0xe0, 0xe1, 0xe2, 0xe3, 0xe4, 0xe5, 0xe6, 0xe7, 0xe8, 0xe9, 0xf0, 0xf1, 0xf2, 0xf3, 0xf4, 0xf5, 0xf6, 0xf7, 0xf8, 0xf9, 0xfa, 0xfb, 0xfc, 0xfd, 0xfe, 0xff, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 };

            var kck = new byte[] { 0x37, 0x9f, 0x98, 0x52, 0xd0, 0x19, 0x92, 0x36, 0xb9, 0x4e, 0x40, 0x7c, 0xe4, 0xc0, 0x0e, 0xc8 };
            var kek = new byte[] { 0x47, 0xc9, 0xed, 0xc0, 0x1c, 0x2c, 0x6e, 0x5b, 0x49, 0x10, 0xca, 0xdd, 0xfb, 0x3e, 0x51, 0xa7 };
            var tk = new byte[] { 0xb2, 0x36, 0x0c, 0x79, 0xe9, 0x71, 0x0f, 0xdd, 0x58, 0xbe, 0xa9, 0x3d, 0xea, 0xf0, 0x65, 0x99 };
            var aMic = new byte[] { 0xdb, 0x98, 0x0a, 0xfb, 0xc2, 0x9c, 0x15, 0x28 };
            var sMic = new byte[] { 0x55, 0x74, 0x0a, 0x6c, 0xe5, 0xae, 0x38, 0x27 };

            var wpa = new Wpa();

            wpa.GeneratePtk(pmk, aa, spa, sNonce, aNonce);

            Assert.AreEqual(32, sNonce.Length);
            Assert.AreEqual(32, aNonce.Length);
            Assert.AreEqual(64, wpa.Length);
            Assert.AreEqual(kck, wpa.Kck);
            Assert.AreEqual(kek, wpa.Kek);
            Assert.AreEqual(tk, wpa.Tk);
            Assert.AreEqual(aMic, wpa.AuthMic);
            Assert.AreEqual(sMic, wpa.SupMic);
        }

        [Test]
        public void Ptk192Test()
        {
            var pmk = new byte[] { 0x0b,0x0b,0x0b,0x0b,0x0b,0x0b,0x0b,0x0b,0x0b,0x0b,0x0b,0x0b,0x0b,0x0b,0x0b,0x0b,0x0b,0x0b,0x0b,0x0b };
            var label = System.Text.Encoding.ASCII.GetBytes("prefix");
            var b = System.Text.Encoding.ASCII.GetBytes("Hi There");
            var expected = new byte[] {0xbc,0xd4,0xc6,0x50,0xb3,0x0b,0x96,0x84,0x95,0x18,0x29,0xe0,0xd7,0x5f,0x9d,0x54,
0xb8,0x62,0x17,0x5e,0xd9,0xf0,0x06,0x06};
            var wpa = new Wpa();

            wpa.PRF(pmk, label, b.ToArray(), 192);

            Assert.AreEqual(expected, wpa.ToArray());
        }

        [Test]
        public void Ptk256Test()
        {
            var pmk = System.Text.Encoding.ASCII.GetBytes("Jefe");
            var label = System.Text.Encoding.ASCII.GetBytes("prefix-2");
            var b = System.Text.Encoding.ASCII.GetBytes("what do ya want for nothing?");
            var expected = new byte[] { 0x47, 0xc4, 0x90, 0x8e, 0x30, 0xc9, 0x47, 0x52, 0x1a, 0xd2, 0x0b, 0xe9, 0x05, 0x34, 0x50, 0xec, 0xbe, 0xa2, 0x3d, 0x3a, 0xa6, 0x04, 0xb7, 0x73, 0x26, 0xd8, 0xb3, 0x82, 0x5f, 0xf7, 0x47, 0x5c };
            var wpa = new Wpa();

            wpa.PRF(pmk, label, b.ToArray(), 256);

            Assert.AreEqual(expected, wpa.ToArray());
        }


        public class Wpa
        {
            MemoryStream ms;
            byte[] pmk;

            public Wpa()
            {
                ms = new MemoryStream();
            }

            public byte[] GeneratePmk(byte[] password, byte[] salt, int iterations)
            {
                var ret = new MemoryStream();

                Pkcs5S2ParametersGenerator gen = new Pkcs5S2ParametersGenerator();

                gen.Init(password, salt, iterations);
                KeyParameter macParameters = (KeyParameter)gen.GenerateDerivedMacParameters(256);

                pmk = macParameters.GetKey();

                return pmk;
            }

            public void GeneratePtk(byte[] pmk, byte[] aa, byte[] spa, byte[] aNonce, byte[] sNonce)
            {

                var label = System.Text.Encoding.ASCII.GetBytes("Pairwise key expansion");
                var b = new MemoryStream();
                b.Write(Min(aa, spa), 0, aa.Length);
                b.Write(Max(aa, spa), 0, aa.Length);
                b.Write(Min(aNonce, sNonce), 0, aNonce.Length);
                b.Write(Max(aNonce, sNonce), 0, aNonce.Length);
                b.Seek(0, SeekOrigin.Begin);

                PRF(pmk, label, b.ToArray(), 512);
            }

            public void GeneratePtk(byte[] aa, byte[] spa, byte[] aNonce, byte[] sNonce)
            {

                var label = System.Text.Encoding.ASCII.GetBytes("Pairwise key expansion");
                var b = new MemoryStream();
                b.Write(Min(aa, spa), 0, aa.Length);
                b.Write(Max(aa, spa), 0, aa.Length);
                b.Write(Min(aNonce, sNonce), 0, aNonce.Length);
                b.Write(Max(aNonce, sNonce), 0, aNonce.Length);
                b.Seek(0, SeekOrigin.Begin);

                PRF(pmk, label, b.ToArray(), 512);
            }

            public byte[] Kck
            {
                get
                {
                    var ret = new byte[16];
                    ms.Seek(0, SeekOrigin.Begin);
                    ms.Read(ret, 0, 16);

                    return ret;
                }
            }

            public int Length
            {
                get
                {
                    return(int) ms.Length;
                }
            }

            public byte[] Kek
            {
                get
                {
                    var ret = new byte[16];
                    ms.Seek(16, SeekOrigin.Begin);
                    ms.Read(ret, 0, 16);

                    return ret;
                }
            }

            public byte[] Tk
            {
                get
                {
                    var ret = new byte[16];
                    ms.Seek(32, SeekOrigin.Begin);
                    ms.Read(ret, 0, 16);

                    return ret;
                }
            }

            public byte[] AuthMic
            {
                get
                {
                    var ret = new byte[8];
                    ms.Seek(48, SeekOrigin.Begin);
                    ms.Read(ret, 0, 8);

                    return ret;
                }
            }

            public byte[] SupMic
            {
                get
                {
                    var ret = new byte[8];
                    ms.Seek(56, SeekOrigin.Begin);
                    ms.Read(ret, 0, 8);

                    return ret;
                }
            }

            public byte[] ToArray()
            {
                ms.Seek(0, SeekOrigin.Begin);
                return ms.ToArray();
            }

            public void PRF(byte[] key, byte[] label, byte[] b, int size)
            {
                var k = new KeyParameter(key);

                HMac mac = new HMac(new Sha1Digest());
                mac.Init(k);

                ms.Seek(0, SeekOrigin.Begin);

                for (byte i = 0; i < (size + 159) / 160; i++)
                {
                    var sha = new byte[mac.GetMacSize()];

                    mac.BlockUpdate(label, 0, label.Length);
                    mac.Update(0x00);
                    mac.BlockUpdate(b, 0, b.Length);
                    mac.Update(i);

                    mac.DoFinal(sha, 0);
                    mac.Reset();

                    ms.Write(sha, 0, sha.Length);
                }

                ms.Seek(0, SeekOrigin.Begin);
                ms.SetLength(size / 8);
            }

            byte[] Max(byte[] a, byte[] b)
            {
                if (a.Length != b.Length)
                    throw new ArgumentException("The two arrays are not the same size.");

                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i] > b[i])
                        return a;
                    if (b[i] > a[i])
                        return b;
                }

                return a;
            }

            byte[] Min(byte[] a, byte[] b)
            {
                if (a.Length != b.Length)
                    throw new ArgumentException("The two arrays are not the same size.");

                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i] < b[i])
                        return a;
                    if (b[i] < a[i])
                        return b;
                }

                return a;
            }

        }
    }
}
