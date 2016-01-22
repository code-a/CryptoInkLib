﻿using System;
using System.IO;
using System.Text;
using System.Reflection;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto; //PBEParametersGenerator;
using Org.BouncyCastle.Crypto.Generators; //PKCS5S2ParametersGenerator;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.X509;


using Org.BouncyCastle.Crypto.Digests; //SHA3Digest;
using Org.BouncyCastle.Crypto.Parameters; //.KeyParameter;
using Org.BouncyCastle.Crypto.Prng; //DigestRandomGenerator;


//for aes encryption
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Utilities.Encoders;
using Org.BouncyCastle.Crypto.IO;
using Org.BouncyCastle.Utilities.IO.Pem;

//for HMAC
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Math;

namespace CryptoInkLib
{

	public class IDManager
	{

		public IDManager ()
		{

		}



		public int createID(string c_sPassword, string c_sIDName, string c_sIDPath)
		{
			/*
			 * Generate and create keys
			*/

			//create Random Number Generator
			var rng = new System.Security.Cryptography.RNGCryptoServiceProvider();

			//create passwordKey from password
			byte[] salt = new byte[16];
			rng.GetBytes (salt);

			byte[] passwordKey = IDCrypto.createPasswordKey (c_sPassword, salt);


			/*
			 * Encrypt keys and add them to keystore
			*/

			//create Keystore and add Header
			ID keyStore = new ID ();
			keyStore.PasswordKeySalt = salt;

			//TODO: save FileEncUserKey as Key
			//create FileEncUserKey Object
			FileEncUserKey userKey = FileEncUserKeyGen.generate();
			userKey.keyID = c_sIDName;

			//create KeyStoreStorage and add our first userKey
			IDStorage keyStoreStorage = new IDStorage();
			Key firstUserKey = new Key ();
			firstUserKey.keyID = c_sIDName;
			firstUserKey.service = new SService();
			firstUserKey.status = 1;
			firstUserKey.type = "FileEncUserKey";
			firstUserKey.keyContent = JsonConvert.SerializeObject (userKey);

			keyStoreStorage.privateKeys = new Key[] { firstUserKey };
			keyStoreStorage.publicKeys = null;


			//get IV for storage encryption
			byte[] storage_iv 	= new byte[16];

			rng.GetBytes (storage_iv);
			keyStore.StorageIV = storage_iv;

			//Encrypt keyStoreStorage
			keyStore.Storage = IDCrypto.encryptKeyStoreStorage (passwordKey, storage_iv, keyStoreStorage);

			//Get KeyStore as String
			string jsonKeystore = JsonConvert.SerializeObject (keyStore);

			//Write Keystore to file
			string sAppPath = Path.Combine(c_sIDPath, (c_sIDName + ".keystore"));
			File.WriteAllText(sAppPath, jsonKeystore);


			return 0;
		}



		public UserSession loadUserKeyStore(string c_sPassword, string c_sKeyStoreName, string c_sKeyStorePath)
		{
			//read contents from KeyStore-file
			StreamReader _StreamReader 	= new StreamReader (Path.Combine(c_sKeyStorePath, c_sKeyStoreName));
			string sKeyStoreContent 	= _StreamReader.ReadToEnd();
			_StreamReader.Close();


			//create PasswordKey from Password
			ID _KeyStore 			= JsonConvert.DeserializeObject <ID> (sKeyStoreContent);
			byte [] baPasswordKey 		= IDCrypto.createPasswordKey (c_sPassword, _KeyStore.PasswordKeySalt);

			 
			IDStorage _KeyStoreStorage = IDCrypto.decryptKeyStoreStorage (baPasswordKey, _KeyStore.StorageIV, _KeyStore.Storage);

			if (_KeyStoreStorage == null) {
				return null;
			}

			//Create UserSession
			UserSession _UserSession 			= new UserSession ();
			_UserSession.m_sKeyStorePath 		= Path.Combine(c_sKeyStorePath, c_sKeyStoreName);
			_UserSession.m_baPasswordKey 		= baPasswordKey;
			_UserSession.m_baPasswordKeySalt 	= _KeyStore.PasswordKeySalt;
			_UserSession.m_baStorage_IV 		= _KeyStore.StorageIV;
			_UserSession.m_KeyStoreStorage 		= _KeyStoreStorage;

			return _UserSession;
		}



		public int updateKeyStore(UserSession c_UserSession, string c_sPassword)
		{
			//transform UserSession to KeyStore
			ID _KeyStore = c_UserSession.toKeyStore();

			//TODO: changed to AES-GCM, check if return value is null to check the validity
			//check password validity
			if( ! IDCrypto.isPasswordValid(c_sPassword, c_UserSession.m_baPasswordKeySalt, c_UserSession.m_baPasswordKey))
			{
				return 1;
			}

			//create KeyStore as String
			string sJsonKeyStore = JsonConvert.SerializeObject (_KeyStore);


			return 0;
		}

		/*
		public FileEncUserKey createFileEncUserKey(byte[] serializedPublic, byte[] encodedPrivate)
		{
			FileEncUserKey fileEncKey = new FileEncUserKey ();
			fileEncKey.publicKey = Convert.ToBase64String (serializedPublic);
			fileEncKey.privateKey = Convert.ToBase64String (encodedPrivate);
			fileEncKey.status = 1;
			fileEncKey.creationDate = DateTime.Now.ToString("d/M/yyyy");
			fileEncKey.revocationDate = null;

			return fileEncKey;
		}*/


		public ID changeKeyStorePassword(string c_sOldPassword, string c_sNewPassword, string c_sKeyStoreName, string c_sKeyStorePath)
		{
			//TODO:
			// - load keystore with password
			// - change UserSession Keys
			// - transform UserSession to KeyStore
			// - save new KeyStore
			ID _KeyStore = new ID ();
			return _KeyStore;
		}


			
	}
}
