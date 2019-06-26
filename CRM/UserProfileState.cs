using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EchoBot1.CRM
{
    public class UserProfileState
    {
        public string AccessCode { get; set; }
        public string CalAccessToken { get; set; }
        public string UserEmail { get; set; }
        public string AccessToken { get; set; }
        public string CurrentDialog { get; set; }
    }
}
