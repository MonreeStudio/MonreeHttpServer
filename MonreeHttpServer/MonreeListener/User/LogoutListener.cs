using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MonreeHttpServer.MonreeListener.User
{
    class LogoutListener
    {
        private string prefixe;
        private MySqlConnection conn;
        private static string connetStr = "server=127.0.0.1;port=3306;user=Monree;password=laixiaxin1718990; database=msdatetest;";

        public LogoutListener(string prefixe)
        {
            this.prefixe = prefixe;
        }

        public void Start()
        {
            try
            {
                ThreadStart threadStart = new ThreadStart(StartLogoutListener);
                Thread thread = new Thread(threadStart);
                thread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void StartLogoutListener()
        {
            try
            {
                using (HttpListener listener = new HttpListener())
                {
                    listener.Prefixes.Add(prefixe);
                    listener.Start();
                    Console.WriteLine("开始监听注销");
                    while (true)
                    {
                        try
                        {
                            HttpListenerContext context = listener.GetContext();//阻塞
                            string contentType = context.Request.ContentType;
                            HttpListenerRequest request = context.Request;
                            string postData = new StreamReader(request.InputStream).ReadToEnd();
                            Console.WriteLine("收到注销请求：" + postData);
                            HttpListenerResponse response = context.Response;//响应
                            string responseBody = MonreeLogout(postData, contentType);
                            Console.WriteLine("请求返回结果:\n" + responseBody);
                            response.ContentLength64 = Encoding.UTF8.GetByteCount(responseBody);
                            response.ContentType = "text/html; Charset=UTF-8";
                            //输出响应内容
                            Stream output = response.OutputStream;
                            using (StreamWriter sw = new StreamWriter(output))
                            {
                                sw.Write(responseBody);
                            }
                            Console.WriteLine("响应注销结束\n");
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

        private string MonreeLogout(string postData, String contentType)
        {
            ConnectToMySQL();
            // 连接数据库查询
            if (!contentType.Equals("application/json"))
            {
                // 返回注销失败
                return getResponse(1000, "注销失败：" + "请求数据格式错误");
            }

            string userName = "";
            try
            {
                JObject jo = JObject.Parse(postData);
                userName = jo["UserName"].ToString();
                if (ConnectionState.Open != conn.State)
                {
                    conn.Open();
                }
                string sql = "Update user set token = '" + "" + "',tokenTimeStamp = '" + "" + "' where userName = '" + userName + "';";
                MySqlCommand cmd0 = new MySqlCommand(sql, conn);
                cmd0.ExecuteNonQuery();
                return getResponse(0, "注销成功");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return getResponse(1001, "注销失败：" + e.Message);
            }

        }

        class Response
        {
            public Int16 Code { get; set; }
            public string Message { get; set; }
        }

        private string getResponse(short code, string msg)
        {
            var response = new Response();
            response.Code = code;
            response.Message = msg;
            string resonpseJson = JsonConvert.SerializeObject(response);
            return resonpseJson;
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
