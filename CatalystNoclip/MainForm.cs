using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

using Catalyst;
using Catalyst.Memory;
using Catalyst.Input;
using Catalyst.Display;

namespace CatalystNoclip
{
    public partial class MainForm : Form
    {
        // Enable drop shadows
        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x20000;
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        Label boxToUpdate;
        string updateParamName;

        Timer mainNoclipLoop;
        bool gameIsRunning;
        bool gameIsLoading;
        MemoryManager memory;
        PlayerInfo pinfo;
        GameInfo ginfo;
        string noclipDisplayState;
        string speedDisplayState;
        string noStumbleDisplayState;

        SharedInfo dllInfo;
        IntPtr hMapFile;
        IntPtr mapFileDataPtr;
        
        bool noclipOn;
        bool noclipFT;
        bool allowAutoNC;
        bool noStumble;

        Vec3 pos;
        Vec3 lastpos;
        Vec3 velocity;
        int speed;

        static readonly long freq = Stopwatch.Frequency;

        public MainForm()
        {
            boxToUpdate = null;

            noclipOn = false;
            noclipFT = false;
            allowAutoNC = false;
            noStumble = false;

            lastpos = Vec3.Zero;
            velocity = Vec3.Zero;
            pos = Vec3.Zero;
            speed = 32;

            gameIsRunning = false;
            gameIsLoading = false;

            mainNoclipLoop = new Timer();
            mainNoclipLoop.Interval = 20;
            mainNoclipLoop.Tick += MainAppLoop;

            memory = new MemoryManager();
            pinfo = new PlayerInfo(memory);
            ginfo = new GameInfo(memory);

            noclipDisplayState = "NOCLIP OFF";
            speedDisplayState = "SPEED: 32";
            noStumbleDisplayState = "NOSTUMBLE OFF";

            Overlay.AddAutoField("ncstate", () => Properties.Settings.Default.ShowNCState ? noclipDisplayState : "");
            Overlay.AddAutoField("speed", () => Properties.Settings.Default.ShowSpeed ? speedDisplayState : "");
            Overlay.AddAutoField("nostumb", () => Properties.Settings.Default.ShowNSState ? noStumbleDisplayState : "");
            
            InitializeComponent();
        }

        #region noclip / nostumble toggle functions

        private void SetNoclip(bool state)
        {
            if (!(noclipOn ^ state))
                return;

            if (!state && noclipFT)
                SetNoclipFT(false);

            noclipOn = state;
            dllInfo.noclipState = state ? 1 : 0;
            InputController.SetToggledFlag((DIKCode)Properties.Settings.Default.ToggleNC, state);
        }

        private void SetNoclipFT(bool state)
        {
            if (!(noclipFT ^ state))
                return;

            if (!noclipOn && state)
                SetNoclip(true);

            noclipFT = state;
            ginfo.SetTimescale(state ? 0 : 1);
            InputController.SetToggledFlag((DIKCode)Properties.Settings.Default.SwitchNCMode, state);
        }

        private void SetNoStumble(bool state)
        {
            if (!(noStumble ^ state))
                return;

            noStumble = state;
            dllInfo.noStumbleState = state ? 1 : 0;
            InputController.SetToggledFlag((DIKCode)Properties.Settings.Default.NSHotkey, state);
        }

        #endregion

        #region app loop

