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
    class PushDataListener
    {
        private string prefixe;
        private MySqlConnection conn;
        private static string connetStr = "server=127.0.0.1;port=3306;user=Monree;password=laixiaxin1718990; database=msdatetest;charset=utf8";

        public PushDataListener(string prefixe)
        {
            this.prefixe = prefixe;
        }

        public void Start()
        {
            try
            {
                ThreadStart threadStart = new ThreadStart(StartPushDataListener);
                Thread thread = new Thread(threadStart);
                thread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void StartPushDataListener()
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
                            string responseBody = MonreePushData(postData, contentType);
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

        private String MonreePushData(string postData, String contentType)
        {
            ConnectToMySQL();
            if (!contentType.Equals("application/json"))
            {
                // 返回注册失败
                return getResponse(-1, "请求格式错误");
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
                while (result0.Read())
                {
                    if (token.Equals("") || !token.Equals(result0.GetString("token")))
                    {
                        return getResponse(2001, "登录状态已过期，请重新登录");
                    }
                }
                conn.Close();
                string taskListStr = jo["TaskList"].ToString();
                JArray taskListJson = (JArray)JsonConvert.DeserializeObject(taskListStr);
                List<ToDoTask> taskList = new List<ToDoTask>();
                for(int i = 0; i < taskListJson.Count; i++)
                {
                    var item = taskListJson[i];
                    ToDoTask task = new ToDoTask();
                    task.TaskName = item["TaskName"].ToString();
                    task.Date = item["Date"].ToString();
                    task.Star = item["Star"].ToString();
                    task.Done = item["Done"].ToString();
                    task.Remark = item["Remark"].ToString();
                    task.UpdateTime = item["UpdateTime"].ToString();
                    List<TaskStep> taskStepList = new List<TaskStep>();
                    string taskStepListStr = item["TaskStepList"].ToString();
                    JArray stepJson = (JArray)JsonConvert.DeserializeObject(taskStepListStr);
                    for(int j = 0; j < stepJson.Count; j++)
                    {
                        var subItem = stepJson[j];
                        TaskStep step = new TaskStep();
                        step.TaskName = subItem["TaskName"].ToString();
                        step.Content = subItem["Content"].ToString();
                        step.Done = subItem["Done"].ToString();
                        step.UpdateTime = subItem["UpdateTime"].ToString();
                        step.IsDelete = subItem["IsDelete"].ToString();
                        step.UserName = userName;
                        taskStepList.Add(step);
                    }
                    task.TaskStepList = taskStepList;
                    taskList.Add(task);
                }
                if (ConnectionState.Open != conn.State)
                {
                    conn.Open();
                }
                foreach(var item in taskList)
                {
                    List<TaskStep> tempStepList = item.TaskStepList;
                    foreach(var subItem in tempStepList)
                    {
                        if (ConnectionState.Open != conn.State)
                        {
                            conn.Open();
                        }
                        string sql1 = "Select *from taskStep where userName = '" + userName + "' and taskName = '" + subItem.TaskName + "' and content = '" + subItem.Content + "';";
                        MySqlCommand cmd1 = new MySqlCommand(sql1, conn);
                        var result1 = cmd1.ExecuteReader();
                        bool isStepExist = false;
                        while(result1.Read())
                        {
                            isStepExist = true;
                        }
                        conn.Close();
                        if (ConnectionState.Open != conn.State)
                        {
                            conn.Open();
                        }
                        if (isStepExist)
                        {
                            string sql3 = "Update taskStep set done = '" + subItem.Done + "', updateTime = '" + subItem.UpdateTime + "', isDelete = '" + subItem.IsDelete + "'" +
                                " where userName = '" + userName + "' and taskName = '" + subItem.TaskName + "' and content = '" + subItem.Content + "';";
                            MySqlCommand cmd3 = new MySqlCommand(sql3, conn);
                            cmd3.ExecuteNonQuery();
                        }
                        else
                        {
                            string sql4 = "Insert into taskStep(taskName, content, done, updateTime, isDelete, userName)" +
                                                        " values('" + subItem.TaskName + "','" + subItem.Content + "','" + subItem.Done + "','" + subItem.UpdateTime + "','" + subItem.IsDelete + "','" + subItem.UserName +  "')" +
                                                        " on duplicate key update done = '" + subItem.UpdateTime + "', updateTime = '" + subItem.UpdateTime + "', isDelete = '" + subItem.IsDelete + "';";
                            MySqlCommand cmd4 = new MySqlCommand(sql4, conn);
                            cmd4.ExecuteNonQuery();
                        }
                        conn.Close();
                    }
                    if (ConnectionState.Open != conn.State)
                    {
                        conn.Open();
                    }
                    string sql5 = "Select *from todotask where userName = '" + userName + "' and taskName = '" + item.TaskName + "';";
                    MySqlCommand cmd5 = new MySqlCommand(sql5, conn);
                    var result5 = cmd5.ExecuteReader();
                    bool isTaskExist = false;
                    while(result5.Read())
                    {
                        isTaskExist = true;
                    }
                    conn.Close();
                    conn.Open();
                    if (isTaskExist)
                    {
                        string sql6 = "Update todotask set taskDate = '" + item.Date + "', star = '" + item.Star + "', done = '" + item.Done + "', remark = '" + item.Remark + "', updateTime = '" + item.UpdateTime + "', isDelete = '" + item.IsDelete + "'" +
                            " where userName = '" + userName + "' and taskName = '" + item.TaskName + "';";
                        MySqlCommand cmd6 = new MySqlCommand(sql6, conn);
                        cmd6.ExecuteNonQuery();
                    }
                    else
                    {
                        string sql7 = "Insert into todotask(userName, taskName, taskDate, star, done, remark, updateTime, isDelete)" +
                                                " values('" + userName + "','" + item.TaskName + "','" + item.Date + "','" + item.Star + "','" + item.Done + "','" + item.Remark + "','" + item.UpdateTime + "','" + item.IsDelete + "')" +
                                                " on duplicate key update taskDate = '" + item.Date + "', star = '" + item.Star + "', done = '" + item.Done + "', remark = '" + item.Remark + "', updateTime = '" + item.UpdateTime + "', isDelete = '" + item.IsDelete + "';";
                        MySqlCommand cmd7 = new MySqlCommand(sql7, conn);
                        cmd7.ExecuteNonQuery();
                    }
                    conn.Close();
                }
                return getResponse(0, "上传成功");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return getResponse(-1, "数据上传云端错误：" + e.Message);
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

        class Response
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
            public string UserName { get; set; }
            public string Content { get; set; }
            public string Done { get; set; }
            public string UpdateTime { get; set; }
            public string IsDelete { get; set; }
        }

        private string getResponse(short code, string msg)
        {
            var failureResponse = new Response();
            failureResponse.Code = code;
            failureResponse.Message = msg;
            string failureResonpseJson = JsonConvert.SerializeObject(failureResponse);
            return failureResonpseJson;
        }
    }
}
