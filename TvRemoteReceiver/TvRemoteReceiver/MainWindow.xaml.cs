using AudioSwitcher.AudioApi.CoreAudio;
using System;
using System.Drawing;
using System.IO.Ports;
using System.Threading;
using System.Timers;
using System.Windows;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MessageBox = System.Windows.MessageBox;
using Point = System.Drawing.Point;
using Timer = System.Timers.Timer;
using WinForms = System.Windows.Forms;


namespace TvRemoteReceiver
{
    public partial class MainWindow : Window
    {
        private readonly System.Timers.Timer _receiveTimer;
        private readonly int _mouseMoveSteps;
        private readonly int _mouseMove;
        private int _mouseMoveHold;

        private Mutex _mutex;
        private SerialPort _port;
        private CoreAudioController _audio;
        private System.Windows.Forms.NotifyIcon _appNotifyIcon;

        private string _lastResult = "";
        private bool _isMute = false;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, IntPtr extraInfo);

        public MainWindow()
        {
            if (!this.IsSingleInstance())
            {
                MessageBox.Show("Aplikacja jest już uruchomiona");
                System.Windows.Application.Current.Shutdown();
            }
            InitializeComponent();

            _mouseMoveSteps = 7;
            _mouseMove = 5;
            _mouseMoveHold = 20;

            _receiveTimer = new Timer(30);
            _receiveTimer.Elapsed += new ElapsedEventHandler(OnTimeEvent);

            NotifyConfig();
            RemoteConfig();
        }

        private void RemoteConfig()
        {
            try
            {
                _port = new SerialPort("COM3", 9601);
                _port.Open();
                txbConnection.Content = _port.PortName;
                _audio = new CoreAudioController();
                _receiveTimer.Enabled = true;
            }
            catch (System.IO.IOException ex)
            {
                txbConnection.Content = "Błąd w nawiązaniu połączenia: " + ex.Message;
            }
        }

        private void OnTimeEvent(object source, ElapsedEventArgs e) => RemoteControl();

        #region Notify
        private void NotifyConfig()
        {
            this._appNotifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = new System.Drawing.Icon(@"C:\Remote.ico")
            };

