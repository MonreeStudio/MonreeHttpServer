using System;
using System.Collections.Generic;
using System.Text;

namespace MonreeHttpServer
{
    class PrefixeHelper
    {
        private static string URL = "http://localhost:8888";

        public static String LOGIN_PREFIXE = URL + "/user/login/";
        public static String SIGN_UP_PREFIXE =  URL + "/user/signup/";
        public static String GET_DATA_PREFIXE = URL + "";
    }
}
