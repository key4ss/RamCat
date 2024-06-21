using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Management;
using System.Windows.Forms;

namespace RamCat
{
    public partial class Form1 : Form
    {
        private int currentIconIndex = 0;
        private System.Windows.Forms.Timer timer;
        private System.Windows.Forms.Timer iConTimer;
        private PerformanceCounter ramCounter;
        private float totalMemory;

        public Form1()
        {
            InitializeComponent();

            // ContextMenuStrip 추가
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("StartUp", null, StartUp_Click);
            contextMenu.Items.Add("Exit", null, ExitMenu_Click);
            notifyIcon1.ContextMenuStrip = contextMenu;

            // RAM PerformanceCounter 설정
            ramCounter = new PerformanceCounter("Memory", "Available Bytes");
            totalMemory = GetTotalPhysicalMemory();

            // Timer 설정
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += Timer_Tick;
            timer.Start();

            // Timer 설정
            iConTimer = new System.Windows.Forms.Timer();
            iConTimer.Interval = 1000;
            iConTimer.Tick += iconTimer_Tick;
            iConTimer.Start();

            // 폼이 처음 실행될 때 보이지 않도록 설정
            this.ShowInTaskbar = false;
            this.Opacity = 0;
            this.WindowState = FormWindowState.Minimized;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // RAM 사용량 체크
            CheckRAMUsage(sender, e);
        }

        private void iconTimer_Tick(object sender, EventArgs e)
        {
            // 아이콘 인덱스 업데이트 ( 0~9까지 순환)
            currentIconIndex = (currentIconIndex + 1) % 10;

            // 리소스에서 아이콘 가져오기
            string iconName = $"gaming_cat_page_{currentIconIndex}";
            Icon icon = (Icon)Properties.Resources.ResourceManager.GetObject(iconName);
            if (icon != null)
            {
                notifyIcon1.Icon = icon;
            }
        }

        private void ExitMenu_Click(object sender, EventArgs e)
        {
            notifyIcon1.Visible = false;
            Application.Exit();
        }

        private void StartUp_Click(object sender, EventArgs e)
        {
            try
            {
                // 현재 실행 파일의 경로를 가져옵니다.
                string executablePath = Application.ExecutablePath;

                // 레지스트리의 Run 키를 열어서 등록된 프로그램을 가져옵니다.
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    // 등록된 프로그램 목록을 가져옵니다.
                    string[] programNames = key.GetValueNames();

                    // 등록된 프로그램 목록 중에서 현재 프로그램이 등록되어 있는지 확인합니다.
                    bool alreadyRegistered = false;
                    foreach (string name in programNames)
                    {
                        if (name == "RamCat")
                        {
                            alreadyRegistered = true;
                            break;
                        }
                    }

                    if (alreadyRegistered)
                    {
                        MessageBox.Show("프로그램이 이미 윈도우 시작 프로그램으로 등록되어 있습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("프로그램이 윈도우 시작 프로그램으로 등록되어 있지 않습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        StartUp_Reg();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"확인 중 오류가 발생하였습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StartUp_Reg()
        {
            try
            {
                // 현재 실행 파일의 경로를 가져옵니다.
                string executablePath = Application.ExecutablePath;

                // 레지스트리의 Run 키를 열어서 프로그램을 등록합니다.
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    // 프로그램 이름을 "RamCat"으로 지정하고 실행 파일 경로를 등록합니다.
                    key.SetValue("RamCat", executablePath);
                }

                // 성공 메시지 출력
                MessageBox.Show("프로그램이 윈도우 시작 프로그램으로 등록되었습니다.", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // 실패 메시지 출력
                MessageBox.Show($"등록 중 오류가 발생하였습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CheckRAMUsage(object sender, EventArgs e)
        {
            // 시스템 메모리 사용량 업데이트
            // 전체 시스템 메모리 양 업데이트
            ulong totalMemory = GetTotalPhysicalMemory();
            double totalMemoryGB = totalMemory / (1024.0 * 1024.0 * 1024.0);
            // 현재 사용중이지 않은 메모리 양 업데이트
            ulong freeMemory = GetFreePhysicalMemory();
            double freeMemoryGB = freeMemory / (1024.0 * 1024.0);
            // 현재 사용중인 메모리 양 업데이트
            double usingMemoryGB = totalMemoryGB - freeMemoryGB;

            double ramUsage = usingMemoryGB / totalMemoryGB;

            // RAM 사용량에 따라 Timer 간격 변경
            int interval = (int)(ramUsage * 100);
            iConTimer.Interval = interval;

            // notifyIcon1의 이름 변경
            notifyIcon1.Text = $"RAM : {ramUsage:P}";
        }

        static ulong GetTotalPhysicalMemory()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            ManagementObjectCollection collection = searcher.Get();

            foreach (ManagementObject obj in collection)
            {
                return Convert.ToUInt64(obj["TotalPhysicalMemory"]);
            }

            return 0;
        }

        static ulong GetFreePhysicalMemory()
        {
            ulong freePhysicalMemory = 0;

            try
            {
                ManagementScope scope = new ManagementScope(@"\\.\root\cimv2");
                ObjectQuery query = new ObjectQuery("SELECT FreePhysicalMemory FROM Win32_OperatingSystem");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    freePhysicalMemory = (ulong)queryObj["FreePhysicalMemory"];
                }
            }
            catch (ManagementException ex)
            {
                Console.WriteLine("An error occurred while querying for WMI data: " + ex.Message);
            }

            return freePhysicalMemory;
        }
    }
}
