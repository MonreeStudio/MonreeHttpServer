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

namespace MonreeHttpServer.MonreeListener.Data
{
    class PullDataListener
    {
        private string prefixe;
        private MySqlConnection conn;
        private static string connetStr = "server=127.0.0.1;port=3306;user=Monree;password=laixiaxin1718990; database=msdatetest;charset=utf8";

        public PullDataListener(string prefixe)
        {
            this.prefixe = prefixe;
        }

        public void Start()
        {
            try
            {
                ThreadStart threadStart = new ThreadStart(StartPullDataListener);
                Thread thread = new Thread(threadStart);
                thread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        private void StartPullDataListener()
        {
            try
            {
                using (HttpListener listener = new HttpListener())
                {
                    listener.Prefixes.Add(prefixe);
                    listener.Start();
                    Console.WriteLine("开始监听拉取数据");
                    while (true)
                    {
                        try
                        {
                            HttpListenerContext context = listener.GetContext();//阻塞
                            string contentType = context.Request.ContentType;
                            HttpListenerRequest request = context.Request;
                            string postData = new StreamReader(request.InputStream).ReadToEnd();
                            Console.WriteLine("收到拉取请求：" + postData);
                            HttpListenerResponse response = context.Response;//响应
                            string responseBody = MonreePullData(postData, contentType);
                            response.ContentLength64 = System.Text.Encoding.UTF8.GetByteCount(responseBody);
                            response.ContentType = "text/html; Charset=UTF-8";
                            //输出响应内容
                            Stream output = response.OutputStream;
                            using (StreamWriter sw = new StreamWriter(output))
                            {
                                sw.Write(responseBody);
                            }
                            Console.WriteLine("响应拉取数据结束\n");
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

        private String MonreePullData(string postData, String contentType)
        {
            ConnectToMySQL();
            if (!contentType.Equals("application/json"))
            {
                return getFailureResponse(-1, "请求格式错误");
            }
            string userName = "";
            string token = "";
            try
            {
                JObject jo = JObject.Parse(postData);
                userName = jo["UserName"].ToString();
                token = jo["Token"].ToString();
                string sql0 = "Select *from user where token = '" + token + "';";
                MySqlCommand cmd0 = new MySqlCommand(sql0, conn);
                if (ConnectionState.Open != conn.State)
                {
                    conn.Open();
                }
                var result0 = cmd0.ExecuteReader();
                while(result0.Read())
                {
                    if (token.Equals("") || !token.Equals(result0.GetString("token")))
                    {
                        return getFailureResponse(2001, "登录状态已过期，请重新登录");
                    }
                }
                conn.Close();
                string sql1 = "Select *from todotask where userName = '" + userName + "';";
                MySqlCommand cmd1 = new MySqlCommand(sql1, conn);
                if (ConnectionState.Open != conn.State)
                {
                    conn.Open();
                }
                var result1 = cmd1.ExecuteReader();
                List<ToDoTask> taskList = new List<ToDoTask>();
                List<string> taskNameList = new List<string>();
                while (result1.Read())
                {
                    taskNameList.Add(result1.GetString("TaskName"));
                }
                conn.Close();
                foreach(var item in taskNameList)
                {
                    if (ConnectionState.Open != conn.State)
                    {
                        conn.Open();
                    }
                    string sql2 = "Select *from taskstep where taskName = '" + item + "';";
                    MySqlCommand cmd2 = new MySqlCommand(sql2, conn);
                    var result2 = cmd2.ExecuteReader();
                    List<TaskStep> stepList = new List<TaskStep>();
                    while (result2.Read())
                    {
                        stepList.Add(new TaskStep() { TaskName = result2.GetString("taskName"), Content = result2.GetString("content"), Done = result2.GetString("done"), UpdateTime = result2.GetString("updateTime"), IsDelete = result2.GetString("isDelete") });
                    }
                    conn.Close();
                    conn.Open();
                    string sql3 = "Select *from todotask where taskName = \"" + item + "\" and userName = '" + userName + "';";
                    MySqlCommand cmd3 = new MySqlCommand(sql3, conn);
                    var result3 = cmd3.ExecuteReader();
                    while(result3.Read())
                    {
                        taskList.Add(new ToDoTask() { TaskName = result3.GetString("taskName"), Date = result3.GetString("taskDate"), Star = result3.GetString("star"), Done = result3.GetString("done"), Remark = result3.GetString("remark"), UpdateTime = result3.GetString("updateTime"), IsDelete = result3.GetString("isDelete"), TaskStepList = stepList });

                    }
                    conn.Close();
                }
                var successResponse = new SuccessResponse();
                successResponse.Code = 0;
                successResponse.Message = "拉取云端数据成功";
                successResponse.TaskList = taskList;
                string successResponseJson = JsonConvert.SerializeObject(successResponse);
                return successResponseJson;
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return getFailureResponse(-1, "拉取云端数据错误：" + e.Message);
            }
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
            public List<ToDoTask> TaskList { get; set; }
        }

        class FailureResponse
        {
            public Int16 Code { get; set; }
            public string Message { get; set; }
        }

        class ToDoTask
        {
            public string TaskName { get; set; }
            public string Date { get; set; }
            public string Star { get; set; }
            public string Done { get; set; }
            public string Remark { get; set; }
            public string UpdateTime { get; set; }

            public string IsDelete { get; set; }

            public List<TaskStep> TaskStepList { get; set; }
        }

        class TaskStep
        {
            public string TaskName { get; set; }
            public string Content { get; set; }
            public string Done { get; set; }
            public string UpdateTime { get; set; }
            public string IsDelete { get; set; }
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
