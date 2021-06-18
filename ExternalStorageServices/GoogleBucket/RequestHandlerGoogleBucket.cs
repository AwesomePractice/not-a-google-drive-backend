﻿using Google.Apis.Auth.OAuth2;
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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        public bool UploadFile(IFormFile file, string fileName)
        {
            var newObject = new Google.Apis.Storage.v1.Data.Object()
            {
                Bucket = BucketToUpload,
                Name = fileName
            };

            // Actions with data here encrypting / compressing

            var fileStream = file.OpenReadStream();
            try
            {
                var uploadRequest = new ObjectsResource.InsertMediaUpload(StorageService, newObject,
                BucketToUpload, fileStream, file.ContentType);
                
              
                var res = uploadRequest.Upload();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Dispose();
                }
            }
            return true;
        }

        public byte[] DownloadFile(string fileId)
        {
            var newObject = new Google.Apis.Storage.v1.Data.Object()
            {
                Bucket = BucketToUpload,
                Name = fileId
            };

            Stream memoryStream = new MemoryStream();
            byte[] result = null;
            try
            {
                var downloadRequest = new ObjectsResource.GetRequest(StorageService,
                BucketToUpload, fileId);
                var resultStatus = downloadRequest.DownloadWithStatus(memoryStream);
                result = ReadToEnd(memoryStream);

                // Actions with data here decrypting / decompressing
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
    }
}
