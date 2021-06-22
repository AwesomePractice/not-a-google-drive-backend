using DatabaseModule.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using not_a_google_drive_backend.Tools;
using not_a_google_drive_backend.DTO.Response;
using MongoDB.Bson;

namespace UnitTests.Tools
{
    public class CombineFilesAndFolders
    {
        [Fact]
        public void OneEmptyFolder()
        {
            string folderName = "Name";
            Folder folder = new Folder(folderName, new MongoDB.Bson.ObjectId(), null, false);
            List<Folder> folders = new List<Folder>() { folder };
            IEnumerable<File> files = new List<File>();
            UserFilesInfoFolder res = FileFolderManager.CombineFilesAndFolders(folders, files);

            Assert.NotNull(res);
            Assert.NotNull(res.Children);
            Assert.NotNull(res.Id);
            Assert.NotNull(res.Files);

            Assert.Equal(Array.Empty<UserFilesInfoFile>(), res.Files);
            Assert.Equal(Array.Empty<UserFilesInfoFolder>(), res.Children);
            Assert.Equal(folderName, res.Name);
            Assert.Equal(folder.Id.ToString(), res.Id);
        }

        [Fact]
        public void FolderStructureWithoutFiles()
        {
            int amountOnLevel1 = 23;
            int amountOnLevel2 = 7;
            string rootFolderName = "Root";

            Random random = new Random();

            Folder rootFolder = new Folder(rootFolderName, ObjectId.Empty, null, false) { 
                Id = ObjectId.GenerateNewId(DateTime.Now.AddDays(random.NextDouble()))
            };
            List<Folder> folders = new List<Folder>() {rootFolder};
            IEnumerable<File> files = new List<File>();

            for(int i = 0; i<amountOnLevel1; ++i)
            {
                string name = "Level_1_" + i;
                Folder folder = new Folder(name, ObjectId.Empty, rootFolder.Id, false)
                {
                    Id = ObjectId.GenerateNewId(DateTime.Now.AddDays(random.NextDouble()))
                };
                folders.Add(folder);
                for(int j=0; j<amountOnLevel2; ++j)
                {
                    string n = name + "_Level_2_" + j;
                    Folder f = new Folder(n, ObjectId.Empty, folder.Id, false)
                    {
                        Id = ObjectId.GenerateNewId(DateTime.Now.AddDays(random.NextDouble()))
                    };
                    folders.Add(f);
                }
            }

            Assert.Equal(1 + amountOnLevel1 * (1 + amountOnLevel2), folders.Count);

            UserFilesInfoFolder res = FileFolderManager.CombineFilesAndFolders(folders, files);

            Assert.NotNull(res);
            Assert.NotNull(res.Children);
            Assert.NotNull(res.Id);
            Assert.NotNull(res.Files);

            Assert.Equal(rootFolder.Id.ToString(), res.Id);
            Assert.Equal(rootFolderName, res.Name);
            Assert.Equal(amountOnLevel1, res.Children.Count());
            Assert.Empty(res.Files);

            for(int i = 0; i<res.Children.Length; ++i)
            {
                var child = res.Children.ElementAt(i);
                var childFolder = folders.First(f => f.ParentId == rootFolder.Id && f.Name == ("Level_1_" + i));

                Assert.NotNull(child);
                Assert.NotNull(child.Children);
                Assert.NotNull(child.Id);
                Assert.NotNull(child.Files);

                Assert.Equal(childFolder.Id.ToString(), child.Id);
                Assert.Equal("Level_1_" + i, child.Name);
                Assert.Equal(amountOnLevel2, child.Children.Count());
                Assert.Empty(child.Files);

                for (int j = 0; j < child.Children.Length; ++j)
                {
                    var childChild = child.Children.ElementAt(j);
                    string expectedName = ("Level_1_" + i + "_Level_2_" + j);
                    var childChildFolder = folders.First(f => f.ParentId == childFolder.Id && f.Name == expectedName);

                    Assert.NotNull(childChild);
                    Assert.NotNull(childChild.Children);
                    Assert.NotNull(childChild.Id);
                    Assert.NotNull(childChild.Files);

                    Assert.Equal(childChild.Id.ToString(), childChild.Id);
                    Assert.Equal(expectedName, childChild.Name);
                    Assert.Empty(childChild.Children);
                    Assert.Empty(childChild.Files);
                }
            }
        }
    }
}
