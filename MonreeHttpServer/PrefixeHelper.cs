using System;
using System.Collections.Generic;
using System.Text;

namespace MonreeHttpServer
{
    class PrefixeHelper
    {
        private static string URL = "http://msdate.monreeing.com:3000";

        public static string LOGIN_PREFIXE = URL + "/user/login/";
        public static string SIGN_UP_PREFIXE =  URL + "/user/signup/";
        public static string LOGOUT_PREFIXE = URL + "/user/logout/";
        public static string SIGN_UP_CODE_PREFIXE = URL + "/email/signup_code/";
        public static string PULL_DATA_PREFIXE = URL + "/data/pulldata/";
        public static string PUSH_DATA_PREFIXE = URL + "/data/pushdata/";
    }
}
