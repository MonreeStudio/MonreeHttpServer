using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MonreeHttpServer.MonreeListener.Email
{
    class SignUpCodeListener
    {
        private string prefixe;
        private static char[] constant ={'0','1','2','3','4','5','6','7','8','9'};
        private static string connetStr = "server=127.0.0.1;port=3306;user=Monree;password=laixiaxin1718990; database=msdatetest;";

        private MySqlConnection conn;

        public SignUpCodeListener(string prefixe)
        {
            this.prefixe = prefixe;
        }

        public void Start()
        {
            try
            {
                ThreadStart threadStart = new ThreadStart(StartSignUpCodeListener);
                Thread thread = new Thread(threadStart);
                thread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void StartSignUpCodeListener()
        {
            try
            {
                using (HttpListener listener = new HttpListener())
                {
                    listener.Prefixes.Add(prefixe);
                    listener.Start();
                    Console.WriteLine("开始监听注册验证码请求");
                    while (true)
                    {
                        try
                        {
                            HttpListenerContext context = listener.GetContext();//阻塞
                            string contentType = context.Request.ContentType;
                            HttpListenerRequest request = context.Request;
                            string postData = new StreamReader(request.InputStream).ReadToEnd();
                            Console.WriteLine("收到注册验证码请求：" + postData);
                            HttpListenerResponse response = context.Response;//响应
                            string responseBody = MonreeSignUpCode(postData, contentType);
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

        private string MonreeSignUpCode(string postData, string contentType)
        {
            ConnectToMySQL();
            if (!contentType.Equals("application/json"))
            {
                // 返回注册验证码失败
                var failureResponse = new FailureResponse();
                failureResponse.Code = -1;
                failureResponse.Message = "获取验证码失败";
                string failureResonpseJson = JsonConvert.SerializeObject(failureResponse);
                return failureResonpseJson;
            }
            string userEmail = "";
            try
            {
                JObject jo = JObject.Parse(postData);
                userEmail = jo["Email"].ToString();
                string code = GetVCode();
                string sql = "insert into verificationcode(code,email,timeStamp) values('"+ code + "','" + userEmail +"','" + DateTime.Now + "') on duplicate key update code = '" + code + "', timeStamp = '" + DateTime.Now + "'";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                int result = cmd.ExecuteNonQuery();
                conn.Close();
                Console.WriteLine("状态码：" + result);
                return SendMailUseGmail(userEmail, code);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return "";
        }

        private void ConnectToMySQL()
        {
            conn = new MySqlConnection(connetStr);
            try
            {
                Console.WriteLine("Connecting to MySQL...");
                conn.Open();
                // Perform database operations
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        class SuccessResponse
        {
            public Int16 Code { get; set; }
            public string Message { get; set; }
        }

        class FailureResponse
        {
            public Int16 Code { get; set; }
            public string Message { get; set; }
        }

        class UserItem
        {
            public string Email { get; set; }
        }

        private string GetVCode()
        {
            System.Random Random = new System.Random();
            int code = Random.Next(0, 9999);
            if(code < 1000)
            {
                return "0" + code;
            }
            return code + ""; 
        }

        private string SendMailUseGmail(string email, string code)
        {
            if (!IsEmail(email))
            {
                var failureResponse = new FailureResponse();
                failureResponse.Code = 1000;
                failureResponse.Message = "获取验证码失败：邮箱格式错误";
                string failureResonpseJson = JsonConvert.SerializeObject(failureResponse);
                return failureResonpseJson;
            }
            MailMessage msg = new MailMessage();
            msg.To.Add(email);
            msg.From = new MailAddress("bill@monreeing.com", "Monree Studio", Encoding.UTF8);
            /* 上面3个参数分别是发件人地址（可以随便写），发件人姓名，编码*/
            msg.Subject = "[夏日]注册验证码";//邮件标题    
            msg.SubjectEncoding = Encoding.UTF8;//邮件标题编码    
            msg.Body = "您的验证码：" + code + "，请在10分钟内完成验证；为保证账号安全，请勿转发验证码给他人。【梦睿】";//邮件内容    
            msg.BodyEncoding = Encoding.UTF8;//邮件内容编码    
            msg.IsBodyHtml = false;//是否是HTML邮件    
            msg.Priority = MailPriority.High;//邮件优先级    

            SmtpClient client = new SmtpClient();
            client.Credentials = new System.Net.NetworkCredential("bill@monreeing.com", "Biuyizythsfy1908");
            client.Port = 25;    
            client.Host = "smtp.mxhichina.com";
            client.EnableSsl = true;//经过ssl加密    
            try
            {
                client.Send(msg);
                Console.WriteLine("发送成功");
                // 返回注册成功
                var successResponse = new SuccessResponse();
                successResponse.Code = 0;
                successResponse.Message = "获取验证码成功";
                string successResponseJson = JsonConvert.SerializeObject(successResponse);
                return successResponseJson;
            }

            catch (SmtpException ex)

            {
                Console.WriteLine("发送失败：" + ex.Message);
                var failureResponse = new FailureResponse();
                failureResponse.Code = 2000;
                failureResponse.Message = "获取验证码失败:" + ex.Message;
                string failureResonpseJson = JsonConvert.SerializeObject(failureResponse);
                return failureResonpseJson;
            }

        }

        public bool IsEmail(string inputData)
        {
            Regex RegEmail = new Regex("^[\\w-]+@[\\w-]+\\.(com|net|org|edu|mil|tv|biz|info)$");
            Match m = RegEmail.Match(inputData);
            return m.Success;
        }
    }
}
