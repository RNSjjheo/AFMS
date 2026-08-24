using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSExtraMonitor
{
    internal class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly ContextMenuStrip _trayMenu;
        private FormMain _frmMain;
        public TrayApplicationContext()
        {
            // 트레이 우클릭 메뉴 생성
            _trayMenu = new ContextMenuStrip();

            var statusMenuItem = new ToolStripMenuItem("펼처 보기");
            var exitMenuItem = new ToolStripMenuItem("종료");

            statusMenuItem.Click += StatusMenuItem_Click;
            exitMenuItem.Click += ExitMenuItem_Click;

            _trayMenu.Items.Add(statusMenuItem);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add(exitMenuItem);

            // 시스템 트레이 아이콘 생성
            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "New Watchdog",
                ContextMenuStrip = _trayMenu,
                Visible = true
            };

            // 더블클릭 이벤트
            _trayIcon.DoubleClick += TrayIcon_DoubleClick;

            // 프로그램 시작 알림
            _trayIcon.ShowBalloonTip(
                timeout: 3000,
                tipTitle: "프로그램 시작",
                tipText: "프로그램이 시스템 트레이에서 실행 중입니다.",
                tipIcon: ToolTipIcon.Info);

            _frmMain = new FormMain();
            _frmMain.Show();
            _frmMain.FormClosing += FrmMain_FormClosing;

            ShowMainForm();
        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            ShowStatus();
        }

        private void TrayIcon_DoubleClick(object? sender, EventArgs e)
        {
            ShowStatus();
        }

        private void StatusMenuItem_Click(object? sender, EventArgs e)
        {
            ShowStatus();
        }

        private void ShowStatus()
        {
            if (_frmMain.Visible)
            {
                HideMainForm();
            }
            else
            {
                ShowMainForm();
            }
        }

        private void ShowMainForm()
        {
            int margin = 5;

            _frmMain.ShowInTaskbar = true;
            _frmMain.StartPosition = FormStartPosition.Manual;
            _frmMain.Show();

            if (_frmMain.WindowState == FormWindowState.Minimized)
            {
                _frmMain.WindowState = FormWindowState.Normal;
            }

            Screen screen = Screen.FromPoint(Cursor.Position);
            Rectangle workingArea = screen.WorkingArea;

            _frmMain.Width = 600;
            _frmMain.Height = 800;
            _frmMain.Location = new Point(
                workingArea.Right - _frmMain.Width - margin,
                workingArea.Bottom - _frmMain.Height - margin);

            _frmMain.Activate();
            _frmMain.BringToFront();
        }

        private void HideMainForm()
        {
            _frmMain.Hide();
            _frmMain.ShowInTaskbar = false;
        }

        private void ExitMenuItem_Click(object? sender, EventArgs e)
        {
            ExitApplication();
        }

        private void ExitApplication()
        {
            // 프로그램 종료 후 아이콘이 남는 현상을 방지
            _trayIcon.Visible = false;

            ExitThread();
        }

        protected override void ExitThreadCore()
        {
            _trayIcon.Visible = false;

            _trayIcon.Dispose();
            _trayMenu.Dispose();

            base.ExitThreadCore();
        }
    }
}
