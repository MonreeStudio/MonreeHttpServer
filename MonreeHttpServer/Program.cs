using MonreeHttpServer.MonreeListner.User;
using System;
using System.IO;
using System.Net;

namespace MonreeHttpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            // 登录监听器
            LoginListener loginListener = new LoginListener(PrefixeHelper.LOGIN_PREFIXE);
            loginListener.Start();

            // 注册监听器
            SiginUpListener siginUpListener = new SiginUpListener(PrefixeHelper.SIGN_UP_PREFIXE);
            siginUpListener.Start();
        }
    }
}
