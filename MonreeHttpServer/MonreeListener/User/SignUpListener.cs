using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace MonreeHttpServer.MonreeListner.User
{
    class SignUpListener
    {
        private string prefixe;
        private MySqlConnection conn;
        private static string connetStr = "server=127.0.0.1;port=3306;user=Monree;password=laixiaxin1718990; database=msdatetest;";

        public SignUpListener(string prefixe)
        {
            this.prefixe = prefixe;
        }

        public void Start()
        {
            try
            {
                ThreadStart threadStart = new ThreadStart(StartSiginUpListener);
                Thread thread = new Thread(threadStart);
                thread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void StartSiginUpListener()
        {
            try
            {
                using (HttpListener listener = new HttpListener())
                {
                    listener.Prefixes.Add(prefixe);
                    listener.Start();
                    Console.WriteLine("开始监听注册");
                    while (true)
                    {
                        try
                        {
                            HttpListenerContext context = listener.GetContext();//阻塞
                            string contentType = context.Request.ContentType;
                            HttpListenerRequest request = context.Request;
                            string postData = new StreamReader(request.InputStream).ReadToEnd();
                            Console.WriteLine("收到注册请求：" + postData);
                            HttpListenerResponse response = context.Response;//响应
                            string responseBody = MonreeSignUp(postData, contentType);
                            response.ContentLength64 = System.Text.Encoding.UTF8.GetByteCount(responseBody);
                            response.ContentType = "text/html; Charset=UTF-8";
                            //输出响应内容
                            Stream output = response.OutputStream;
                            using (StreamWriter sw = new StreamWriter(output))
                            {
                                sw.Write(responseBody);
                            }
                            Console.WriteLine("响应注册结束\n");
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

        private String MonreeSignUp(string postData, String contentType)
        {
            ConnectToMySQL();
            if (!contentType.Equals("application/json"))
            {
                // 返回注册失败
                return getFailureResponse(-1, "请求格式错误");
            }
            string userName = "";
            string password = "";
            string code = "";
            try
            {
                JObject jo = JObject.Parse(postData);
                string userInfo = jo["User"].ToString();
                JObject userInfoObject = JObject.Parse(userInfo);
                userName = userInfoObject["UserName"].ToString();
                password = userInfoObject["Password"].ToString();
                code = userInfoObject["Code"].ToString();
                string sql0 = "Select *from user where userName = '" + userName + "';";
                MySqlCommand cmd0 = new MySqlCommand(sql0, conn);
                if(ConnectionState.Open != conn.State)
                {
                    conn.Open();
                }
                var result0 = cmd0.ExecuteReader();
                bool isExist = false;
                while (result0.Read())
                {
                    isExist = true;
                }
                if (isExist)
                {
                    conn.Close();
                    return getFailureResponse(1000, "该邮箱已被注册");
                }
                conn.Close();
                conn.Open();
                string sql1 = "Select *from verificationcode where email = '" + userName + "';";
                MySqlCommand cmd1 = new MySqlCommand(sql1, conn);
                var result1 = cmd1.ExecuteReader();
                while (result1.Read())
                {
                    if(result1.GetString("code").Equals(code))
                    {
                        conn.Close();
                        conn.Open();
                        string sql2 = "Insert into user(userName, password) values('" + userName + "','" + password + "');";
                        MySqlCommand cmd2 = new MySqlCommand(sql2, conn);
                        cmd2.ExecuteNonQuery();
                        // 返回注册成功
                        var successResponse = new SuccessResponse();
                        successResponse.Code = 0;
                        successResponse.Message = "注册成功";
                        UserItem userItem = new UserItem();
                        userItem.Id = "1";
                        userItem.UserName = userName;
                        userItem.Password = password;
                        successResponse.User = userItem;
                        string successResponseJson = JsonConvert.SerializeObject(successResponse);
                        return successResponseJson;
                    }
                    else
                    {
                        conn.Close();
                        return getFailureResponse(1001, "验证码错误");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return getFailureResponse(-1, "未知错误");
        }

        private void ConnectToMySQL()
        {
            conn = new MySqlConnection(connetStr);
            try
            {
                Console.WriteLine("Connecting to MySQL...");
                conn.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.WriteLine("Done.");
        }

        class SuccessResponse
        {
            public Int16 Code { get; set; }
            public string Message { get; set; }
            public UserItem User { get; set; }
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
        }

        private string getFailureResponse(short code, string msg)
        {
            var failureResponse = new FailureResponse();
            failureResponse.Code = code;
            failureResponse.Message = msg;
            string failureResonpseJson = JsonConvert.SerializeObject(failureResponse);
            return failureResonpseJson;
        }
    }
}