        private void MainAppLoop(object sender, EventArgs e)
        {
            if (boxToUpdate != null)
            {
                var keys = InputController.GetPressedKeys();
                if (keys.Length == 0) return;

                var key = keys[0];
                if (key == DIKCode.ESCAPE) // ESC to cancel
                    key = (DIKCode)Properties.Settings.Default[updateParamName];

                Properties.Settings.Default[updateParamName] = (int)key;
                Properties.Settings.Default.Save();
                boxToUpdate.Text = key.ToString();
                boxToUpdate = null;

                InputController.MakeProcessSpecific("MirrorsEdgeCatalyst");
                return;
            }

            var p = Process.GetProcessesByName("MirrorsEdgeCatalyst");
            if (p.Length == 0 && gameIsRunning)
            {
                gameIsRunning = false;
                memory.ReleaseProcess();

                GameRunningLabel.Text = "NOT RUNNING";
                GameRunningLabel.ForeColor = Color.Red;

                Overlay.Disable();
            }
            else if (p.Length != 0)
            {
                bool lastIter = false;

                if (!gameIsRunning)
                {
                    gameIsRunning = true;
                    memory.OpenProcess("MirrorsEdgeCatalyst");
                    lastIter = true;

                    Overlay.Enable(true);
                }

                bool loading = false;
                try
                {
                    loading = ginfo.IsLoading();
                }
                catch (Exception)
                {
                    loading = true;
                }

                if (loading && (!gameIsLoading || lastIter))
                {
                    SetNoclip(false);
                    gameIsLoading = true;
                    GameRunningLabel.ForeColor = Color.Goldenrod;
                    GameRunningLabel.Text = "LOADING";

                    noclipOn = false;
                    noclipFT = false;
                }
                else if (!loading && (gameIsLoading || lastIter))
                {
                    gameIsLoading = false;
                    GameRunningLabel.ForeColor = Color.Green;
                    GameRunningLabel.Text = "RUNNING";
                }

                if (!gameIsLoading)
                {
                    ToggleLoop();
                }

                // Handle overlay text
                bool d = Properties.Settings.Default.OnlyShowWhenTrue;
                speedDisplayState = (noclipOn || !d) ? "SPEED: " + speed.ToString() : "";
                noclipDisplayState = noclipOn ? "NOCLIP ON [" + (noclipFT ? "FT" : "RT") + "]" : (d ? "" : "NOCLIP OFF");
                noStumbleDisplayState = noStumble ? "NOSTUMBLE ON" : (d ? "" : "NOSTUMBLE OFF");
            }
        }

        private void ToggleLoop()
        {
            DIKCode rtoggle = (DIKCode)Properties.Settings.Default.ToggleNC;
            DIKCode ftoggle = (DIKCode)Properties.Settings.Default.SwitchNCMode;
            DIKCode ntoggle = (DIKCode)Properties.Settings.Default.NSHotkey;
            DIKCode speedu = (DIKCode)Properties.Settings.Default.FasterHotkey;
            DIKCode speedd = (DIKCode)Properties.Settings.Default.SlowerHotkey;

            if (!noclipOn)
            {
                pos = pinfo.GetPosition();
                velocity = (pos - lastpos) * 15;
                lastpos = pos;

                var mstate = pinfo.GetMovementState();
                if (mstate != MovementState.Crouching)
                    allowAutoNC = true;

                else if (
                    Properties.Settings.Default.AutoNoclip && 
                    allowAutoNC && 
                    Math.Abs(velocity.y + 2.45) < 0.1)
                {
                    allowAutoNC = false;
                    SetNoclip(true);
                }
            }

            SetNoclip(InputController.IsKeyToggled(rtoggle));
            SetNoclipFT(InputController.IsKeyToggled(ftoggle));
            SetNoStumble(InputController.IsKeyToggled(ntoggle));

            if (InputController.OnKeyDown(speedu))
            {
                if (speed < 2048) speed *= 2;
                speedDisplayState = "SPEED:" + speed.ToString();
            }
            if (InputController.OnKeyDown(speedd))
            {
                if (speed > 1) speed /= 2;
                speedDisplayState = "SPEED:" + speed.ToString();
            }

            if (noclipOn)
            {
                Vec3 dpos = Vec3.Zero;
                Vec3 yawv = pinfo.GetCameraYawVector();

                if (InputController.IsGameActionPressed(GameAction.MoveForward))
                    dpos += yawv;
                if (InputController.IsGameActionPressed(GameAction.MoveBackward))
                    dpos -= yawv;
                if (InputController.IsGameActionPressed(GameAction.MoveLeft))
                    dpos += yawv.Left;
                if (InputController.IsGameActionPressed(GameAction.MoveRight))
                    dpos += yawv.Right;
                if (InputController.IsGameActionPressed(GameAction.Jump))
                    dpos += Vec3.AxisY;
                if (InputController.IsGameActionPressed(GameAction.DownActions))
                    dpos -= Vec3.AxisY;

                dllInfo.dpos = dpos * speed;
            }

            FileMappingWrapper.WriteGenericToFileMap(mapFileDataPtr, dllInfo);
        }

