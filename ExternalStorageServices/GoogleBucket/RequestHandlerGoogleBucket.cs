using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Storage.v1;
using Google.Apis.Storage.v1.Data;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DatabaseModule.Entities;
using File = DatabaseModule.Entities.File;
using System.IO.Compression;

namespace ExternalStorageServices.GoogleBucket
{
    public class RequestHandlerGoogleBucket : IStorageAccessor
    {
        public UserCredential UserCredential { get; set; }
        public CancellationTokenSource Cts { get; set; }
        public StorageService StorageService { get; set; }
        public string BucketToUpload { get; set; }
        public string ProjectName { get; set; }


        public RequestHandlerGoogleBucket(string configData, string bucketToUpload)
        {
            var scopes = new[] { @"https://www.googleapis.com/auth/devstorage.full_control" };

            BucketToUpload = bucketToUpload;

            var credential = GoogleCredential.FromJson(configData)
                                      .CreateScoped(scopes)
                                      .UnderlyingCredential as ServiceAccountCredential;

            StorageService = new StorageService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Not a google drive",
            });
        }

        

        public List<string> GetAllBuckets()
        {
            var bucketsQuery = StorageService.Buckets.List(ProjectName);
            bucketsQuery.OauthToken = UserCredential.Token.AccessToken;
            var availableBuckets = bucketsQuery.Execute();
            return availableBuckets.Items.ToList().ConvertAll(x => new String(x.Name));
        }

        public UploadResult UploadFile(IFormFile file, string fileName, bool encryption, bool compressing)
        {
            var newObject = new Google.Apis.Storage.v1.Data.Object()
            {
                Bucket = BucketToUpload,
                Name = fileName
            };
            string encryptionKey = null, iv = null;

            // Actions with data here encrypting / compressing

            var fileStream = file.OpenReadStream();
            var fileBytes = ReadToEnd(fileStream);

            #region Encryption and compressing
            if (encryption)
            {
                var actionData = GenerateKeyAndIV(FileActionsConstants.AESFlavour);
                fileBytes = Encrypt(fileBytes, actionData.Item1, actionData.Item2);
                encryptionKey = Convert.ToBase64String(actionData.Item1);
                iv = Convert.ToBase64String(actionData.Item2);
            }
            if (compressing)
            {
                fileBytes = Compress(fileBytes);
            }
            #endregion


            Stream fileOutStream = new MemoryStream(fileBytes);
            try
            {
                var uploadRequest = new ObjectsResource.InsertMediaUpload(StorageService, newObject,
                BucketToUpload, fileOutStream, file.ContentType);
                
              
                var res = uploadRequest.Upload();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new UploadResult { Success = false};
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Dispose();
                }
            }
            return new UploadResult(encryptionKey, iv);
        }

        public byte[] DownloadFile(File fileInfo)
        {
            var newObject = new Google.Apis.Storage.v1.Data.Object()
            {
                Bucket = BucketToUpload,
                Name = fileInfo.Id.ToString()
            };

            Stream memoryStream = new MemoryStream();
            byte[] result = null;
            try
            {
                var downloadRequest = new ObjectsResource.GetRequest(StorageService,
                BucketToUpload, fileInfo.Id.ToString());
                var resultStatus = downloadRequest.DownloadWithStatus(memoryStream);
                result = ReadToEnd(memoryStream);
                if (fileInfo.Encrypted)
                {
                    result = Decrypt(result, Convert.FromBase64String(fileInfo.EncryptionKey), Convert.FromBase64String(fileInfo.IV));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                memoryStream.Dispose();
            }
            return result;
        }

        


        public bool DeleteFile(string fileId)
        {
            var newObject = new Google.Apis.Storage.v1.Data.Object()
            {
                Bucket = BucketToUpload,
                Name = fileId
            };
            string resultStatus;
            try
            {
                var deleteRequest = new ObjectsResource.DeleteRequest(StorageService,
                BucketToUpload, fileId);
                resultStatus = deleteRequest.Execute();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return true;
        }

        public List<string> GetFilesList()
        {
            var newObject = new Google.Apis.Storage.v1.Data.Object()
            {
                Bucket = BucketToUpload,
            };
            List<string> resultStatus = null;
            try
            {
                var listRequest = new ObjectsResource.ListRequest(StorageService,
                BucketToUpload);
                listRequest.OauthToken = UserCredential.Token.AccessToken;
                resultStatus = listRequest.Execute().Items.Select(x => x.Name).ToList();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return resultStatus;
        }




        private byte[] ReadToEnd(Stream stream)
        {
            long originalPosition = 0;

            if (stream.CanSeek)
            {
                originalPosition = stream.Position;
                stream.Position = 0;
            }

            try
            {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
            }
        }
           
        

        private (byte[], byte[]) GenerateKeyAndIV(int length)
        {
            return (GenerateRandomBytes(length), GenerateRandomBytes(length));
        }


        private byte[] GenerateRandomBytes(int length)
        {
            byte[] result = new byte[length];
            RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
            rngCsp.GetBytes(result);
            return result;
        }

        private byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 128;
                aes.BlockSize = 128;
                aes.Padding = PaddingMode.Zeros;

                aes.Key = key;
                aes.IV = iv;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    return PerformCryptography(data, encryptor);
                }
            }
        }

        private byte[] Decrypt(byte[] data, byte[] key, byte[] iv)
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 128;
                aes.BlockSize = 128;
                aes.Padding = PaddingMode.Zeros;

                aes.Key = key;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    return PerformCryptography(data, decryptor);
                }
            }
        }

        private byte[] PerformCryptography(byte[] data, ICryptoTransform cryptoTransform)
        {
            using (var ms = new MemoryStream())
            using (var cryptoStream = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write))
            {
                cryptoStream.Write(data, 0, data.Length);
                cryptoStream.FlushFinalBlock();

                return ms.ToArray();
            }
        }


        public byte[] Compress(byte[] source)
        {
            using (MemoryStream sourceStream = new MemoryStream(source))
            {
                using (MemoryStream targetStream = new MemoryStream())
                {
                    using (GZipStream compressionStream = new GZipStream(targetStream, CompressionMode.Compress))
                    {
                        sourceStream.CopyTo(compressionStream);
                        return ReadToEnd(targetStream);
                    }
                }
            }
        }

        public void Decompress(string compressedFile, string targetFile)
        {
            // поток для чтения из сжатого файла
            using (FileStream sourceStream = new FileStream(compressedFile, FileMode.OpenOrCreate))
            {
                // поток для записи восстановленного файла
                using (FileStream targetStream = File.Create(targetFile))
                {
                    // поток разархивации
                    using (GZipStream decompressionStream = new GZipStream(sourceStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(targetStream);
                        Console.WriteLine("Восстановлен файл: {0}", targetFile);
                    }
                }
            }
        }
    }
}
