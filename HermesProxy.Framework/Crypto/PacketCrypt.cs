using System;
using System.Security.Cryptography;

using HermesProxy.Framework.Util;

namespace HermesProxy.Framework.Crypto
{
    public enum CryptType
    {
        Client = 0x544E4C43,
        Server = 0x52565253
    }

    public sealed class NewWorldCrypt : IDisposable
    {
        public bool IsInitialized { get; private set; }

        AesGcm _serverEncrypt;
        AesGcm _clientEncrypt;
        ulong _clientCounter;
        ulong _serverCounter;

        public void Initialize(byte[] key)
        {
            if (IsInitialized)
                throw new InvalidOperationException($"{nameof(NewWorldCrypt)} is already iniatialized");

            _serverEncrypt = new(key);
            _clientEncrypt = new(key);
        }

        public bool Encrypt(ref byte[] data, ref byte[] tag)
        {
            try
            {
                if (IsInitialized)
                    _serverEncrypt.Encrypt(BitConverter.GetBytes(_serverCounter).Combine(BitConverter.GetBytes((int)CryptType.Server)), data, data, tag);

                ++_serverCounter;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Decrypt(ref byte[] data, ref byte[] tag)
        {
            try
            {
                if (IsInitialized)
                    _clientEncrypt.Decrypt(BitConverter.GetBytes(_clientCounter).Combine(BitConverter.GetBytes((int)CryptType.Client)), data, data, tag);

                ++_clientCounter;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            IsInitialized = false;
        }
    }
}