        #endregion

        #region app events

        private void MainForm_Load(object sender, EventArgs e)
        {
            RTInputBox.Click += (o, s) => SetHotkey(RTInputBox, "ToggleNC");
            FTInputBox.Click += (o, s) => SetHotkey(FTInputBox, "SwitchNCMode");
            MFInputBox.Click += (o, s) => SetHotkey(MFInputBox, "FasterHotkey");
            MSInputBox.Click += (o, s) => SetHotkey(MSInputBox, "SlowerHotkey");
            NSInputBox.Click += (o, s) => SetHotkey(NSInputBox, "NSHotkey");

            dllInfo = new SharedInfo();
            dllInfo.injectorPID = Process.GetCurrentProcess().Id;

            hMapFile = FileMappingWrapper.CreateFileMapping("NoclipDllFileMapping", 24);
            mapFileDataPtr = FileMappingWrapper.GetFileMapPtr(hMapFile, 24);
            FileMappingWrapper.WriteGenericToFileMap(mapFileDataPtr, dllInfo);

            mainNoclipLoop.Start();

            UpdateSettings();
            InputController.MakeProcessSpecific("MirrorsEdgeCatalyst");
            InputController.EnableInputHook();

            Overlay.Enable(true);
        }

        private void XButton_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            /* For some reason the process refuses to close unless I force quit */
            ForceClose();
        }

        private void ForceClose()
        {
            mainNoclipLoop.Stop();

            Overlay.Disable();
            InputController.DisableInputHook();
            memory.ReleaseProcess();

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C taskkill /F /IM CatalystNoclip.exe";
            process.StartInfo = startInfo;
            process.Start();
        }

        private void XButton_MouseEnter(object sender, EventArgs e)
        {
            XButton.LinkColor = Color.Red;
        }

        private void XButton_MouseLeave(object sender, EventArgs e)
        {
            XButton.LinkColor = Color.White;
        }

        private void Minimize_MouseEnter(object sender, EventArgs e)
        {
            Minimize.LinkColor = ForeColor;
        }

        private void Minimize_MouseLeave(object sender, EventArgs e)
        {
            Minimize.LinkColor = Color.White;
        }

        private void Minimize_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        #endregion

        #region injection

