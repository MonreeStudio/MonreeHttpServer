using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
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
        private MySqlConnection conn;
        private static string connetStr = "server=127.0.0.1;port=3306;user=Monree;password=laixiaxin1718990; database=msdatetest;";

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
            ConnectToMySQL();
            // 连接数据库查询
            if (!contentType.Equals("application/json"))
            {
                // 返回登录失败
                return getFailureResponse(1000, "登录失败：" + "请求数据格式错误");
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
                if (ConnectionState.Open != conn.State)
                {
                    conn.Open();
                }
                string token = Guid.NewGuid().ToString("N");
                string timeStamp = DateTime.Now.ToString();
                string sql0 = "Select *from user where userName = '" + userName + "';";
                MySqlCommand cmd0 = new MySqlCommand(sql0, conn);
                var result0 = cmd0.ExecuteReader();
                bool isExist = false;
                while(result0.Read())
                {
                    isExist = true;
                    if(result0.GetString("password").Equals(password))
                    {
                        conn.Close();
                        conn.Open();
                        string sql1 = "Update user set token = '" + token + "', tokenTimeStamp = '" + timeStamp + "' where userName = '" + userName + "';";
                        MySqlCommand cmd1 = new MySqlCommand(sql1, conn);
                        cmd1.ExecuteNonQuery();
                        conn.Close();
                        conn.Open();
                        string sql2 = "Select *from user where userName = '" + userName + "';";
                        MySqlCommand cmd2 = new MySqlCommand(sql2, conn);
                        var result2 = cmd2.ExecuteReader();
                        while(result2.Read())
                        {
                            // 返回登录成功
                            var successResponse = new SuccessResponse();
                            successResponse.Code = 0;
                            successResponse.Message = "登录成功";
                            UserItem userItem = new UserItem();
                            userItem.Id = result2.GetString("uid");
                            userItem.UserName = result2.GetString("userName");
                            userItem.Password = result2.GetString("password");
                            userItem.Token = result2.GetString("token");
                            userItem.TimeStamp = result2.GetString("tokenTimeStamp");
                            successResponse.User = userItem;
                            string successResponseJson = JsonConvert.SerializeObject(successResponse);
                            return successResponseJson;
                        }
                        
                    }
                    return getFailureResponse(1002, "登录失败：邮箱或密码错误");
                }
                if(!isExist)
                {
                    return getFailureResponse(1002, "登录失败：用户不存在");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return getFailureResponse(1001, "登录失败：" + e.Message);
            }
            return getFailureResponse(-1, "登录失败：未知原因");
            
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

        private string getFailureResponse(short code, string msg)
        {
            var failureResponse = new FailureResponse();
            failureResponse.Code = code;
            failureResponse.Message = msg;
            string failureResonpseJson = JsonConvert.SerializeObject(failureResponse);
            return failureResonpseJson;
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
    }
}
