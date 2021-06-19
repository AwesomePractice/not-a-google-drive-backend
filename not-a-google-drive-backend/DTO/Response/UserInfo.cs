
using System;

namespace not_a_google_drive_backend.DTO.Response
{
    public class UserInfo
    {
        public UserInfo(DatabaseModule.Entities.User user) {
            Id = user.Id.ToString();
            Login = user.Login;
            FirstName = user.FirstName;
            LastName = user.LastName;
            BirthDate = user.BirthDate;
        }

        public string Id { get; set; }
        public string Login { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public DateTime BirthDate { get; set; }
    }
}