            //Notify Events
            this._appNotifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(AppNotifyIcon_MouseDoubleClick);
            this._appNotifyIcon.MouseDown += new WinForms.MouseEventHandler(AppNotifyIcon_MouseDown);
        }
        void AppNotifyIcon_MouseDown(object sender, WinForms.MouseEventArgs e)
        {
            if (e.Button == WinForms.MouseButtons.Right)
                ((ContextMenu)this.FindResource("NotifierContextMenu")).IsOpen = true;
        }
        void AppNotifyIcon_MouseDoubleClick(object sender, WinForms.MouseEventArgs e) => this.WindowState = WindowState.Normal;
        private void Menu_Open(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Normal;
        private void Menu_Close(object sender, RoutedEventArgs e) => System.Windows.Application.Current.Shutdown();
        #endregion

        #region Window's envets
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.ShowInTaskbar = false;
                _appNotifyIcon.Visible = true;
            }
            else if (this.WindowState == WindowState.Normal)
            {
                _appNotifyIcon.Visible = false;
                this.ShowInTaskbar = true;
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.ShowInTaskbar = false;
            _appNotifyIcon.Visible = true;
            e.Cancel = true;
            this.WindowState = WindowState.Minimized;
        }
        #endregion


        private void RemoteControl()
        {
            string res = String.Empty;
            try
            {
                _receiveTimer.Enabled = false; //stop receiveing

                res = _port.ReadLine();
                if (!String.IsNullOrEmpty(res))
                {
                    RemoteRes remote = Newtonsoft.Json.JsonConvert.DeserializeObject<RemoteRes>(res);// parse receive to object

#if !Release
                    Console.WriteLine(remote.Result + " " + remote.Hex + " " + remote.Hold);
#endif
                    txbReceive.Dispatcher.Invoke(() => { txbReceive.Content = res + remote.Result + " " + remote.Hex + " " + remote.Hold; });//show ress

                    switch (remote.Result)
                    {
                        #region Audio
                        case ButtonName.VolMute:
                            _audio.DefaultPlaybackDevice.Mute(!_isMute);
                            _isMute = !_isMute;
                            break;
                        case ButtonName.VolUp:
                            _audio.DefaultPlaybackDevice.Volume += remote.Hold ? 3 : 1;
                            break;
                        case ButtonName.VolDown:
                            _audio.DefaultPlaybackDevice.Volume -= remote.Hold ? 3 : 1;
                            break;
                        case ButtonName.BtnRed://Stop/Play music
                            if (!remote.Hold)
                                keybd_event(0xB3, 0, 1, IntPtr.Zero);
                            break;
                        case ButtonName.BtnGreen://Next //0xB1-prev
                            if (!remote.Hold)
                                keybd_event(0xB0, 0, 1, IntPtr.Zero);
                            break;

                        #endregion

                        #region Mouse stering
                        case ButtonName.CrossUp:
                            LinearSmoothMove(new Point(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y - (remote.Hold ? GetMouseMoveHold(remote.Hex) : _mouseMove)), _mouseMoveSteps);
                            break;
                        case ButtonName.CrossDown:
                            LinearSmoothMove(new Point(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y + (remote.Hold ? GetMouseMoveHold(remote.Hex) : _mouseMove)), _mouseMoveSteps);
                            break;
                        case ButtonName.CrossLeft:
                            LinearSmoothMove(new Point(System.Windows.Forms.Cursor.Position.X - (remote.Hold ? GetMouseMoveHold(remote.Hex) : _mouseMove), System.Windows.Forms.Cursor.Position.Y), _mouseMoveSteps);
                            break;
                        case ButtonName.CrossRight:
                            LinearSmoothMove(new Point(System.Windows.Forms.Cursor.Position.X + (remote.Hold ? GetMouseMoveHold(remote.Hex) : _mouseMove), System.Windows.Forms.Cursor.Position.Y), _mouseMoveSteps);
                            break;
                        case ButtonName.CrossOk:
                            mouse_event(0x02, System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y, 0, 0);//MOUSEEVENTF_LEFTDOWN
                            mouse_event(0x04, System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y, 0, 0);//MOUSEEVENTF_LEFTUP
                            break;
                        default:
                            break;
                            #endregion
                    }
                }

                _receiveTimer.Enabled = true;//start receiveing

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "RES: " + res);
                _port.Close();
                _port.Open();
            }
        }

        #region Helpers
        private int GetMouseMoveHold(string result)
        {
            if (_lastResult == result)
            {
                _mouseMoveHold += 4;
            }
            else
            {
                _lastResult = result;
                _mouseMoveHold = 20;
            }
#if !Release
            Console.WriteLine("Mouse move speed:" + _mouseMoveHold);
#endif
            return _mouseMoveHold;
        }
        private void LinearSmoothMove(Point newPosition, int steps)
        {
            Point start = new Point(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y);
            PointF iterPoint = start;

            // Find the slope of the line segment defined by start and newPosition
            PointF slope = new PointF(newPosition.X - start.X, newPosition.Y - start.Y);

            // Divide by the number of steps
            slope.X = slope.X / steps;
            slope.Y = slope.Y / steps;

            // Move the mouse to each iterative point.
            for (int i = 0; i < steps; i++)
            {
                iterPoint = new PointF(iterPoint.X + slope.X, iterPoint.Y + slope.Y);
                System.Windows.Forms.Cursor.Position = Point.Round(iterPoint);
                Thread.Sleep(10);
            }
            // Move the mouse to the final destination.
            System.Windows.Forms.Cursor.Position = newPosition;
        }
        private bool IsSingleInstance()
        {
            try
            {
                // Try to open existing mutex.
                Mutex.OpenExisting("fbd52dfe-54a4-4ca8-af1c-66cbe3d5c83b");
            }
            catch
            {
                // If exception occurred, there is no such mutex.
                this._mutex = new Mutex(true, "fbd52dfe-54a4-4ca8-af1c-66cbe3d5c83b");
                // Only one instance.
                return true;
            }
            // More than one instance.
            return false;
        }
        #endregion

    }
}
