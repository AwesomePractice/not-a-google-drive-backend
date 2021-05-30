using DatabaseModule.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseModule
{
    public class DBConnectionService
    {
        private static IMongoRepository<User> _usersRepository;

        public static IMongoRepository<User> UsersRepository 
        { 
            get { return _usersRepository; } 
        }

        private MongoDbSettings _dbSettings;

        public DBConnectionService(string connectionStr, string dbName)
        {
           if(_dbSettings == null)
            {
                _dbSettings = new MongoDbSettings();
                _dbSettings.ConnectionString = connectionStr;
                _dbSettings.DatabaseName = dbName;

                _usersRepository = new MongoRepository<User>(_dbSettings);
               
            } 
        }
    }
}
