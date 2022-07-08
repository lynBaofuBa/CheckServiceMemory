using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using SqlSugar;

namespace CheckServiceMemory
{
    class Program
    {
        //进程名称
        static string ProcessName = "Siemens.MES.AutomationGateway.GAC.WeldingPlant.Server";
        //服务名称
        static string ServiceName = "SiemensMESAutomationGateway$WeldingPlantServer";

        public static SqlSugarClient getSSDB()
        {
            return new SqlSugarClient(new ConnectionConfig()
            {
                ConnectionString = "Data Source=172.21.81.109;Initial Catalog=AlertDB;Persist Security Info=True;User ID=sa;Password=siemens12!@", //数据库连接字符串
                DbType = SqlSugar.DbType.SqlServer, //设置数据库类型
                IsAutoCloseConnection = true, //自动释放数据务，如果存在事务，在事务结束后释放
                InitKeyType = InitKeyType.Attribute //从实体特性中读取主键自增列信息
            });
        }

        static void Main(string[] args)
        {
            Console.WriteLine("开始了");
            var thread = new Thread(() =>
            {
                while (true)
                {
                    if (DateTime.Now.Hour >= 22 && DateTime.Now.Hour < 23)//
                    {
                        //得到进程
                        var process = GetProcessByName(ProcessName);
                        if (process != null)
                        {

                            var p1 = new PerformanceCounter("Process", "Working Set - Private", process.ProcessName);
                            float memorySizeKb = p1.NextValue() / 1024;
                            float memorySizeMb = p1.NextValue() / 1024/1024;
                            WriteLine("内存:" + memorySizeKb.ToString() + " KB,"+ memorySizeMb + "MB");
                            if (memorySizeMb > 1200) {
                                try
                                {
                                    process.Kill();
                                    WriteLine("关闭9服务器UAF接口进程！");
                                    StopService();
                                    WriteLine("停止9服务器UAF接口服务！");
                                    StartService();
                                    WriteLine("启动9服务器UAF接口服务！");
                                    using (SqlSugarClient sdb = getSSDB())
                                    {
                                        string alertInfo = "9服务器UAF接口程序内存大于" + memorySizeMb.ToString()+"MB自动重启";
                                        string alertGroupID = "2C6444B3-5084-4718-A42A-5E7D5006294C";
                                        string alertID = "11EA1A37-A203-4371-A6F9-CDF1B9294FA6";
                                        string sqlUpdate = string.Format(@"insert into AlertDB.dbo.WC_AlertMessage(MsgSubject,MsgContent,AlertClass,AlertMethod, MsgFormat, AlertID, AlertGroupID,SentNum,DeleteFlag,ArchiveFlag,CreateTime)
                                        select N'9服务器UAF接口服务重启提醒', N'{0}', 1, 2, 2, '{1}', '{2}', 0, 0, 0, GETDATE()  ", alertInfo, alertID, alertGroupID);
                                        int count0 = sdb.Ado.ExecuteCommand(sqlUpdate);
                                        if (count0 == 1)
                                        {
                                            WriteLine("微信预警成功！");
                                        }
                                        else
                                        {
                                            WriteLine("微信预警失败！");
                                        }
                                    }                                   
                                }
                                catch (Exception ex)
                                {
                                    WriteLine(ex.Message);
                                }
                            }
                        }
                        else
                        {
                            WriteLine("未找到进程");
                        }
                    }

                    // 模拟等待
                    Thread.Sleep(1000*10*60);

                }
            });
            thread.Start();
            Console.ReadKey();
        }

        /// <summary>
        /// 获取进程
        /// </summary>
        public static Process GetProcessByName(string strName)
        {
            Process[] ps = Process.GetProcesses();

            foreach (Process p in ps)
            {
                if (p.MainWindowHandle != null)
                {
                    if (p.ProcessName== strName)
                        return p;
                }
            }
            return null;
        }

        //输出
        public static void WriteLine(string str) {
            Console.WriteLine(DateTime.Now.ToString()+"——"+str);
        }

        /// <summary>
        /// 打开服务
        /// </summary>
        public static void StartService()
        {
            ProcessStartInfo a = new ProcessStartInfo(@"c:/windows/system32/cmd.exe", "/c  net start "+ ServiceName);
            a.WindowStyle = ProcessWindowStyle.Hidden;  
            Process process1 = Process.Start(a);
        }

        /// <summary>
        /// 关闭服务
        /// </summary>
        public static void StopService()
        {
            ProcessStartInfo a = new ProcessStartInfo(@"c:/windows/system32/cmd.exe", "/c  net stop "+ ServiceName);
            a.WindowStyle = ProcessWindowStyle.Hidden;  
            Process process1 = Process.Start(a);
        }

    }
}
