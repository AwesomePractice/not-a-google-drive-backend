using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseModule
{
    public static class DBConnectionService
    {

        static RepositoryService()
        {
            MongoDbSettings _dbSettings = new MongoDbSettings();
            _dbSettings.ConnectionString = ConfigurationManager.AppSettings["db_connection"];
            _dbSettings.DatabaseName = ConfigurationManager.AppSettings["db_name"];

           
        }
    }
}