        private void InjectDLL()
        {
            DllInjectionResult r = DllInjector.Instance.Inject("MirrorsEdgeCatalyst", "NoclipInjectedDLL.dll");
            if (r == DllInjectionResult.Success)
                MessageBox.Show(this, "Dll sucessfully injected!", "MEC: Noclip", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
            {
                MessageBox.Show(this, "Dll could not be injected, reason: " + r.ToString(), "MEC: Noclip", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion injection

        #region enable dragging on borderless form

        const int WM_NCLBUTTONDOWN = 0xA1;
        const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        private void panel2_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void title_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }


        #endregion

        #region settings & hotkeys

        private void UpdateSettings()
        {
            RTInputBox.Text = ((DIKCode)Properties.Settings.Default.ToggleNC).ToString();
            FTInputBox.Text = ((DIKCode)Properties.Settings.Default.SwitchNCMode).ToString();
            MFInputBox.Text = ((DIKCode)Properties.Settings.Default.FasterHotkey).ToString();
            MSInputBox.Text = ((DIKCode)Properties.Settings.Default.SlowerHotkey).ToString();
            NSInputBox.Text = ((DIKCode)Properties.Settings.Default.NSHotkey).ToString();
            AutoNoclipCheckbox.Checked = Properties.Settings.Default.AutoNoclip;

            OverlayCheckbox.Checked = Properties.Settings.Default.ShowOverlay;
            NCStateCheckbox.Checked = Properties.Settings.Default.ShowNCState;
            SpeedCheckbox.Checked = Properties.Settings.Default.ShowSpeed;
            NSStateCheckbox.Checked = Properties.Settings.Default.ShowNSState;
            OSECheckbox.Checked = Properties.Settings.Default.OnlyShowWhenTrue;
        }

        private void ResetBtn_Click(object sender, EventArgs e)
        {
            CancelSetHotkey();

            Properties.Settings.Default.ToggleNC = 59;
            Properties.Settings.Default.SwitchNCMode = 60;
            Properties.Settings.Default.FasterHotkey = 200;
            Properties.Settings.Default.SlowerHotkey = 208;
            Properties.Settings.Default.AutoNoclip = false;
            Properties.Settings.Default.NSHotkey = 61;
            Properties.Settings.Default.ShowOverlay = true;
            Properties.Settings.Default.ShowNCState = true;
            Properties.Settings.Default.ShowSpeed = true;
            Properties.Settings.Default.ShowNSState = true;
            Properties.Settings.Default.OnlyShowWhenTrue = true;
            Properties.Settings.Default.Save();

            UpdateSettings();
        }

        private void SetHotkey(Label box, string paramName)
        {
            if (boxToUpdate != null)
            {
                if (boxToUpdate == box) { return; }
                boxToUpdate.Text = ((DIKCode)Properties.Settings.Default[updateParamName]).ToString();
            }

            InputController.MakeProcessSpecific("CatalystNoclip");

            box.Text = "Press a key...";
            boxToUpdate = box;
            updateParamName = paramName;
        }

        private void CancelSetHotkey()
        {
            if (boxToUpdate == null) return;

            boxToUpdate.Text = ((DIKCode)Properties.Settings.Default[updateParamName]).ToString();
            boxToUpdate = null;

            InputController.MakeProcessSpecific("MirrorsEdgeCatalyst");
        }

        private void MainForm_Deactivate(object sender, EventArgs e)
        {
            CancelSetHotkey();
        }

        #endregion

        #region checkboxes

        private void AutoNoclipCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            CancelSetHotkey();

            Properties.Settings.Default.AutoNoclip = AutoNoclipCheckbox.Checked;
            Properties.Settings.Default.Save();
        }

        private void OverlayCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            CancelSetHotkey();

            SpeedCheckbox.AutoCheck = OverlayCheckbox.Checked;
            SpeedCheckbox.ForeColor = OverlayCheckbox.Checked ? Color.White : Color.DimGray;
            NCStateCheckbox.AutoCheck = OverlayCheckbox.Checked;
            NCStateCheckbox.ForeColor = OverlayCheckbox.Checked ? Color.White : Color.DimGray;
            NSStateCheckbox.AutoCheck = OverlayCheckbox.Checked;
            NSStateCheckbox.ForeColor = OverlayCheckbox.Checked ? Color.White : Color.DimGray;
            OSECheckbox.AutoCheck = OverlayCheckbox.Checked;
            OSECheckbox.ForeColor = OverlayCheckbox.Checked ? Color.White : Color.DimGray;

            Properties.Settings.Default.ShowOverlay = OverlayCheckbox.Checked;
            Properties.Settings.Default.Save();

            Overlay.Displaying = OverlayCheckbox.Checked;
        }

        private void NCStateCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            CancelSetHotkey();

            Properties.Settings.Default.ShowNCState = NCStateCheckbox.Checked;
            Properties.Settings.Default.Save();
        }

        private void SpeedCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            CancelSetHotkey();

            Properties.Settings.Default.ShowSpeed = SpeedCheckbox.Checked;
            Properties.Settings.Default.Save();
        }

        private void NSStateCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            CancelSetHotkey();

            Properties.Settings.Default.ShowNSState = NSStateCheckbox.Checked;
        }

        private void OSECheckbox_CheckedChanged(object sender, EventArgs e)
        {
            CancelSetHotkey();

            Properties.Settings.Default.OnlyShowWhenTrue = OSECheckbox.Checked;
            Properties.Settings.Default.Save();
        }

        #endregion

        #region kys (WIP)

        private void KysButton_Click(object sender, EventArgs e)
        {
            if (gameIsRunning && !gameIsLoading)
            {
                SetNoclip(false);

                // This should kill the player, but since the game only does
                // height checks when we are airborne, the player must jump.
                float yc = pinfo.GetPosition().y + 1000;
                pinfo.SetLastGroundYCoord(yc);
            }
        }

        #endregion
    }
}
