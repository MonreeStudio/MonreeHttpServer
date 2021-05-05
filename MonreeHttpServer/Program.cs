using MonreeHttpServer.MonreeListener.Data;
using MonreeHttpServer.MonreeListener.Email;
using MonreeHttpServer.MonreeListener.User;
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

            // 注销监听器
            LogoutListener logoutListener = new LogoutListener(PrefixeHelper.LOGOUT_PREFIXE);
            logoutListener.Start();

            // 注册监听器
            SignUpListener signUpListener = new SignUpListener(PrefixeHelper.SIGN_UP_PREFIXE);
            signUpListener.Start();

            // 注册验证码监听器
            SignUpCodeListener signUpCodeListener = new SignUpCodeListener(PrefixeHelper.SIGN_UP_CODE_PREFIXE);
            signUpCodeListener.Start();

            // 拉取云端数据监听器
            PullDataListener pullDataListener = new PullDataListener(PrefixeHelper.PULL_DATA_PREFIXE);
            pullDataListener.Start();

            // 数据推送到云端监听器
            PushDataListener pushDataListener = new PushDataListener(PrefixeHelper.PUSH_DATA_PREFIXE);
            pushDataListener.Start();
        }
    }
}
