/*
Copyright 2014 Calvary Chapel of Melbourne, Inc.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CCM.Volunteer.ApprovalProcess.Core.Cryptography
{

    #region " Hash"

    /// <summary> 
    /// Hash functions are fundamental to modern cryptography. These functions map binary 
    /// strings of an arbitrary length to small binary strings of a fixed length, known as 
    /// hash values. A cryptographic hash function has the property that it is computationally 
    /// infeasible to find two distinct inputs that hash to the same value. Hash functions 
    /// are commonly used with digital signatures and for data integrity. 
    /// </summary> 
    public class Hash
    {

        /// <summary> 
        /// Type of hash; some are security oriented, others are fast and simple 
        /// </summary> 
        public enum Provider
        {
            /// <summary> 
            /// Cyclic Redundancy Check provider, 32-bit 
            /// </summary> 
            CRC32,
            /// <summary> 
            /// Secure Hashing Algorithm provider, SHA-1 variant, 160-bit 
            /// </summary> 
            SHA1,
            /// <summary> 
            /// Secure Hashing Algorithm provider, SHA-2 variant, 256-bit 
            /// </summary> 
            SHA256,
            /// <summary> 
            /// Secure Hashing Algorithm provider, SHA-2 variant, 384-bit 
            /// </summary> 
            SHA384,
            /// <summary> 
            /// Secure Hashing Algorithm provider, SHA-2 variant, 512-bit 
            /// </summary> 
            SHA512,
            /// <summary> 
            /// Message Digest algorithm 5, 128-bit 
            /// </summary> 
            MD5
        }

        private HashAlgorithm _Hash;
        private Data _HashValue = new Data();

        private Hash()
        {
        }

        /// <summary> 
        /// Instantiate a new hash of the specified type 
        /// </summary> 
        public Hash(Provider p)
        {
            switch (p)
            {
                case Provider.CRC32:
                    _Hash = new CRC32();
                    break;
                case Provider.MD5:
                    _Hash = new MD5CryptoServiceProvider();
                    break;
                case Provider.SHA1:
                    _Hash = new SHA1Managed();
                    break;
                case Provider.SHA256:
                    _Hash = new SHA256Managed();
                    break;
                case Provider.SHA384:
                    _Hash = new SHA384Managed();
                    break;
                case Provider.SHA512:
                    _Hash = new SHA512Managed();
                    break;
            }
        }

        /// <summary> 
        /// Returns the previously calculated hash 
        /// </summary> 
        public Data Value
        {
            get { return _HashValue; }
        }

        /// <summary> 
        /// Calculates hash on a stream of arbitrary length 
        /// </summary> 
        public Data Calculate(ref System.IO.Stream s)
        {
            _HashValue.Bytes = _Hash.ComputeHash(s);
            return _HashValue;
        }

        /// <summary> 
        /// Calculates hash for fixed length <see cref="Data"/> 
        /// </summary> 
        public Data Calculate(Data d)
        {
            return CalculatePrivate(d.Bytes);
        }

        /// <summary> 
        /// Calculates hash for a string with a prefixed salt value. 
        /// A "salt" is random data prefixed to every hashed value to prevent 
        /// common dictionary attacks. 
        /// </summary> 
        public Data Calculate(Data d, Data salt)
        {
            byte[] nb = new byte[d.Bytes.Length + salt.Bytes.Length];
            salt.Bytes.CopyTo(nb, 0);
            d.Bytes.CopyTo(nb, salt.Bytes.Length);
            return CalculatePrivate(nb);
        }

        /// <summary> 
        /// Calculates hash for an array of bytes 
        /// </summary> 
        private Data CalculatePrivate(byte[] b)
        {
            _HashValue.Bytes = _Hash.ComputeHash(b);
            return _HashValue;
        }

        #region " CRC32 HashAlgorithm"
        private class CRC32 : HashAlgorithm
        {

            private uint result = (uint)0xffffffff;

            protected override void HashCore(byte[] array, int ibStart, int cbSize)
            {
                uint lookup = 0;
                for (int i = ibStart; i <= cbSize - 1; i++)
                {
                    lookup = (result & 0xff) ^ array[i];
                    result = (uint)((result & 0xffffff00) / 0x100) & 0xffffff;
                    result = result ^ crcLookup[lookup];
                }
            }

            protected override byte[] HashFinal()
            {
                byte[] b = BitConverter.GetBytes(result);
                Array.Reverse(b);
                return b;
            }

            public override void Initialize()
            {
                result = 0xffffffff;
            }

            private uint[] crcLookup = { 0x0, (uint)0x77073096, (uint)0xee0e612c, (uint)0x990951ba, (uint)0x76dc419, (uint)0x706af48f, (uint)0xe963a535, (uint)0x9e6495a3, (uint)0xedb8832, (uint)0x79dcb8a4, (uint)
            0xe0d5e91e, (uint)0x97d2d988, (uint)0x9b64c2b, (uint)0x7eb17cbd, (uint)0xe7b82d07, (uint)0x90bf1d91, (uint)0x1db71064, (uint)0x6ab020f2, (uint)0xf3b97148, (uint)0x84be41de, (uint)
            0x1adad47d, (uint)0x6ddde4eb, (uint)0xf4d4b551, (uint)0x83d385c7, (uint)0x136c9856, (uint)0x646ba8c0, (uint)0xfd62f97a, (uint)0x8a65c9ec, (uint)0x14015c4f, (uint)0x63066cd9, (uint)
            0xfa0f3d63, (uint)0x8d080df5, (uint)0x3b6e20c8, (uint)0x4c69105e, (uint)0xd56041e4, (uint)0xa2677172, (uint)0x3c03e4d1, (uint)0x4b04d447, (uint)0xd20d85fd, (uint)0xa50ab56b, (uint)
            0x35b5a8fa, (uint)0x42b2986c, (uint)0xdbbbc9d6, (uint)0xacbcf940, (uint)0x32d86ce3, (uint)0x45df5c75, (uint)0xdcd60dcf, (uint)0xabd13d59, (uint)0x26d930ac, (uint)0x51de003a, (uint)
            0xc8d75180, (uint)0xbfd06116, (uint)0x21b4f4b5, (uint)0x56b3c423, (uint)0xcfba9599, (uint)0xb8bda50f, (uint)0x2802b89e, (uint)0x5f058808, (uint)0xc60cd9b2, (uint)0xb10be924, (uint)
            0x2f6f7c87, (uint)0x58684c11, (uint)0xc1611dab, (uint)0xb6662d3d, (uint)0x76dc4190, (uint)0x1db7106, (uint)0x98d220bc, (uint)0xefd5102a, (uint)0x71b18589, (uint)0x6b6b51f, (uint)
            0x9fbfe4a5, (uint)0xe8b8d433, (uint)0x7807c9a2, (uint)0xf00f934, (uint)0x9609a88e, (uint)0xe10e9818, (uint)0x7f6a0dbb, (uint)0x86d3d2d, (uint)0x91646c97, (uint)0xe6635c01, (uint)
            0x6b6b51f4, (uint)0x1c6c6162, (uint)0x856530d8, (uint)0xf262004e, (uint)0x6c0695ed, (uint)0x1b01a57b, (uint)0x8208f4c1, (uint)0xf50fc457, (uint)0x65b0d9c6, (uint)0x12b7e950, (uint)
            0x8bbeb8ea, (uint)0xfcb9887c, (uint)0x62dd1ddf, (uint)0x15da2d49, (uint)0x8cd37cf3, (uint)0xfbd44c65, (uint)0x4db26158, (uint)0x3ab551ce, (uint)0xa3bc0074, (uint)0xd4bb30e2, (uint)
            0x4adfa541, (uint)0x3dd895d7, (uint)0xa4d1c46d, (uint)0xd3d6f4fb, (uint)0x4369e96a, (uint)0x346ed9fc, (uint)0xad678846, (uint)0xda60b8d0, (uint)0x44042d73, (uint)0x33031de5, (uint)
            0xaa0a4c5f, (uint)0xdd0d7cc9, (uint)0x5005713c, (uint)0x270241aa, (uint)0xbe0b1010, (uint)0xc90c2086, (uint)0x5768b525, (uint)0x206f85b3, (uint)0xb966d409, (uint)0xce61e49f, (uint)
            0x5edef90e, (uint)0x29d9c998, (uint)0xb0d09822, (uint)0xc7d7a8b4, (uint)0x59b33d17, (uint)0x2eb40d81, (uint)0xb7bd5c3b, (uint)0xc0ba6cad, (uint)0xedb88320, (uint)0x9abfb3b6, (uint)
            0x3b6e20c, (uint)0x74b1d29a, (uint)0xead54739, (uint)0x9dd277af, (uint)0x4db2615, (uint)0x73dc1683, (uint)0xe3630b12, (uint)0x94643b84, (uint)0xd6d6a3e, (uint)0x7a6a5aa8, (uint)
            0xe40ecf0b, (uint)0x9309ff9d, (uint)0xa00ae27, (uint)0x7d079eb1, (uint)0xf00f9344, (uint)0x8708a3d2, (uint)0x1e01f268, (uint)0x6906c2fe, (uint)0xf762575d, (uint)0x806567cb, (uint)
            0x196c3671, (uint)0x6e6b06e7, (uint)0xfed41b76, (uint)0x89d32be0, (uint)0x10da7a5a, (uint)0x67dd4acc, (uint)0xf9b9df6f, (uint)0x8ebeeff9, (uint)0x17b7be43, (uint)0x60b08ed5, (uint)
            0xd6d6a3e8, (uint)0xa1d1937e, (uint)0x38d8c2c4, (uint)0x4fdff252, (uint)0xd1bb67f1, (uint)0xa6bc5767, (uint)0x3fb506dd, (uint)0x48b2364b, (uint)0xd80d2bda, (uint)0xaf0a1b4c, (uint)
            0x36034af6, (uint)0x41047a60, (uint)0xdf60efc3, (uint)0xa867df55, (uint)0x316e8eef, (uint)0x4669be79, (uint)0xcb61b38c, (uint)0xbc66831a, (uint)0x256fd2a0, (uint)0x5268e236, (uint)
            0xcc0c7795, (uint)0xbb0b4703, (uint)0x220216b9, (uint)0x5505262f, (uint)0xc5ba3bbe, (uint)0xb2bd0b28, (uint)0x2bb45a92, (uint)0x5cb36a04, (uint)0xc2d7ffa7, (uint)0xb5d0cf31, (uint)
            0x2cd99e8b, (uint)0x5bdeae1d, (uint)0x9b64c2b0, (uint)0xec63f226, (uint)0x756aa39c, (uint)0x26d930a, (uint)0x9c0906a9, (uint)0xeb0e363f, (uint)0x72076785, (uint)0x5005713, (uint)
            0x95bf4a82, (uint)0xe2b87a14, (uint)0x7bb12bae, (uint)0xcb61b38, (uint)0x92d28e9b, (uint)0xe5d5be0d, (uint)0x7cdcefb7, (uint)0xbdbdf21, (uint)0x86d3d2d4, (uint)0xf1d4e242, (uint)
            0x68ddb3f8, (uint)0x1fda836e, (uint)0x81be16cd, (uint)0xf6b9265b, (uint)0x6fb077e1, (uint)0x18b74777, (uint)0x88085ae6, (uint)0xff0f6a70, (uint)0x66063bca, (uint)0x11010b5c, (uint)
            0x8f659eff, (uint)0xf862ae69, (uint)0x616bffd3, (uint)0x166ccf45, (uint)0xa00ae278, (uint)0xd70dd2ee, (uint)0x4e048354, (uint)0x3903b3c2, (uint)0xa7672661, (uint)0xd06016f7, (uint)
            0x4969474d, (uint)0x3e6e77db, (uint)0xaed16a4a, (uint)0xd9d65adc, (uint)0x40df0b66, (uint)0x37d83bf0, (uint)0xa9bcae53, (uint)0xdebb9ec5, (uint)0x47b2cf7f, (uint)0x30b5ffe9, (uint)
            0xbdbdf21c, (uint)0xcabac28a, (uint)0x53b39330, (uint)0x24b4a3a6, (uint)0xbad03605, (uint)0xcdd70693, (uint)0x54de5729, (uint)0x23d967bf, (uint)0xb3667a2e, (uint)0xc4614ab8, (uint)
            0x5d681b02, (uint)0x2a6f2b94, (uint)0xb40bbe37, (uint)0xc30c8ea1, (uint)0x5a05df1b, (uint)0x2d02ef8d };

            public override byte[] Hash
            {
                get
                {
                    byte[] b = BitConverter.GetBytes(result);
                    Array.Reverse(b);
                    return b;
                }
            }
        }
    }

        #endregion

    #endregion

    #region " Symmetric"

    /// <summary> 
    /// Symmetric encryption uses a single key to encrypt and decrypt. 
    /// Both parties (encryptor and decryptor) must share the same secret key. 
    /// </summary> 
    public class Symmetric
    {

        private const string _DefaultIntializationVector = "%1Az=-@qT";
        private const int _BufferSize = 2048;

        public enum Provider
        {
            /// <summary> 
            /// The Data Encryption Standard provider supports a 64 bit key only 
            /// </summary> 
            DES,
            /// <summary> 
            /// The Rivest Cipher 2 provider supports keys ranging from 40 to 128 bits, default is 128 bits 
            /// </summary> 
            RC2,
            /// <summary> 
            /// The Rijndael (also known as AES) provider supports keys of 128, 192, or 256 bits with a default of 256 bits 
            /// </summary> 
            Rijndael,
            /// <summary> 
            /// The TripleDES provider (also known as 3DES) supports keys of 128 or 192 bits with a default of 192 bits 
            /// </summary> 
            TripleDES
        }

        private Data _data;
        private Data _key;
        private Data _iv;
        private SymmetricAlgorithm _crypto;
        private byte[] _EncryptedBytes;
        private bool _UseDefaultInitializationVector;

        private Symmetric()
        {
        }

        /// <summary> 
        /// Instantiates a new symmetric encryption object using the specified provider. 
        /// </summary> 
        public Symmetric(Provider provider, bool useDefaultInitializationVector)
        {
            switch (provider)
            {
                case Provider.DES:
                    _crypto = new DESCryptoServiceProvider();
                    break;
                case Provider.RC2:
                    _crypto = new RC2CryptoServiceProvider();
                    break;
                case Provider.Rijndael:
                    _crypto = new RijndaelManaged();
                    break;
                case Provider.TripleDES:
                    _crypto = new TripleDESCryptoServiceProvider();
                    break;
            }

            //-- make sure key and IV are always set, no matter what 
            this.Key = RandomKey();
            if (useDefaultInitializationVector)
            {
                this.IntializationVector = new Data(_DefaultIntializationVector);
            }
            else
            {
                this.IntializationVector = RandomInitializationVector();
            }
        }

        /// <summary> 
        /// Key size in bytes. We use the default key size for any given provider; if you 
        /// want to force a specific key size, set this property 
        /// </summary> 
        public int KeySizeBytes
        {
            get { return _crypto.KeySize / 8; }
            set
            {
                _crypto.KeySize = value * 8;
                _key.MaxBytes = value;
            }
        }

        /// <summary> 
        /// Key size in bits. We use the default key size for any given provider; if you 
        /// want to force a specific key size, set this property 
        /// </summary> 
        public int KeySizeBits
        {
            get { return _crypto.KeySize; }
            set
            {
                _crypto.KeySize = value;
                _key.MaxBits = value;
            }
        }

        /// <summary> 
        /// The key used to encrypt/decrypt data 
        /// </summary> 
        public Data Key
        {
            get { return _key; }
            set
            {
                _key = value;
                _key.MaxBytes = _crypto.LegalKeySizes[0].MaxSize / 8;
                _key.MinBytes = _crypto.LegalKeySizes[0].MinSize / 8;
                _key.StepBytes = _crypto.LegalKeySizes[0].SkipSize / 8;
            }
        }

        /// <summary> 
        /// Using the default Cipher Block Chaining (CBC) mode, all data blocks are processed using 
        /// the value derived from the previous block; the first data block has no previous data block 
        /// to use, so it needs an InitializationVector to feed the first block 
        /// </summary> 
        public Data IntializationVector
        {
            get { return _iv; }
            set
            {
                _iv = value;
                _iv.MaxBytes = _crypto.BlockSize / 8;
                _iv.MinBytes = _crypto.BlockSize / 8;
            }
        }

        /// <summary> 
        /// generates a random Initialization Vector, if one was not provided 
        /// </summary> 
        public Data RandomInitializationVector()
        {
            _crypto.GenerateIV();
            Data d = new Data(_crypto.IV);
            return d;
        }

        /// <summary> 
        /// generates a random Key, if one was not provided 
        /// </summary> 
        public Data RandomKey()
        {
            _crypto.GenerateKey();
            Data d = new Data(_crypto.Key);
            return d;
        }

        /// <summary> 
        /// Ensures that _crypto object has valid Key and IV 
        /// prior to any attempt to encrypt/decrypt anything 
        /// </summary> 
        private void ValidateKeyAndIv(bool isEncrypting)
        {
            if (_key.IsEmpty)
            {
                if (isEncrypting)
                {
                    _key = RandomKey();
                }
                else
                {
                    throw new CryptographicException("No key was provided for the decryption operation!");
                }
            }
            if (_iv.IsEmpty)
            {
                if (isEncrypting)
                {
                    _iv = RandomInitializationVector();
                }
                else
                {
                    throw new CryptographicException("No initialization vector was provided for the decryption operation!");
                }
            }
            _crypto.Key = _key.Bytes;
            _crypto.IV = _iv.Bytes;
        }

        /// <summary> 
        /// Encrypts the specified Data using provided key 
        /// </summary> 
        public Data Encrypt(Data d, Data key)
        {
            this.Key = key;
            return Encrypt(d);
        }

        /// <summary> 
        /// Encrypts the specified Data using preset key and preset initialization vector 
        /// </summary> 
        public Data Encrypt(Data d)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();

            ValidateKeyAndIv(true);

            CryptoStream cs = new CryptoStream(ms, _crypto.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(d.Bytes, 0, d.Bytes.Length);
            cs.Close();
            ms.Close();

            return new Data(ms.ToArray());
        }

        /// <summary> 
        /// Encrypts the stream to memory using provided key and provided initialization vector 
        /// </summary> 
        public Data Encrypt(Stream s, Data key, Data iv)
        {
            this.IntializationVector = iv;
            this.Key = key;
            return Encrypt(s);
        }

        /// <summary> 
        /// Encrypts the stream to memory using specified key 
        /// </summary> 
        public Data Encrypt(Stream s, Data key)
        {
            this.Key = key;
            return Encrypt(s);
        }

        /// <summary> 
        /// Encrypts the specified stream to memory using preset key and preset initialization vector 
        /// </summary> 
        public Data Encrypt(Stream s)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            byte[] b = new byte[_BufferSize + 1];
            int i = 0;

            ValidateKeyAndIv(true);

            CryptoStream cs = new CryptoStream(ms, _crypto.CreateEncryptor(), CryptoStreamMode.Write);
            i = s.Read(b, 0, _BufferSize);
            while (i > 0)
            {
                cs.Write(b, 0, i);
                i = s.Read(b, 0, _BufferSize);
            }

            cs.Close();
            ms.Close();

            return new Data(ms.ToArray());
        }

        /// <summary> 
        /// Decrypts the specified data using provided key and preset initialization vector 
        /// </summary> 
        public Data Decrypt(Data encryptedData, Data key)
        {
            this.Key = key;
            return Decrypt(encryptedData);
        }

        /// <summary> 
        /// Decrypts the specified stream using provided key and preset initialization vector 
        /// </summary> 
        public Data Decrypt(Stream encryptedStream, Data key)
        {
            this.Key = key;
            return Decrypt(encryptedStream);
        }

        /// <summary> 
        /// Decrypts the specified stream using preset key and preset initialization vector 
        /// </summary> 
        public Data Decrypt(Stream encryptedStream)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            byte[] b = new byte[_BufferSize + 1];

            ValidateKeyAndIv(false);
            CryptoStream cs = new CryptoStream(encryptedStream, _crypto.CreateDecryptor(), CryptoStreamMode.Read);

            int i = 0;
            i = cs.Read(b, 0, _BufferSize);

            while (i > 0)
            {
                ms.Write(b, 0, i);
                i = cs.Read(b, 0, _BufferSize);
            }
            cs.Close();
            ms.Close();

            return new Data(ms.ToArray());
        }

        /// <summary> 
        /// Decrypts the specified data using preset key and preset initialization vector 
        /// </summary> 
        public Data Decrypt(Data encryptedData)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream(encryptedData.Bytes, 0, encryptedData.Bytes.Length);
            byte[] b = new byte[encryptedData.Bytes.Length];

            ValidateKeyAndIv(false);
            CryptoStream cs = new CryptoStream(ms, _crypto.CreateDecryptor(), CryptoStreamMode.Read);

            try
            {
                cs.Read(b, 0, encryptedData.Bytes.Length - 1);
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException("Unable to decrypt data. The provided key may be invalid.", ex);
            }
            finally
            {
                cs.Close();
            }
            return new Data(b);
        }
    }


    #endregion

    #region " Data"

    /// <summary> 
    /// represents Hex, Byte, Base64, or String data to encrypt/decrypt; 
    /// use the .Text property to set/get a string representation 
    /// use the .Hex property to set/get a string-based Hexadecimal representation 
    /// use the .Base64 to set/get a string-based Base64 representation 
    /// </summary> 
    public class Data
    {
        private byte[] _b;
        private int _MaxBytes = 0;
        private int _MinBytes = 0;
        private int _StepBytes = 0;

        /// <summary> 
        /// Determines the default text encoding across ALL Data instances 
        /// </summary> 
        public static System.Text.Encoding DefaultEncoding = System.Text.Encoding.GetEncoding("Windows-1252");

        /// <summary> 
        /// Determines the default text encoding for this Data instance 
        /// </summary> 
        public System.Text.Encoding Encoding = DefaultEncoding;

        /// <summary> 
        /// Creates new, empty encryption data 
        /// </summary> 
        public Data()
        {
        }

        /// <summary> 
        /// Creates new encryption data with the specified byte array 
        /// </summary> 
        public Data(byte[] b)
        {
            _b = b;
        }

        /// <summary> 
        /// Creates new encryption data with the specified string; 
        /// will be converted to byte array using default encoding 
        /// </summary> 
        public Data(string s)
        {
            this.Text = s;
        }

        /// <summary> 
        /// Creates new encryption data using the specified string and the 
        /// specified encoding to convert the string to a byte array. 
        /// </summary> 
        public Data(string s, System.Text.Encoding encoding)
        {
            this.Encoding = encoding;
            this.Text = s;
        }

        /// <summary> 
        /// returns true if no data is present 
        /// </summary> 
        public bool IsEmpty
        {
            get
            {
                if (_b == null)
                {
                    return true;
                }
                if (_b.Length == 0)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary> 
        /// allowed step interval, in bytes, for this data; if 0, no limit 
        /// </summary> 
        public int StepBytes
        {
            get { return _StepBytes; }
            set { _StepBytes = value; }
        }

        /// <summary> 
        /// allowed step interval, in bits, for this data; if 0, no limit 
        /// </summary> 
        public int StepBits
        {
            get { return _StepBytes * 8; }
            set { _StepBytes = value / 8; }
        }

        /// <summary> 
        /// minimum number of bytes allowed for this data; if 0, no limit 
        /// </summary> 
        public int MinBytes
        {
            get { return _MinBytes; }
            set { _MinBytes = value; }
        }

        /// <summary> 
        /// minimum number of bits allowed for this data; if 0, no limit 
        /// </summary> 
        public int MinBits
        {
            get { return _MinBytes * 8; }
            set { _MinBytes = value / 8; }
        }

        /// <summary> 
        /// maximum number of bytes allowed for this data; if 0, no limit 
        /// </summary> 
        public int MaxBytes
        {
            get { return _MaxBytes; }
            set { _MaxBytes = value; }
        }

        /// <summary> 
        /// maximum number of bits allowed for this data; if 0, no limit 
        /// </summary> 
        public int MaxBits
        {
            get { return _MaxBytes * 8; }
            set { _MaxBytes = value / 8; }
        }

        /// <summary> 
        /// Returns the byte representation of the data; 
        /// This will be padded to MinBytes and trimmed to MaxBytes as necessary! 
        /// </summary> 
        public byte[] Bytes
        {
            get
            {
                if (_MaxBytes > 0)
                {
                    if (_b.Length > _MaxBytes)
                    {
                        byte[] b = new byte[_MaxBytes];
                        Array.Copy(_b, b, b.Length);
                        _b = b;
                    }
                }
                if (_MinBytes > 0)
                {
                    if (_b.Length < _MinBytes)
                    {
                        byte[] b = new byte[_MinBytes];
                        Array.Copy(_b, b, _b.Length);
                        _b = b;
                    }
                }
                return _b;
            }
            set { _b = value; }
        }

        /// <summary> 
        /// Sets or returns text representation of bytes using the default text encoding 
        /// </summary> 
        public string Text
        {
            get
            {
                if (_b == null)
                {
                    return "";
                }
                else
                {
                    //-- need to handle nulls here; oddly, C# will happily convert 
                    //-- nulls into the string whereas VB stops converting at the 
                    //-- first null! 
                    int i = Array.IndexOf(_b, (byte)0);
                    if (i >= 0)
                    {
                        return this.Encoding.GetString(_b, 0, i);
                    }
                    else
                    {
                        return this.Encoding.GetString(_b);
                    }
                }
            }
            set { _b = this.Encoding.GetBytes(value); }
        }

        /// <summary> 
        /// Sets or returns Hex string representation of this data 
        /// </summary> 
        public string Hex
        {
            get { return Utils.ToHex(_b); }
            set { _b = Utils.FromHex(value); }
        }

        /// <summary> 
        /// Sets or returns Base64 string representation of this data 
        /// </summary> 
        public string Base64
        {
            get { return Utils.ToBase64(_b); }
            set { _b = Utils.FromBase64(value); }
        }

        /// <summary> 
        /// Returns text representation of bytes using the default text encoding 
        /// </summary> 
        public new string ToString()
        {
            return this.Text;
        }

        /// <summary> 
        /// returns Base64 string representation of this data 
        /// </summary> 
        public string ToBase64()
        {
            return this.Base64;
        }

        /// <summary> 
        /// returns Hex string representation of this data 
        /// </summary> 
        public string ToHex()
        {
            return this.Hex;
        }
    }


    #endregion

    #region " Utils"

    /// <summary> 
    /// Friend class for shared utility methods used by multiple Encryption classes 
    /// </summary> 
    internal class Utils
    {

        /// <summary> 
        /// converts an array of bytes to a string Hex representation 
        /// </summary> 
        static internal string ToHex(byte[] ba)
        {
            if (ba == null || ba.Length == 0)
            {
                return "";
            }
            const string HexFormat = "{0:X2}";
            StringBuilder sb = new StringBuilder();
            foreach (byte b in ba)
            {
                sb.Append(string.Format(HexFormat, b));
            }
            return sb.ToString();
        }

        /// <summary> 
        /// converts from a string Hex representation to an array of bytes 
        /// </summary> 
        static internal byte[] FromHex(string hexEncoded)
        {
            if (hexEncoded == null || hexEncoded.Length == 0)
            {
                return null;
            }
            try
            {
                int l = Convert.ToInt32(hexEncoded.Length / 2);
                byte[] b = new byte[l];
                for (int i = 0; i <= l - 1; i++)
                {
                    b[i] = Convert.ToByte(hexEncoded.Substring(i * 2, 2), 16);
                }
                return b;
            }
            catch (Exception ex)
            {
                throw new System.FormatException("The provided string does not appear to be Hex encoded:" + Environment.NewLine + hexEncoded + Environment.NewLine, ex);
            }
        }

        /// <summary> 
        /// converts from a string Base64 representation to an array of bytes 
        /// </summary> 
        static internal byte[] FromBase64(string base64Encoded)
        {
            if (base64Encoded == null || base64Encoded.Length == 0)
            {
                return null;
            }
            try
            {
                return Convert.FromBase64String(base64Encoded);
            }
            catch (System.FormatException ex)
            {
                throw new System.FormatException("The provided string does not appear to be Base64 encoded:" + Environment.NewLine + base64Encoded + Environment.NewLine, ex);
            }
        }

        /// <summary> 
        /// converts from an array of bytes to a string Base64 representation 
        /// </summary> 
        static internal string ToBase64(byte[] b)
        {
            if (b == null || b.Length == 0)
            {
                return "";
            }
            return Convert.ToBase64String(b);
        }

        /// <summary> 
        /// retrieve an element from an XML string 
        /// </summary> 
        static internal string GetXmlElement(string xml, string element)
        {
            Match m = default(Match);
            m = Regex.Match(xml, "<" + element + ">(?<Element>[^>]*)</" + element + ">", RegexOptions.IgnoreCase);
            if (m == null)
            {
                throw new Exception("Could not find <" + element + "></" + element + "> in provided Public Key XML.");
            }
            return m.Groups["Element"].ToString();
        }

        //static internal string GetConfigString(string key)
        //{
        //    return GetConfigString(key, true);
        //}
        ///// <summary> 
        ///// Returns the specified string value from the application .config file 
        ///// </summary> 
        //static internal string GetConfigString(string key, bool isRequired)
        //{

        //    string s = (string) ConfigurationManager.AppSettings.Get(key);
        //    if (s == null)
        //    {
        //        if (isRequired)
        //        {
        //            throw new ConfigurationErrorsException("key <" + key + "> is missing from .config file");
        //        }
        //        else
        //        {
        //            return "";
        //        }
        //    }
        //    else
        //    {
        //        return s;
        //    }
        //}

        static internal string WriteConfigKey(string key, string value)
        {
            string s = "<add key=\"{0}\" value=\"{1}\" />" + Environment.NewLine;
            return string.Format(s, key, value);
        }

        static internal string WriteXmlElement(string element, string value)
        {
            string s = "<{0}>{1}</{0}>" + Environment.NewLine;
            return string.Format(s, element, value);
        }

        static internal string WriteXmlNode(string element)
        {
            return WriteXmlNode(element, false);
        }
        static internal string WriteXmlNode(string element, bool isClosing)
        {
            string s = null;
            if (isClosing)
            {
                s = "</{0}>" + Environment.NewLine;
            }
            else
            {
                s = "<{0}>" + Environment.NewLine;
            }
            return string.Format(s, element);
        }
    }



    #endregion 

    public static class CryptoExtensions{

    
        public static string Encrypt(this string str, Symmetric.Provider provider, string key)
        {
            Symmetric algorithm = new Symmetric(provider, true);

            Data encryptedData = algorithm.Encrypt(new Data(str), new Data(key));
            return encryptedData.ToBase64();
        }

        public static string Encrypt(this string str, Symmetric.Provider provider)
        {
            return str.Encrypt(provider, ConfigurationManager.AppSettings["symmetricKey"]);
        }

        public static string Encrypt(this string str)
        {
            return str.Encrypt(Symmetric.Provider.Rijndael, ConfigurationManager.AppSettings["symmetricKey"]);
        }

        public static string Decrypt(this string str, Symmetric.Provider provider, string key)
        {
            Symmetric algorithm = new Symmetric(provider, true);

            Data encryptedData = new Data();
            encryptedData.Base64 = str;

            return algorithm.Decrypt(encryptedData, new Data(key)).ToString();
        }

        public static string Decrypt(this string str, Symmetric.Provider provider)
        {
            return str.Decrypt(provider, ConfigurationManager.AppSettings["symmetricKey"]);
        }

        public static string Decrypt(this string str)
        {
            return str.Decrypt(Symmetric.Provider.Rijndael, ConfigurationManager.AppSettings["symmetricKey"]);
        }

        public static byte[] MD5Encode(this string ToEncode)
        {
            return (new MD5CryptoServiceProvider()).ComputeHash((new UTF8Encoding()).GetBytes(ToEncode));
        }
    }
}
