using Steam_Account_Manager.Infrastructure.Models;
using Steam_Account_Manager.Infrastructure.Models.AccountModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

namespace Steam_Account_Manager.Infrastructure
{
    internal static class Config
    {

        public static ConfigProperties Properties { get; set; }
        public static List<Account> Accounts { get; set; }
        public static string TempUserKey { get; set; }


        #region Config properties methods
        private static void InitProperties()
        {
            Properties = new ConfigProperties
            {
                UserCryptoKey = CryptoKey,
                Theme = Models.Themes.Dark,
                Language = Languages.English,
                RecentlyLoggedUsers = new List<RecentlyLoggedUser>()
            };
            try
            {
                Properties.SteamDirection = Utilities.GetSteamRegistryDirection();
                switch (Utilities.GetSteamRegistryLanguage())
                {

                    case "russian":
                        Properties.Language = Languages.Russian;
                        break;

                    case "ukrainian":
                        Properties.Language = Languages.Ukrainian;
                        break;
                }
            }
            catch { }
        }
        public static bool GetPropertiesInstance()
        {
            if (Properties == null)
            {
                if (File.Exists(App.WorkingDirectory + "\\config.dat"))
                {
                    var result = (ConfigProperties)Deserialize(App.WorkingDirectory + @"\config.dat", CryptoKey);
                    if(result == null)
                    {
                        return false;
                    }
                    Properties = result;
                    Properties.Theme = Properties.Theme;
                    Properties.Language = Properties.Language;
                }
                else
                {
                    InitProperties();
                    SaveProperties();
                }
            }
            return true;
        }
        public static void SaveProperties()
        {
            Serialize(Properties, App.WorkingDirectory + @"\config.dat", CryptoKey);
        }
        public static void ClearProperties()
        {
            InitProperties();
            SaveProperties();
        }
        #endregion


        #region Config accounts methods
        public static bool GetAccountsInstance()
        {
            if (Accounts == null)
            {
                if (File.Exists(App.WorkingDirectory + "\\database.dat"))
                {
                    if (Properties == null) GetPropertiesInstance();
                    var result = (List<Account>)Deserialize(App.WorkingDirectory + @"\database.dat", Properties.UserCryptoKey);
                    if(result == null)
                    {
                        return false;
                    }
                    Accounts = result;
                }
                else
                {
                    Accounts = new List<Account>();
                    Serialize(Accounts, App.WorkingDirectory + @"\database.dat", Properties.UserCryptoKey);
                }
            }
            return true;
        }
        public static void SaveAccounts()
        {
            if (Accounts != null)
                Serialize(Accounts, App.WorkingDirectory + @"\database.dat", Properties.UserCryptoKey);
        }
        #endregion


        //Encrypting
        private static readonly string CryptoKey = "EOtzannXEOSd5HGBSJvs0op1BHRuvFwlKMZcJXcOp0M=";
        private static readonly int KeySize = 256;
        private static readonly int IvSize = 16; // block size is 128-bit

        public static string GetDefaultCryptoKey => CryptoKey;

        private static void WriteObjectToStream(Stream outputStream, object obj)
        {
            if (ReferenceEquals(obj, null)) return;
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(outputStream, obj);
        }
        private static object ReadObjectFromStream(Stream inputStream)
        {
            BinaryFormatter binForm = new BinaryFormatter();
            var obj = binForm.Deserialize(inputStream);
            return obj;
        }
        private static CryptoStream CreateEncryptionStream(byte[] key, Stream outputStream)
        {
            byte[] iv = new byte[IvSize];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider()) rng.GetNonZeroBytes(iv);

            outputStream.Write(iv, 0, iv.Length);
            Rijndael rijndael = new RijndaelManaged();
            rijndael.KeySize = KeySize;
            CryptoStream encryptor = new CryptoStream(outputStream, rijndael.CreateEncryptor(key, iv), CryptoStreamMode.Write);
            return encryptor;
        }
        private static CryptoStream CreateDecryptionStream(byte[] key, Stream inputStream)
        {
            byte[] iv = new byte[IvSize];

            if (inputStream.Read(iv, 0, iv.Length) != iv.Length)
            {
                throw new ApplicationException("Failed to read IV from stream.");
            }

            Rijndael rijndael = new RijndaelManaged
            {
                KeySize = KeySize
            };

            CryptoStream decryptor = new CryptoStream(
                inputStream,
                rijndael.CreateDecryptor(key, iv),
                CryptoStreamMode.Read);
            return decryptor;
        }
        public static void Serialize(object obj, string path, string CryptoKey)
        {
            byte[] key = Convert.FromBase64String(CryptoKey);

            using (FileStream file = new FileStream(path, FileMode.Create))
            {
                using (CryptoStream cryptoStream = CreateEncryptionStream(key, file))
                {
                    WriteObjectToStream(cryptoStream, obj);
                }
            }
        }
        public static object Deserialize(string path, string CryptoKey)
        {
            try
            {
                byte[] key = Convert.FromBase64String(CryptoKey);

                using (FileStream file = new FileStream(path, FileMode.Open))
                using (CryptoStream cryptoStream = CreateDecryptionStream(key, file))
                {
                    return ReadObjectFromStream(cryptoStream);
                }
            }
            catch
            {
                return null;
            }

        }

    }
}
