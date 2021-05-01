using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace MonreeHttpServer.MonreeListner.User
{
    class LoginListener
    {
        private string prefixe;
        public LoginListener(string prefixe)
        {
            this.prefixe = prefixe;
        }

        public void Start()
        {
            try
            {
                ThreadStart threadStart = new ThreadStart(StartLoginListener);
                Thread thread = new Thread(threadStart);
                thread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void StartLoginListener()
        {
            try
            {
                using (HttpListener listener = new HttpListener())
                {
                    listener.Prefixes.Add(prefixe);
                    listener.Start();
                    Console.WriteLine("开始监听登录");
                    while (true)
                    {
                        try
                        {
                            HttpListenerContext context = listener.GetContext();//阻塞
                            string contentType = context.Request.ContentType;
                            HttpListenerRequest request = context.Request;
                            string postData = new StreamReader(request.InputStream).ReadToEnd();
                            Console.WriteLine("收到登录请求：" + postData);
                            HttpListenerResponse response = context.Response;//响应
                            string responseBody = MonreeLogin(postData, contentType);
                            Console.WriteLine("请求返回结果:\n" + responseBody);
                            response.ContentLength64 = Encoding.UTF8.GetByteCount(responseBody);
                            response.ContentType = "text/html; Charset=UTF-8";
                            //输出响应内容
                            Stream output = response.OutputStream;
                            using (StreamWriter sw = new StreamWriter(output))
                            {
                                sw.Write(responseBody);
                            }
                            Console.WriteLine("响应登录结束\n");
                        }
                        catch (Exception err)
                        {
                            Console.WriteLine(err.Message);
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("程序异常，请重新打开程序：" + err.Message);
            }

        }

        public void Stop()
        {

        }

        // 用户登录
        private string MonreeLogin(string postData, String contentType)
        {
            // 连接数据库查询
            if (!contentType.Equals("application/json"))
            {
                // 返回登录失败
                var failureResponse = new FailureResponse();
                failureResponse.Code = -1;
                failureResponse.Message = "登录失败";
                string failureResonpseJson = JsonConvert.SerializeObject(failureResponse);
                return failureResonpseJson;
            }

            string userName = "";
            string password = "";
            try
            {
                JObject jo = JObject.Parse(postData);
                string userInfo = jo["User"].ToString();
                JObject userInfoObject = JObject.Parse(userInfo);
                userName = userInfoObject["UserName"].ToString();
                password = userInfoObject["Password"].ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
            // 返回登录成功
            var successResponse = new SuccessResponse();
            successResponse.Code = 2000;
            successResponse.Message = "登录成功";
            UserItem userItem = new UserItem();
            userItem.Id = "1";
            userItem.UserName = userName;
            userItem.Password = password;
            userItem.Token = Guid.NewGuid().ToString("N");
            userItem.TimeStamp = DateTime.Now.ToString();
            successResponse.User = userItem;
            string successResponseJson = JsonConvert.SerializeObject(successResponse);
            return successResponseJson;
        }

        class SuccessResponse
        {
            public Int16 Code { get; set; }
            public string Message { get; set; }
            public UserItem User{ get; set; }
        }

        class FailureResponse
        {
            public Int16 Code { get; set; }
            public string Message { get; set; }
        }

        class UserItem
        {
            public string Id { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }
            public string Token { get; set; }
            public string TimeStamp { get; set; }
        }
    }
}
