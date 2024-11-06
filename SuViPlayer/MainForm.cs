using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using System.Text.RegularExpressions;
using System.Text; 
using System.Linq;
using System.Runtime.InteropServices; // You need this to import DwmSetWindowAttribute
using System.Diagnostics;

namespace SuViPlayer
{
    public partial class MainForm : Form
    {

        // Import DwmSetWindowAttribute from dwmapi.dll
        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attribute,
                                                         ref int pvAttribute, uint cbAttribute);

        // Define the DWMWINDOWATTRIBUTE enum
        private enum DWMWINDOWATTRIBUTE : uint
        {
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
            // Other attributes can be added here if needed
        }

        private void EnableDarkMode()
        {
            // Set the immersive dark mode attribute for the form handle
            int preference = 1; // 1 enables dark mode, 0 disables it
            DwmSetWindowAttribute(this.Handle,
                                  DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                                  ref preference,
                                  sizeof(int));
        }


        // Add new fields for timeline controls
        private TrackBar timelineTrackBar;
        private Label currentTimeLabel;
        private Label totalTimeLabel;
        private bool isTimelineBeingDragged = false;


        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private VideoView videoView;
        private Panel controlsPanel;
        private Panel menuBar;
        private int currentSubtitleIndex = 0;
        private bool isLooping = false;
        private Timer loopTimer;
        private TrackBar volumeSlider;
        private Label volumeLabel;
        private Button playPauseButton;
        private Button stopButton;
        private Button loopButton;

        // Add the missing subtitles list as a class field
        private List<SubtitleParser.SubtitleEntry> subtitles;

        // Add a timer for updating the timeline
        private Timer timeUpdateTimer;

        //// Add a new field to track video end state
        private bool hasVideoEnded = false;

        private NumericUpDown subUpDown;

        // Add this field to your form class
        private Label subtitleTrackingLabel;

        private int realTimeSubtitleIndex = 0;

        private bool isAutoLoop = false;


        // Add these members to your MainForm class
        //private Button showSubtitlesButton;
        private SubtitleListForm subtitleListForm;

        private bool isVideoViewMaximized = false;
        private FormWindowState lastWindowState;
        // Variable to store the last known screen bounds before going fullscreen.
        private Rectangle lastScreenBounds;

        // Add these fields to the MainForm class
        private NumericUpDown repeatCountUpDown;
        private int currentRepeatCount = 1;
        private int targetRepeatCount = 0;
        //private int lastProcessedSubtitleIndex = -1;


        // Add these fields at the class level
        private bool isHandlingEnter = false;
        private bool isHandlingEscape = false;
        private bool isHandlingL = false;
        private bool isHandlingUp = false;
        private bool isHandlingDown = false;
        private bool isHandlingLeft = false;
        private bool isHandlingRight = false;
        private bool isHandlingSpace = false;
        private bool isHandlingOemMinus = false;
        private bool isHandlingOemplus = false;
        private bool isHandlingO = false;
        private bool isHandlingOemSemicolon = false;
        private bool isHandlingOemQuotes = false;
        private bool isHandlingH = false;
        private bool isHandlingS = false;

        private const int VOLUME_STEP = 10; // Adjust this value to change how much the volume changes with each key press

        // 1. Add this field to your MainForm class (alongside other private fields)
        private NumericUpDown speedNumericUpDown;

        private Label notificationLabel;
        private Timer notificationTimer;

        private const string APP_NAME = "SuVi Player";

        private bool isSubtitlesEnabled = true;
        private int currentSubtitleTrack_mediaPlayer_Spu;
        public MainForm()
        {
            //Application.UseWaitCursor = true;
            //this.Cursor = Cursors.WaitCursor;  // Show loading cursor

            //var sw = Stopwatch.StartNew();

            InitializeComponent();

            //Debug.WriteLine($"InitializeComponent(); took: {sw.ElapsedMilliseconds}ms");

            //sw.Restart();

            SetupUI();

            //Debug.WriteLine($"SetupUI(); took: {sw.ElapsedMilliseconds}ms");

            //sw.Restart();

            InitializeVLC();

            //Debug.WriteLine($"InitializeVLC(); took: {sw.ElapsedMilliseconds}ms");


            //sw.Restart();

            // Initialize the subtitles list
            subtitles = new List<SubtitleParser.SubtitleEntry>();

            loopTimer = new Timer();
            loopTimer.Interval = 100; // Check every 100ms
            loopTimer.Tick += LoopTimer_Tick;

            // Initialize timeline update timer
            timeUpdateTimer = new Timer();
            timeUpdateTimer.Interval = 500; // Update every 500ms
            timeUpdateTimer.Tick += TimeUpdateTimer_Tick;
            timeUpdateTimer.Start();

            // Initialize lastScreenBounds with the form's current bounds.
            lastScreenBounds = this.Bounds;
            lastWindowState = this.WindowState;

            // Initialize notificationTimer
            notificationTimer = new Timer();
            notificationTimer.Interval = 2000; // Update every 2s
            notificationTimer.Tick += NotificationTimer_Tick;

            // Initialize the notification label
            notificationLabel = new Label();
            notificationLabel.AutoSize = true;
            notificationLabel.Location = new Point(20, 40);
            notificationLabel.Padding = new Padding(5);
            notificationLabel.Visible = false;
            notificationLabel.Font = new Font("Segoe UI", 16F);
            notificationLabel.Text = "notificationLabel.Text";
            this.Controls.Add(notificationLabel);
            notificationLabel.BringToFront();

            //Debug.WriteLine($"the rest of them took: {sw.ElapsedMilliseconds}ms");

            //sw.Restart();

            // Enable dark mode for the title bar
            EnableDarkMode();

            //Debug.WriteLine($"EnableDarkMode(); took: {sw.ElapsedMilliseconds}ms");

            // sw.Stop();  // Stop the stopwatch when done

            // this.Cursor = Cursors.Default;  // Reset cursor
            //Application.UseWaitCursor = false;
        }

        private void SetupUI()
        {
            // Main form settings
            this.Size = new Size(810, 600);
            //this.Text = "Video Player with Subtitles";
            this.Text = APP_NAME;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            // Add keyboard handling for the form
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;
            this.KeyUp += MainForm_KeyUp;
            this.SizeChanged += MainForm_SizeChanged;

            // Enable drag and drop for the entire form
            this.AllowDrop = true;

            // Add event handlers for drag and drop
            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDrop;


            // Video view
            videoView = new VideoView();
            videoView.Dock = DockStyle.Fill;
            videoView.Size = new Size(800, 400);

            // Controls panel
            controlsPanel = new Panel();
            controlsPanel.Dock = DockStyle.Bottom;
            controlsPanel.Height = 105; // Increased height to accommodate timeline controls
            controlsPanel.AutoScroll = true;





            // Timeline controls
            timelineTrackBar = new TrackBar();
            timelineTrackBar.Location = new Point(58, 75);
            timelineTrackBar.Width = 677;
            timelineTrackBar.Maximum = 1000; // We'll convert video duration to this scale
            //timelineTrackBar.TickFrequency = 0;
            timelineTrackBar.TickStyle = TickStyle.None;
            timelineTrackBar.AutoSize = false;
            timelineTrackBar.Height = 25;
            timelineTrackBar.TabStop = false;
            timelineTrackBar.MouseDown += TimelineTrackBar_MouseDown;
            timelineTrackBar.MouseUp += TimelineTrackBar_MouseUp;
            //timelineTrackBar.BackColor = Color.White;

            currentTimeLabel = new Label();
            currentTimeLabel.Location = new Point(10, 79);
            currentTimeLabel.Text = "00:00:00";
            currentTimeLabel.AutoSize = true;

            totalTimeLabel = new Label();
            totalTimeLabel.Location = new Point(735, 79);
            totalTimeLabel.Text = "00:00:00";
            totalTimeLabel.AutoSize = true;

            // Existing controls...
            Button loadButton = new Button();
            loadButton.Text = "Load Video";
            loadButton.Location = new Point(10, 10);
            loadButton.TabStop = false;
            loadButton.Click += LoadButton_Click;
            loadButton.ApplyCustomStyle();

            playPauseButton = new Button();
            playPauseButton.Text = "Pause";
            playPauseButton.Location = new Point(90, 10);
            playPauseButton.Width = 80;
            playPauseButton.Enabled = false;
            playPauseButton.TabStop = false;
            playPauseButton.Click += PlayPauseButton_Click;
            playPauseButton.ApplyCustomStyle();

            stopButton = new Button();
            stopButton.Text = "Stop";
            stopButton.Location = new Point(175, 10);
            stopButton.Width = 80;
            stopButton.Enabled = false;
            stopButton.TabStop = false;
            stopButton.Click += StopButton_Click;
            stopButton.ApplyCustomStyle();

            Button prevSubButton = new Button();
            prevSubButton.Text = "Previous Subtitle";
            prevSubButton.Location = new Point(260, 10);
            prevSubButton.TabStop = false;
            prevSubButton.Click += PrevSubButton_Click;
            prevSubButton.ApplyCustomStyle();

            Button nextSubButton = new Button();
            nextSubButton.Text = "Next Subtitle";
            nextSubButton.Location = new Point(340, 10);
            nextSubButton.TabStop = false;
            nextSubButton.Click += NextSubButton_Click;
            nextSubButton.ApplyCustomStyle();

            loopButton = new Button();
            loopButton.Text = "Loop: off";
            loopButton.Location = new Point(420, 10);
            loopButton.TabStop = false;
            loopButton.Click += LoopButton_Click;
            loopButton.ApplyCustomStyle();


            //Label subLabel = new Label();
            //subLabel.Text = "Subtitles:";
            //subLabel.Location = new Point(555, 47);
            //subLabel.AutoSize = true;

            subUpDown = new NumericUpDown();
            subUpDown.Size = new Size(60, 20);
            subUpDown.Location = new Point(500, 11);
            subUpDown.Maximum = 0;
            subUpDown.TabStop = false;
            subUpDown.ValueChanged += SubUpDown_ValueChanged;
            subUpDown.BackColor = this.BackColor;
            subUpDown.ForeColor = this.ForeColor;

            // In your SetupUI method, add this after creating SubUpDown
            subtitleTrackingLabel = new Label();
            subtitleTrackingLabel.Text = "Subtitles: 0/0";
            subtitleTrackingLabel.Location = new Point(563, 13);
            subtitleTrackingLabel.AutoSize = true;

            volumeLabel = new Label();
            volumeLabel.Text = "Volume: 100%";
            volumeLabel.Location = new Point(10, 50);
            volumeLabel.AutoSize = true;

            volumeSlider = new TrackBar();
            volumeSlider.Location = new Point(90, 45);
            volumeSlider.Width = 160;
            volumeSlider.Minimum = 0;
            volumeSlider.Maximum = 200;
            volumeSlider.Value = 100;
            volumeSlider.TickFrequency = 100;
            //volumeSlider.TickStyle = TickStyle.None;
            //volumeSlider.BackColor = Color.White;
            volumeSlider.AutoSize = false;
            volumeSlider.Height = 25;
            volumeSlider.TabStop = false;
            volumeSlider.SmallChange = 10;
            volumeSlider.LargeChange = 10;
            volumeSlider.Scroll += VolumeSlider_Scroll;



            //// Add this before the final Controls.AddRange
            //showSubtitlesButton = new Button();
            //showSubtitlesButton.Text = "Show Subtitles";
            //showSubtitlesButton.Location = new Point(694, 10);
            //showSubtitlesButton.Width = 90;
            //showSubtitlesButton.TabStop = false;
            //showSubtitlesButton.Click += ShowSubtitlesButton_Click;
            //showSubtitlesButton.ApplyCustomStyle();

            // Add this to your SetupUI method, after creating other controls
            repeatCountUpDown = new NumericUpDown();
            repeatCountUpDown.Size = new Size(60, 20);
            repeatCountUpDown.Location = new Point(370, 45);
            repeatCountUpDown.Minimum = 0;
            repeatCountUpDown.Maximum = 999;
            repeatCountUpDown.Value = 0;
            repeatCountUpDown.TabStop = false;
            repeatCountUpDown.ValueChanged += RepeatCountUpDown_ValueChanged;
            repeatCountUpDown.BackColor = this.BackColor;
            repeatCountUpDown.ForeColor = this.ForeColor;


            Label repeatLabel = new Label();
            repeatLabel.Text = "Repeat Count (0 = ∞):";
            repeatLabel.Location = new Point(260, 47);
            repeatLabel.AutoSize = true;

            // 2. Add this to your SetupUI method (in the controls creation section)
            Label speedLabel = new Label();
            speedLabel.Text = "Speed:";
            speedLabel.Location = new Point(440, 47);
            speedLabel.AutoSize = true;
            


            speedNumericUpDown = new NumericUpDown();
            speedNumericUpDown.Location = new Point(484, 45);
            speedNumericUpDown.Size = new Size(60, 20);
            speedNumericUpDown.Minimum = (decimal)0.1;
            speedNumericUpDown.Maximum = (decimal)2.0;
            speedNumericUpDown.Value = (decimal)1.0;
            speedNumericUpDown.Increment = (decimal)0.05;
            speedNumericUpDown.DecimalPlaces = 2;
            speedNumericUpDown.TabStop = false;
            speedNumericUpDown.ValueChanged += SpeedNumericUpDown_ValueChanged;
            speedNumericUpDown.BackColor = this.BackColor;
            speedNumericUpDown.ForeColor = this.ForeColor;

            // Add all controls
            controlsPanel.Controls.AddRange(new Control[] {
                loadButton, playPauseButton, stopButton, prevSubButton,
                nextSubButton, loopButton, volumeLabel, volumeSlider,
                timelineTrackBar, currentTimeLabel, totalTimeLabel,
                subUpDown, subtitleTrackingLabel,/* showSubtitlesButton,*/
                repeatLabel, repeatCountUpDown, speedLabel, speedNumericUpDown
            });


            // menu Bar
            menuBar = new Panel();
            menuBar.Dock = DockStyle.Top;
            menuBar.Height = 22;
            //menuBar.AutoScroll = true;

            Button aboutButton = new Button();
            aboutButton.Text = "About";
            //aboutButton.Location = new Point(10, 10);
            aboutButton.TabStop = false;
            aboutButton.Click += AboutButton_Click;
            aboutButton.ApplyCustomStyle();
            aboutButton.Dock = DockStyle.Left;
            aboutButton.FlatAppearance.BorderSize = 0;

            Button shortcutsButton = new Button();
            shortcutsButton.Text = "Shortcuts";
            //shortcutsButton.Location = new Point(10, 10);
            shortcutsButton.TabStop = false;
            shortcutsButton.Click += ShortcutsButton_Click;
            shortcutsButton.ApplyCustomStyle();
            shortcutsButton.Dock = DockStyle.Left;
            shortcutsButton.FlatAppearance.BorderSize = 0;


            Button subtitleListButton = new Button();
            subtitleListButton.Text = "Subtitle List";
            //subtitleListButton.Location = new Point(10, 10);
            subtitleListButton.TabStop = false;
            subtitleListButton.Click += ShowSubtitlesButton_Click;
            subtitleListButton.ApplyCustomStyle();
            subtitleListButton.Dock = DockStyle.Left;
            subtitleListButton.FlatAppearance.BorderSize = 0;
            subtitleListButton.AutoSize = true;
            //subtitleListButton.Width = 200;



            menuBar.Controls.AddRange(new Control[] {
                 aboutButton,shortcutsButton,subtitleListButton
            });

            // Add everything to form
            this.Controls.Add(videoView);
            this.Controls.Add(controlsPanel);
            this.Controls.Add(menuBar);
        }


        // Modify the InitializeVLC method to handle video end state
        private void InitializeVLC()
        {
            Core.Initialize();
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);
            videoView.MediaPlayer = _mediaPlayer;
            _mediaPlayer.Volume = 100;

            _mediaPlayer.Playing += (s, e) => {
                if (playPauseButton.InvokeRequired)
                {
                    playPauseButton.Invoke(new Action(() => {
                        playPauseButton.Text = "Pause";
                        playPauseButton.Enabled = true;
                        stopButton.Enabled = true;
                        UpdateTotalDuration();
                        timeUpdateTimer.Start();
                    }));
                }
            };

            _mediaPlayer.EndReached += (s, e) =>
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        hasVideoEnded = true;
                        timeUpdateTimer.Stop();
                        playPauseButton.Text = "Play";
                        currentSubtitleIndex = 0;
                        currentRepeatCount = 1;           // And this
                        currentTimeLabel.Text = "00:00:00";
                        timelineTrackBar.Value = 0;
                        subtitleTrackingLabel.Text = $"Subtitles: 1/{subtitles.Count}";
                        speedNumericUpDown.Value = 1.0m; // Reset to 1.0x
                    }));
                }
            };

            _mediaPlayer.Stopped += (s, e) =>
            {
                if (playPauseButton.InvokeRequired)
                {
                    playPauseButton.Invoke(new Action(() =>
                    {
                        hasVideoEnded = true;
                        timeUpdateTimer.Stop();
                        playPauseButton.Text = "Play";
                        currentSubtitleIndex = 0;
                        currentRepeatCount = 1;           // And this
                        currentTimeLabel.Text = "00:00:00";
                        timelineTrackBar.Value = 0;
                        subtitleTrackingLabel.Text = $"Subtitles: 1/{subtitles.Count}";
                        speedNumericUpDown.Value = 1.0m; // Reset to 1.0x
                    }));
                }
            };
        }

        private void ShortcutsButton_Click(object sender, EventArgs e)
        {
            ShortcutsForm shortcutsForm = new ShortcutsForm();
            shortcutsForm.ShowDialog();
        }

        private void AboutButton_Click(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
            //MessageBox.Show("Ahmed Ismail\nelcoder01@gmail.com");
            //MyTemplateForm aboutForm = new MyTemplateForm();
            //aboutForm.Text = "About";
            //aboutForm.Controls.AddRange(new Control[] {});
            //aboutForm.ShowDialog(this);
            AboutForm aboutForm = new AboutForm();
            aboutForm.ShowDialog();
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            if (this.Size.Width < 810)
            {
                //MessageBox.Show("this.Size.Width < 810");
                controlsPanel.Height =125;
            }
            else
            {
                controlsPanel.Height = 105;
            }
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            // Check if the dragged data contains file(s)
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Get the file paths
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // Check if any of the files are video files
                bool isVideoFile = files.Any(file =>
                    new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm" }
                    .Contains(Path.GetExtension(file).ToLower()));

                // If it's a video file, allow the drop
                if (isVideoFile)
                {
                    e.Effect = DragDropEffects.Copy;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }


        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            // Get the file paths
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            // Find the first video file
            string videoPath = files.FirstOrDefault(file =>
                new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm" }
                .Contains(Path.GetExtension(file).ToLower()));

            if (videoPath != null)
            {
                // Reset the flag for keyboard handling if needed
                isHandlingO = false;

                string srtPath = Path.ChangeExtension(videoPath, ".srt");

                if (File.Exists(srtPath))
                {
                    LoadSubtitles(srtPath);
                    PlayVideo(videoPath);
                    ShowNotification(Path.GetFileName(videoPath));
                    this.Text = $"{APP_NAME}: {Path.GetFileName(videoPath)}";
                }
                else
                {
                    MessageBox.Show("No matching SRT file found!", "Missing Subtitles",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    // Option to play video without subtitles
                    if (MessageBox.Show("Would you like to play the video without subtitles?",
                        "Play Without Subtitles", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        PlayVideo(videoPath);
                        ShowNotification(Path.GetFileName(videoPath));
                        this.Text = $"{APP_NAME}: {Path.GetFileName(videoPath)}";
                    }
                }
            }
        }


        private void NotificationTimer_Tick(object sender, EventArgs e)
        {
            notificationLabel.Visible = false;
            notificationTimer.Stop();
        }


        // Method to show notification
        public void ShowNotification(string message)
        {
            //if (menuBar.Visible) {
            //    notificationLabel.Location = new Point(20, 60);
            //}
            //else
            //{
            //    notificationLabel.Location = new Point(20, 40);
            //}
            notificationTimer.Stop();
            notificationLabel.Text = message;
            notificationLabel.Visible = true;
            notificationLabel.BringToFront();
            notificationTimer.Start();
        }



        // 4. Add this method to handle speed changes
        private void SpeedNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (_mediaPlayer != null)
            {
                float selectedSpeed = (float)speedNumericUpDown.Value;
                _mediaPlayer.SetRate(selectedSpeed);
                ShowNotification($"Speed: {speedNumericUpDown.Value}x");
            }
        }

        // 6. Add these helper methods for speed control
        private void DecreaseSpeed()
        {
            if (speedNumericUpDown.Value > speedNumericUpDown.Minimum)
            {
                speedNumericUpDown.Value -= speedNumericUpDown.Increment;
            }
        }

        private void IncreaseSpeed()
        {
            if (speedNumericUpDown.Value < speedNumericUpDown.Maximum)
            {
                speedNumericUpDown.Value += speedNumericUpDown.Increment;
            }
        }






        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    if (!isHandlingEnter)
                    {
                        isHandlingEnter = true;
                        EnterFullScreen();
                        e.Handled = true;
                    }
                    break;

                case Keys.Escape:
                    if (!isHandlingEscape)
                    {
                        isHandlingEscape = true;
                        EscapeFullScreen();
                        e.Handled = true;
                    }
                    break;

                case Keys.L:
                    if (!isHandlingL)
                    {
                        isHandlingL = true;
                        LoopButton_Click();
                        e.Handled = true;
                    }
                    break;

                case Keys.Up:
                   // if (!isHandlingUp)
                   // {
                        //isHandlingUp = true;
                        IncreaseVolume_KeyAction();  // You'll need to implement this method
                        e.Handled = true;
                   // }
                    break;

                case Keys.Down:
                    //if (!isHandlingDown)
                    //{
                    //    isHandlingDown = true;
                        DecreaseVolume_KeyAction();  // You'll need to implement this method
                        e.Handled = true;
                    //}
                    break;

                case Keys.Left:
                    if (!isHandlingLeft)
                    {
                        isHandlingLeft = true;
                        PrevSubButton_Click();  // You'll need to implement this method
                        e.Handled = true;
                    }
                    break;

                case Keys.Right:
                    if (!isHandlingRight)
                    {
                        isHandlingRight = true;
                        NextSubButton_Click();  // You'll need to implement this method
                        e.Handled = true;
                    }
                    break;

                case Keys.Space:
                    if (!isHandlingSpace)
                    {
                        isHandlingSpace = true;
                        PlayPauseButton_Click();  // You'll need to implement this method
                        e.Handled = true;
                    }
                    break;

                case Keys.OemMinus:
                case Keys.Subtract:
                    if (!isHandlingOemMinus)
                    {
                        isHandlingOemMinus = true;
                        DecreaseSpeed();
                        e.Handled = true;
                    }
                    break;

                case Keys.Oemplus:
                case Keys.Add:
                    if (!isHandlingOemplus)
                    {
                        isHandlingOemplus = true;
                        IncreaseSpeed();
                        e.Handled = true;
                    }
                    break;

                case Keys.O:
                    if (!isHandlingO)
                    {
                        isHandlingO = true;
                        LoadButton_Click();
                        e.Handled = true;
                    }
                    break;

                case Keys.OemSemicolon:
                    if (!isHandlingOemSemicolon)
                    {
                        isHandlingOemSemicolon = true;
                        DecreaseRepeatCount_KeyAction();
                        e.Handled = true;
                    }
                    break;

                case Keys.OemQuotes:
                    if (!isHandlingOemQuotes)
                    {
                        isHandlingOemQuotes = true;
                        IncreaseRepeatCount_KeyAction();
                        e.Handled = true;
                    }
                    break;

                case Keys.H:
                    if (!isHandlingH)
                    {
                        isHandlingH = true;
                        HideOrShowControlPanelAndMenuBar();
                        e.Handled = true;
                    }
                    break;

                case Keys.S:
                    if (!isHandlingS)
                    {
                        isHandlingS = true;
                        StopButton_Click();
                        e.Handled = true;
                    }
                    break;

                case Keys.V:
                    if (playPauseButton.Enabled) 
                    {
                        if (isSubtitlesEnabled)
                        {
                            ToggleSubtitles(false);
                            isSubtitlesEnabled = false;
                            ShowNotification("Subtitles disabled");
                        }
                        else
                        {
                            ToggleSubtitles(true);
                            isSubtitlesEnabled=true;
                            ShowNotification("Subtitles enabled");
                        }
                    }
                    
                    
                    break;
            }
        }

        private void HideOrShowControlPanelAndMenuBar()
        {
            if (menuBar.Visible && controlsPanel.Visible)
            { 
                menuBar.Visible = false;
                controlsPanel.Visible = false;
            }
            else
            {
                menuBar.Visible = true;
                controlsPanel.Visible = true;
            }
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    isHandlingEnter = false;
                    break;
                case Keys.Escape:
                    isHandlingEscape = false;
                    break;
                case Keys.L:
                    isHandlingL = false;
                    break;
                case Keys.Up:
                    isHandlingUp = false;
                    break;
                case Keys.Down:
                    isHandlingDown = false;
                    break;
                case Keys.Left:
                    isHandlingLeft = false;
                    break;
                case Keys.Right:
                    isHandlingRight = false;
                    break;
                case Keys.Space:
                    isHandlingSpace = false;
                    break;
                case Keys.OemMinus:
                case Keys.Subtract:
                    isHandlingOemMinus = false;
                    break;
                case Keys.Oemplus:
                case Keys.Add:
                    isHandlingOemplus = false;
                    break;
                case Keys.O:
                    isHandlingO = false;
                    break;

                case Keys.OemSemicolon:
                    isHandlingOemSemicolon = false;
                    break;

                case Keys.OemQuotes:
                    isHandlingOemQuotes = false;
                    break;

                case Keys.H:
                    isHandlingH = false;
                    break;

                case Keys.S:
                    isHandlingS = false;
                    break;
            }
            e.Handled = true;
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            // Check for Enter key
            if (keyData == Keys.Enter)
            {
                if (!isHandlingEnter)
                {
                    isHandlingEnter = true;
                    EnterFullScreen();
                    return true;
                }
            }
            else if (isHandlingEnter)
            {
                isHandlingEnter = false;
            }

            // Check for Escape key
            if (keyData == Keys.Escape)
            {
                if (!isHandlingEscape)
                {
                    isHandlingEscape = true;
                    EscapeFullScreen();
                    return true;
                }
            }
            else if (isHandlingEscape)
            {
                isHandlingEscape = false;
            }

            // Check for L key
            if (keyData == Keys.L)
            {
                if (!isHandlingL)
                {
                    isHandlingL = true;
                    LoopButton_Click();
                    return true;
                }
            }
            else if (isHandlingL)
            {
                isHandlingL = false;
            }

            // Check for Up arrow
            if (keyData == Keys.Up)
            {
                //if (!isHandlingUp)
                //{
                //    isHandlingUp = true;
                    IncreaseVolume_KeyAction();
                    return true;
                //}
            }
            else if (isHandlingUp)
            {
                //isHandlingUp = false;
            }

            // Check for Down arrow
            if (keyData == Keys.Down)
            {
                //if (!isHandlingDown)
                //{
                //    isHandlingDown = true;
                    DecreaseVolume_KeyAction();
                    return true;
                //}
            }
            else if (isHandlingDown)
            {
                //isHandlingDown = false;
            }

            // Check for Left arrow
            if (keyData == Keys.Left)
            {
                if (!isHandlingLeft)
                {
                    isHandlingLeft = true;
                    PrevSubButton_Click();
                    return true;
                }
            }
            else if (isHandlingLeft)
            {
                isHandlingLeft = false;
            }

            // Check for Right arrow
            if (keyData == Keys.Right)
            {
                if (!isHandlingRight)
                {
                    isHandlingRight = true;
                    NextSubButton_Click();
                    return true;
                }
            }
            else if (isHandlingRight)
            {
                isHandlingRight = false;
            }

            // Check for Space key
            if (keyData == Keys.Space)
            {
                if (!isHandlingSpace)
                {
                    isHandlingSpace = true;
                    PlayPauseButton_Click();
                    return true;
                }
            }
            else if (isHandlingSpace)
            {
                isHandlingSpace = false;
            }

            // Check for O key
            if (keyData == Keys.O)
            {
                if (!isHandlingO)
                {
                    isHandlingO = true;
                    LoadButton_Click();
                    return true;
                }
            }
            else if (isHandlingO)
            {
                isHandlingO = false;
            }


            // Check for OemSemicolon key
            if (keyData == Keys.OemSemicolon)
            {
                if (!isHandlingOemSemicolon)
                {
                    isHandlingOemSemicolon = true;
                    DecreaseRepeatCount_KeyAction();
                    return true;
                }

            }
            else if (isHandlingOemSemicolon)
            {
                isHandlingOemSemicolon = false;
            }


            // Check for OemQuotes key
            if (keyData == Keys.OemQuotes)
            {
                if (!isHandlingOemQuotes)
                {
                    isHandlingOemQuotes = true;
                    IncreaseRepeatCount_KeyAction();
                    return true;
                }

            }
            else if (isHandlingOemQuotes)
            {
                isHandlingOemQuotes = false;
            }


            // Check for H key
            if (keyData == Keys.H)
            {
                if (!isHandlingH)
                {
                    isHandlingH = true;
                    HideOrShowControlPanelAndMenuBar();
                    return true;
                }

            }
            else if (isHandlingH)
            {
                isHandlingH = false;
            }



            // Check for S key
            if (keyData == Keys.S)
            {
                if (!isHandlingS)
                {
                    isHandlingS = true;
                    StopButton_Click();
                    return true;
                }

            }
            else if (isHandlingS)
            {
                isHandlingS = false;
            }



            // Let the base class handle other keys
            return base.ProcessDialogKey(keyData);
        }


        // Modify the RepeatCountUpDown_ValueChanged method
        private void RepeatCountUpDown_ValueChanged(object sender, EventArgs e)
        {
            targetRepeatCount = (int)repeatCountUpDown.Value;
            currentRepeatCount = 1;
            //lastProcessedSubtitleIndex = -1;
            ShowNotification($"Repeat Count: {repeatCountUpDown.Value}");


        }

        private void IncreaseRepeatCount_KeyAction()
        {
            if (repeatCountUpDown.Value < 999)
            {
                repeatCountUpDown.Value++;
            }
        }

        private void DecreaseRepeatCount_KeyAction()
        {
            if (repeatCountUpDown.Value > 0)
            {
                repeatCountUpDown.Value--;
            }
        }



        private void IncreaseVolume_KeyAction()
        {
            if (_mediaPlayer != null && volumeSlider != null)
            {
                // Increase volume by VOLUME_STEP, but don't exceed maximum (200)
                int newVolume = Math.Min(volumeSlider.Value + VOLUME_STEP, 200);

                // Update the slider value
                volumeSlider.Value = newVolume;

                // Update the media player volume
                _mediaPlayer.Volume = newVolume;

                // Update the volume label
                volumeLabel.Text = $"Volume: {newVolume}%";
                ShowNotification($"Volume: {newVolume}%");
            }
        }

        private void DecreaseVolume_KeyAction()
        {
            if (_mediaPlayer != null && volumeSlider != null)
            {
                // Decrease volume by VOLUME_STEP, but don't go below 0
                int newVolume = Math.Max(volumeSlider.Value - VOLUME_STEP, 0);

                // Update the slider value
                volumeSlider.Value = newVolume;

                // Update the media player volume
                _mediaPlayer.Volume = newVolume;

                // Update the volume label
                volumeLabel.Text = $"Volume: {newVolume}%";
                ShowNotification($"Volume: {newVolume}%");
            }
        }

        private void ToggleSubtitles(bool enable)
        {
            if (_mediaPlayer != null)
            {
                if (!enable)
                {
                    //MessageBox.Show(_mediaPlayer.Spu.ToString());
                    currentSubtitleTrack_mediaPlayer_Spu = _mediaPlayer.Spu;
                    _mediaPlayer.SetSpu(-1);  // Disable subtitles
                    
                }
                else
                {
                    // To enable subtitles, you need to set it to the desired track number
                    // Usually track 0 is the first subtitle track
                    //_mediaPlayer.SetSpu(0);
                    //MessageBox.Show(_mediaPlayer.SetSpu(3).ToString());
                    _mediaPlayer.SetSpu(currentSubtitleTrack_mediaPlayer_Spu);
                }
            }
        }


        private void EscapeFullScreen()
        {
            //============
            if (isVideoViewMaximized)
            {
                isVideoViewMaximized = false;

                
                this.WindowState = lastWindowState;

                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.Bounds = lastScreenBounds;
                this.TopMost = false;

                controlsPanel.Visible = true;
                menuBar.Visible = true;

                ShowNotification("Fullscreen: Off");
            }
            //============
        }

        private void EnterFullScreen()
        {
            
            //============
            if (!isVideoViewMaximized)
            {
                isVideoViewMaximized = true;

                lastWindowState = this.WindowState;
                if (this.WindowState == FormWindowState.Normal)
                {
                    lastScreenBounds = this.Bounds;
                }

                controlsPanel.Visible = false;
                menuBar.Visible = false;

                // Make the form always stay on top.
                this.TopMost = true;

                //==================================================
                //this is a repetitive code ,i know, to fix visual problem (flickering) 
                this.FormBorderStyle = FormBorderStyle.None;
                this.Bounds = Screen.PrimaryScreen.Bounds;
                //==================================================

                this.WindowState = FormWindowState.Normal;
                // Set the form border style to none to hide window borders and title bar.
                this.FormBorderStyle = FormBorderStyle.None;
                // Set the form's size to cover the whole screen.
                this.Bounds = Screen.PrimaryScreen.Bounds;




                //this.WindowState = FormWindowState.Maximized;
                //this.FormBorderStyle = FormBorderStyle.None;

                ShowNotification("Fullscreen: On");
            }
            else
            {

                EscapeFullScreen();

            }
            //============
        }







        // Add this new method to your MainForm class:
        private void ShowSubtitlesButton_Click(object sender, EventArgs e)
        {
            if (subtitles == null || subtitles.Count == 0)
            {
                if (playPauseButton.Enabled)
                {
                    MessageBox.Show("No subtitles loaded.", "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                
                return;
            }

            // Close existing form if it exists
            if (subtitleListForm != null && !subtitleListForm.IsDisposed)
            {
                subtitleListForm.Close();
            }

            // Create and show the new form
            subtitleListForm = new SubtitleListForm(subtitles, (selectedIndex) =>
            {
                // Update the current subtitle index
                currentSubtitleIndex = selectedIndex;
                subUpDown.Value = selectedIndex + 1;
                PlaySubtitle(selectedIndex);
            });
            subtitleListForm.Show(this);
        }

        // Modify the PlayPauseButton_Click method to handle replay
        private void PlayPauseButton_Click(object sender = null, EventArgs e = null)
        {
            //this line to prevent calling the button function when it's disabled.
            if (!playPauseButton.Enabled) { return; }

            if (_mediaPlayer != null)
            {
                if (_mediaPlayer.IsPlaying)
                {
                    _mediaPlayer.Pause();
                    playPauseButton.Text = "Play";
                    ShowNotification("Pause");
                }
                else
                {
                    
                    if (hasVideoEnded)
                    {
                        hasVideoEnded= false;
                        _mediaPlayer.Stop();
                    }
                    _mediaPlayer.Play();
                    playPauseButton.Text = "Pause";
                    ShowNotification("Play");
                }

            }
        }

        // Add this to your StopButton_Click method
        private void StopButton_Click(object sender = null, EventArgs e = null)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Stop();
                timeUpdateTimer.Stop();
                playPauseButton.Text = "Play";
                currentSubtitleIndex = 0;
                currentRepeatCount = 1;           // And this

                currentTimeLabel.Text = "00:00:00";
                timelineTrackBar.Value = 0;
                subtitleTrackingLabel.Text = $"Subtitles: 1/{subtitles.Count}";
                speedNumericUpDown.Value = 1.0m; // Reset to 1.0x

                //=====================================
                if (isLooping)
                {
                    LoopButton_Click(sender, e);
                }

                subUpDown.Value = subUpDown.Minimum;
                //=====================================
                ShowNotification("Stop");
            }
        }

        private void VolumeSlider_Scroll(object sender, EventArgs e)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = volumeSlider.Value;
                volumeLabel.Text = $"Volume: {volumeSlider.Value}%";
            }
        }



        private void LoadButton_Click(object sender = null, EventArgs e = null)
        {

            //===========================================================
            //This is irrelevant code here I know but it's necessary for
            //open file from the keyboard function 
            isHandlingO = false;
            //===========================================================

            using (OpenFileDialog videoDialog = new OpenFileDialog())
            {
                videoDialog.Filter = "Video files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.flv;*.webm|All files|*.*";
                if (videoDialog.ShowDialog() == DialogResult.OK)
                {
                    string videoPath = videoDialog.FileName;
                    string srtPath = Path.ChangeExtension(videoPath, ".srt");

                    if (File.Exists(srtPath))
                    {
                        LoadSubtitles(srtPath);
                        PlayVideo(videoPath);
                        ShowNotification(videoDialog.SafeFileName);
                        this.Text = $"{APP_NAME}: {videoDialog.SafeFileName}";
                    }
                    else
                    {
                        MessageBox.Show("No matching SRT file found!", "Missing Subtitles",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);

                        // Option to play video without subtitles
                        if (MessageBox.Show("Would you like to play the video without subtitles?",
                            "Play Without Subtitles", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            PlayVideo(videoPath);
                            ShowNotification(videoDialog.SafeFileName);
                            this.Text = $"{APP_NAME}: {videoDialog.SafeFileName}";
                        }
                    }
                }
            }
        }



        // Please open video file function it's for opening file with open with feature from program.cs 
        public void OpenVideoFile(string filePath)
        {
            string videoPath = filePath;
            string srtPath = Path.ChangeExtension(videoPath, ".srt");
            string fileName = Path.GetFileName(videoPath);

            if (File.Exists(srtPath))
            {
                LoadSubtitles(srtPath);
                PlayVideo(videoPath);
                ShowNotification(fileName);
                this.Text = $"{APP_NAME}: {fileName}";
            }
            else
            {
                MessageBox.Show("No matching SRT file found!", "Missing Subtitles",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);

                if (MessageBox.Show("Would you like to play the video without subtitles?",
                    "Play Without Subtitles", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    PlayVideo(videoPath);
                    ShowNotification(fileName);
                    this.Text = $"{APP_NAME}: {fileName}";
                }
            }
        }





        // Modify LoadSubtitles method to update the label when subtitles are loaded
        private void LoadSubtitles(string srtPath)
        {
            try
            {
                subtitles.Clear();
                subtitles = SubtitleParser.ParseSRT(srtPath);
                currentSubtitleIndex = 0;

                if (subtitles.Count == 0)
                {
                    MessageBox.Show("No valid subtitles were found in the file.", "Warning",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    subUpDown.Minimum = 0;
                    subUpDown.Maximum = 0;
                    subUpDown.Value = 0;
                    subUpDown.Enabled = false;
                    subtitleTrackingLabel.Text = "Subtitles: 0/0";
                }
                else
                {
                    subUpDown.Minimum = 1;
                    subUpDown.Maximum = subtitles.Count;
                    subUpDown.Value = 1;
                    subUpDown.Enabled = true;
                    subtitleTrackingLabel.Text = $"Subtitles: 1/{subtitles.Count}";

                    //MessageBox.Show($"Successfully loaded {subtitles.Count} subtitles.", "Success",
                    //    MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading subtitle file: {ex.Message}", "File Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                subtitles.Clear();
                subUpDown.Minimum = 0;
                subUpDown.Maximum = 0;
                subUpDown.Value = 0;
                subUpDown.Enabled = false;
                subtitleTrackingLabel.Text = "Subtitles: 0/0";
            }

            //// Add this at the end of the method:
            //showSubtitlesButton.Enabled = (subtitles != null && subtitles.Count > 0);
        }




        private void PlayVideo(string videoPath)
        {
            try
            {
                using (var media = new Media(_libVLC, videoPath))
                {
                    _mediaPlayer.Play(media);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error playing video: {ex.Message}", "Playback Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Update the PrevSubButton_Click method to keep NumericUpDown in sync
        private void PrevSubButton_Click(object sender = null, EventArgs e = null)
        {
            UpdateCurrentSubtitleIndex();
            if (currentSubtitleIndex > 0)
            {
                currentSubtitleIndex--;
                subUpDown.Value = currentSubtitleIndex + 1; // This will trigger SubUpDown_ValueChanged
                ShowNotification("Previous subtitle");
            }
        }

        // Update the NextSubButton_Click method to keep NumericUpDown in sync
        private void NextSubButton_Click(object sender = null, EventArgs e = null)
        {
            UpdateCurrentSubtitleIndex();
            if (currentSubtitleIndex < subtitles.Count - 1)
            {
                currentSubtitleIndex++;
                subUpDown.Value = currentSubtitleIndex + 1; // This will trigger SubUpDown_ValueChanged
                ShowNotification("Next subtitle");
            }
        }

        // Update the SubUpDown_ValueChanged method to handle navigation
        private void SubUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (subtitles != null && subtitles.Count > 0 )
            {
                // Subtract 1 because subtitle indices in the list are 0-based,
                // but we're displaying them as 1-based in the NumericUpDown
                int newIndex = (int)subUpDown.Value - 1;

                // Validate the index
                if (newIndex >= 0 && newIndex < subtitles.Count)
                {
                    currentSubtitleIndex = newIndex;
                    PlaySubtitle(currentSubtitleIndex);
                }
            }
        }

        private void LoopButton_Click(object sender = null, EventArgs e = null)
        {
            isLooping = !isLooping;
            if (isLooping)
            {
                loopTimer.Start();
                loopButton.Text = "Loop: ON";
                ShowNotification("Loop: On");
                UpdateCurrentSubtitleIndex();
            }
            else
            {
                loopTimer.Stop();
                loopButton.Text = "Loop: OFF";
                ShowNotification("Loop: Off");
            }
        }

        private void UpdateCurrentSubtitleIndex()
        {
            // Check if values are NOT within 2 positions of each other
            if (Math.Abs(currentSubtitleIndex - realTimeSubtitleIndex) >= 2)
            {
                // Execute code only when values are far apart
                //MessageBox.Show("if (Math.Abs(currentSubtitleIndex - realTimeSubtitleIndex) >= 2)");
                //currentSubtitleIndex = realTimeSubtitleIndex;
                subUpDown.Value = realTimeSubtitleIndex+1; 

            }
        }

        private void PlaySubtitle(int index)
        {
            if (subtitles != null && subtitles.Count > 0 && index >= 0 && index < subtitles.Count)
            {
                var subtitle = subtitles[index];
                _mediaPlayer.Time = (long)subtitle.StartTime.TotalMilliseconds;
            }
        }



        private void LoopTimer_Tick(object sender, EventArgs e)
        {
            if (!isLooping || subtitles == null || subtitles.Count == 0 || _mediaPlayer == null)
                return;

            var currentTime = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
            var currentIndex = currentSubtitleIndex;

            // First, verify if we're still within the current subtitle's time range
            if (currentIndex >= 0 && currentIndex < subtitles.Count)
            {
                var currentSub = subtitles[currentIndex];

                //var nextSub = subtitles[currentIndex+1];

                // If we've passed the end time of current subtitle
                if (currentTime > currentSub.EndTime)
                {
                    // Check if we should repeat
                    if (targetRepeatCount == 0 || currentRepeatCount < targetRepeatCount)
                    {
                        // Reset to start of current subtitle
                        _mediaPlayer.Time = (long)currentSub.StartTime.TotalMilliseconds;
                        if (targetRepeatCount > 0)
                        {
                            currentRepeatCount++;
                        }
                    }
                    else
                    {
                        // We've finished our repeats for this subtitle
                        currentRepeatCount = 1;
                        // Move to next subtitle if available
                        if (currentIndex < subtitles.Count - 1)
                        {
                            currentSubtitleIndex++;

                           // SubUpDown.Value = currentSubtitleIndex + 1;
                           // PlaySubtitle(currentSubtitleIndex);
                        }
                        else
                        {
                            // We've reached the end of subtitles
                            isLooping = false;
                            loopTimer.Stop();
                            loopButton.Text = "Loop: OFF";
                        }
                    }
                }
            }



            //// Update the real-time tracking
            //for (int i = 0; i < subtitles.Count; i++)
            //{
            //    if (currentTime >= subtitles[i].StartTime && currentTime <= subtitles[i].EndTime)
            //    {
            //        realTimeSubtitleIndex = i;
            //        subtitleTrackingLabel.Text = $"Subtitles: {i + 1}/{subtitles.Count}";
            //        break;
            //    }
            //}
        }



        // New methods for timeline control
        private void TimeUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (_mediaPlayer != null /*&& _mediaPlayer.IsPlaying*/ && !isTimelineBeingDragged)
            {
                UpdateTimeDisplay();

                // Add this line to update subtitle display
                UpdateSubtitleDisplay(_mediaPlayer.Time);
            }
        }

        private void UpdateSubtitleDisplay(long currentTime)
        {
            // Check if we have subtitles loaded
            if (subtitles != null && subtitles.Count > 0)
            {
                // Convert the video's current time from milliseconds to a TimeSpan
                var currentTimeSpan = TimeSpan.FromMilliseconds(currentTime);


                if (currentTimeSpan <= subtitles[0].StartTime)
                {
                    //MessageBox.Show("currentTimeSpan <= subtitles[0].StartTime");
                    subtitleTrackingLabel.Text = $"Subtitles: {0 + 1}/{subtitles.Count}";
                    realTimeSubtitleIndex = 0;

                    return;
                }

                if (currentTimeSpan >= subtitles[subtitles.Count-1].StartTime)
                {
                    //MessageBox.Show("currentTimeSpan >= subtitles[subtitles.Count-1].EndTime");
                    subtitleTrackingLabel.Text = $"Subtitles: {subtitles.Count - 1 + 1}/{subtitles.Count}";
                    realTimeSubtitleIndex = subtitles.Count - 1;
                    return;
                }


                // Loop through all subtitles
                for (int i = 0; i < subtitles.Count+1; i++)
                {

                    // Check if current video time falls within this subtitle's time range
                    if (currentTimeSpan >= subtitles[i].StartTime &&
                        currentTimeSpan <= subtitles[i+1].StartTime)
                    {

                        subtitleTrackingLabel.Text = $"Subtitles: {i + 1}/{subtitles.Count}";
                        realTimeSubtitleIndex = i;

                        //// Only update if we're on a different subtitle
                        //if (currentSubtitleIndex != i)
                        //{

                        //    //currentSubtitleIndex = i;

                        //    //// Update the NumericUpDown to show current subtitle number
                        //    //// Add 1 because SubUpDown uses 1-based numbering
                        //    //if (SubUpDown.Value != i + 1)
                        //    //{
                        //    //    SubUpDown.Value = i + 1;
                        //    //}


                        //}
                        break;  // Exit loop once we find the current subtitle
                    }
                }
            }
        }

        private void TimelineTrackBar_MouseDown(object sender, MouseEventArgs e)
        {
            isTimelineBeingDragged = true;

            //=====================================
            if (isLooping)
            {
                LoopButton_Click(sender, e);
                isAutoLoop = true;
            }
            //=====================================

        }

        private void TimelineTrackBar_MouseUp(object sender, MouseEventArgs e)
        {
            if (_mediaPlayer != null && _mediaPlayer.Length > 0)
            {
                long newPosition = (_mediaPlayer.Length * timelineTrackBar.Value) / timelineTrackBar.Maximum;
                _mediaPlayer.Time = newPosition;
            }
            isTimelineBeingDragged = false;

            //=====================================
            if (!isLooping && isAutoLoop)
            {
                TimeUpdateTimer_Tick(sender, e);
                LoopButton_Click(sender, e);
                isAutoLoop = false;
            }
            //=====================================
        }

        private void UpdateTimeDisplay()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(UpdateTimeDisplay));
                return;
            }

            if (_mediaPlayer != null && _mediaPlayer.Length > 0)
            {
                // Update current time label
                TimeSpan currentTime = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
                currentTimeLabel.Text = currentTime.ToString(@"hh\:mm\:ss");

                // Update timeline position if not being dragged
                if (!isTimelineBeingDragged)
                {
                    int position = (int)((_mediaPlayer.Time * timelineTrackBar.Maximum) / _mediaPlayer.Length);
                    timelineTrackBar.Value = Math.Min(position, timelineTrackBar.Maximum);
                }
            }
        }

        private void UpdateTotalDuration()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(UpdateTotalDuration));
                return;
            }

            if (_mediaPlayer != null && _mediaPlayer.Length > 0)
            {
                TimeSpan totalTime = TimeSpan.FromMilliseconds(_mediaPlayer.Length);
                totalTimeLabel.Text = totalTime.ToString(@"hh\:mm\:ss");
            }
        }

        // Modify the ResetTimeDisplay method to maintain control state
        private void ResetTimeDisplay()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(ResetTimeDisplay));
                return;
            }

            currentTimeLabel.Text = "00:00:00";
            totalTimeLabel.Text = "00:00:00";
            timelineTrackBar.Value = 0;

            // Keep controls enabled if we have a video loaded
            if (_mediaPlayer != null && _mediaPlayer.Media != null)
            {
                playPauseButton.Enabled = true;
                stopButton.Enabled = true;
                timelineTrackBar.Enabled = true;
            }
            else
            {
                playPauseButton.Enabled = false;
                stopButton.Enabled = false;
                timelineTrackBar.Enabled = false;
            }
        }


    }




    public static class ControlExtensions
    {
        // Extension method for button styling
        public static void ApplyCustomStyle(this Button button)
        {
            
            button.BackColor = Color.FromArgb(32, 32, 32);
            button.ForeColor = Color.White;

            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = SystemColors.ControlDarkDark;
            button.FlatAppearance.MouseDownBackColor = Color.DarkSlateGray;
            button.FlatAppearance.MouseOverBackColor = Color.Teal;
            button.Cursor = Cursors.Hand;

            //button.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            //button.Padding = new Padding(5);
           

            //// Hover effects
            //button.MouseEnter += (s, e) => {
            //    button.BackColor = Color.FromArgb(62, 62, 66);
            //};

            //button.MouseLeave += (s, e) => {
            //    button.BackColor = Color.FromArgb(45, 45, 48);
            //};
        }
    }









    public class SubtitleParser
    {
        public class SubtitleEntry
        {
            public int Index { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
            public string Text { get; set; }
        }

        public static List<SubtitleEntry> ParseSRT(string filePath)
        {
            var subtitles = new List<SubtitleEntry>();
            var fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            // Normalize line endings
            fileContent = fileContent.Replace("\r\n", "\n").Replace('\r', '\n');
            var blocks = fileContent.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var block in blocks)
            {
                try
                {
                    var lines = block.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
                    if (lines.Count < 3) continue;

                    // Parse index
                    if (!int.TryParse(lines[0].Trim(), out int index))
                        continue;

                    // Parse timestamp line
                    var timestamps = lines[1].Split(new[] { " --> " }, StringSplitOptions.None);
                    if (timestamps.Length != 2)
                        continue;

                    if (!TryParseTimeStamp(timestamps[0].Trim(), out TimeSpan startTime) ||
                        !TryParseTimeStamp(timestamps[1].Trim(), out TimeSpan endTime))
                        continue;

                    // Parse text (can be multiple lines)
                    var text = string.Join("\n", lines.Skip(2));

                    subtitles.Add(new SubtitleEntry
                    {
                        Index = index,
                        StartTime = startTime,
                        EndTime = endTime,
                        Text = text.Trim()
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing subtitle block: {ex.Message}");
                    // Continue with next block instead of failing completely
                    continue;
                }
            }

            return subtitles;
        }

        private static bool TryParseTimeStamp(string timestamp, out TimeSpan result)
        {
            result = TimeSpan.Zero;
            try
            {
                // Remove BOM and clean the timestamp
                timestamp = timestamp.Trim().Replace("\uFEFF", "");

                // Match pattern: 00:00:00,000 or 00:00:00.000
                var match = Regex.Match(timestamp, @"(\d{2}):(\d{2}):(\d{2})[,\.](\d{3})");

                if (!match.Success)
                    return false;

                var hours = int.Parse(match.Groups[1].Value);
                var minutes = int.Parse(match.Groups[2].Value);
                var seconds = int.Parse(match.Groups[3].Value);
                var milliseconds = int.Parse(match.Groups[4].Value);

                result = new TimeSpan(0, hours, minutes, seconds, milliseconds);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }






    // Add this entire class right before your SubtitleParser class:
    public class SubtitleListForm : Form
    {

        // Import DwmSetWindowAttribute from dwmapi.dll
        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attribute,
                                                         ref int pvAttribute, uint cbAttribute);

        // Define the DWMWINDOWATTRIBUTE enum
        private enum DWMWINDOWATTRIBUTE : uint
        {
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
            // Other attributes can be added here if needed
        }

        private void EnableDarkMode()
        {
            // Set the immersive dark mode attribute for the form handle
            int preference = 1; // 1 enables dark mode, 0 disables it
            DwmSetWindowAttribute(this.Handle,
                                  DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                                  ref preference,
                                  sizeof(int));
        }

        private ListBox subtitleListBox;
        private Action<int> onSubtitleSelected;

        public SubtitleListForm(List<SubtitleParser.SubtitleEntry> subtitles, Action<int> onSubtitleSelected)
        {
            this.onSubtitleSelected = onSubtitleSelected;
            InitializeSubtitleList(subtitles);
            SetupForm();


            EnableDarkMode();
        }

        private void SetupForm()
        {
            this.Text = "Subtitle List";
            this.Size = new Size(400, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            //this.TopMost = true;
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }

        private void InitializeSubtitleList(List<SubtitleParser.SubtitleEntry> subtitles)
        {
            subtitleListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.None,
                SelectionMode = SelectionMode.One,
                BackColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.White

            };

            // Add items with number prefix
            for (int i = 0; i < subtitles.Count; i++)
            {
                subtitleListBox.Items.Add($"{i + 1}. {subtitles[i].Text}");
                //MessageBox.Show(subtitles[i].Text);
            }



            // Handle double-click to select subtitle
            subtitleListBox.DoubleClick += (s, e) =>
            {
                if (subtitleListBox.SelectedIndex != -1)
                {
                    onSubtitleSelected(subtitleListBox.SelectedIndex);
                }
            };


            // Add context menu for copying
            var contextMenu = new ContextMenuStrip();
            var copyItem = new ToolStripMenuItem("Copy Text");
            copyItem.Click += (s, e) =>
            {
                if (subtitleListBox.SelectedItem != null)
                {
                    string fullText = subtitleListBox.SelectedItem.ToString();
                    // Remove the number prefix before copying
                    string textOnly = fullText.Substring(fullText.IndexOf('.') + 2);
                    Clipboard.SetText(textOnly);
                }
            };
            contextMenu.Items.Add(copyItem);
            subtitleListBox.ContextMenuStrip = contextMenu;

            this.Controls.Add(subtitleListBox);
        }
    }






    // 
    public class MyTemplateForm : Form
    {

        // Import DwmSetWindowAttribute from dwmapi.dll
        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attribute,
                                                         ref int pvAttribute, uint cbAttribute);

        // Define the DWMWINDOWATTRIBUTE enum
        private enum DWMWINDOWATTRIBUTE : uint
        {
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
            // Other attributes can be added here if needed
        }

        private void EnableDarkMode()
        {
            // Set the immersive dark mode attribute for the form handle
            int preference = 1; // 1 enables dark mode, 0 disables it
            DwmSetWindowAttribute(this.Handle,
                                  DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                                  ref preference,
                                  sizeof(int));
        }


        public MyTemplateForm()
        {
            SetupForm();
            EnableDarkMode();
        }

        private void SetupForm()
        {
            this.Size = new Size(400, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }

    }






    public class AboutForm : MyTemplateForm
    {
        public AboutForm()
        {
            SetupForm();
        }

   

        private void SetupForm()
        {

            this.Size = new Size(500, 400);
            this.Text = "About";
            //======================================================================


            Panel aboutPanel = new Panel();
            aboutPanel.BorderStyle = BorderStyle.FixedSingle;
            aboutPanel.BackColor = Color.FromArgb(64, 64, 64);
            aboutPanel.Size = new Size(400, 150);
            aboutPanel.Location = new Point(Convert.ToInt32(this.ClientSize.Width / 2 - aboutPanel.Width / 2),
                       Convert.ToInt32(this.ClientSize.Height / 2 - aboutPanel.Height / 2));



            PictureBox logoPictureBox = new PictureBox();
            logoPictureBox.Image = Properties.Resources.suvi_high_resolution_logo2_modified;
            logoPictureBox.Location = new Point(10,16);
            logoPictureBox.Size = new Size(120,120);
            logoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            

            TextBox aboutTextBox = new TextBox();
            aboutTextBox.ReadOnly = true;
            aboutTextBox.Multiline = true;
            aboutTextBox.BorderStyle = BorderStyle.None;
            aboutTextBox.BackColor = aboutPanel.BackColor;
            aboutTextBox.ForeColor = Color.White;
            aboutTextBox.Font = new Font("Segoe UI Semibold", 12, FontStyle.Bold);
            aboutTextBox.Location = new Point(132,30);
            aboutTextBox.Size = new Size(260,110);
            aboutTextBox.TextAlign = HorizontalAlignment.Center;
            aboutTextBox.Text = "Product Name: SuVi Player \r\nCreated by: Ahmed Ismail\r\n" +
                "Email: elcoder01@gmail.com\r\nVersion: 1.0.0.0";
            aboutTextBox.TabStop = false;


            aboutPanel.Controls.Add(logoPictureBox);
            aboutPanel.Controls.Add(aboutTextBox);
            this.Controls.Add(aboutPanel);


            this.Resize += (sender, e) =>
            {
                aboutPanel.Location = new Point(Convert.ToInt32(this.ClientSize.Width / 2 - aboutPanel.Width / 2),
                    Convert.ToInt32(this.ClientSize.Height / 2 - aboutPanel.Height / 2));
            };

        }


    }






    //public class ShortcutsForm : MyTemplateForm
    //{
    //    public ShortcutsForm()
    //    {
    //        SetupForm();
    //    }
    //    private void SetupForm()
    //    {
    //        this.Size = new Size(600, 600);
    //        this.Text = "Shortcuts";
    //        //======================================================================
    //    }
    //}





    public class ShortcutsForm : MyTemplateForm
    {
        public ShortcutsForm()
        {
            SetupForm();
        }

        private void SetupForm()
        {
            this.Size = new Size(600, 600);
            this.Text = "Shortcuts";

            TableLayoutPanel table = new TableLayoutPanel
            {
                //Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(10),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
                ,AutoSize = true
                ,Width= 560
                ,Location= new Point(10,10)
            };

            // Set column styles
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));

            // Add header
            AddTableHeader(table);

            Dictionary<string, string> shortcuts = new Dictionary<string, string>
        {
            {"Enter", "Enter fullscreen mode"},
            {"Escape", "Exit fullscreen mode"},
            {"Space", "Play/Pause"},
            {"O", "Open file"},
            {"↑ (Up)", "Increase volume"},
            {"↓ (Down)", "Decrease volume"},
            {"← (Left)", "Previous subtitle"},
            {"→ (Right)", "Next subtitle"},
            {"L", "Toggle loop"},
            {"; (Semicolon)", "Decrease repeat count"},
            {"' (Quote)", "Increase repeat count"},
            {"- (Minus)", "Decrease playback speed"},
            {"+ (Plus)", "Increase playback speed"},
            {"H", "Toggle control panel and menu bar"},
            {"S", "Stop video"},
            {"V","Hide or show subtitles" }
        };

            // Set the total number of rows
            table.RowCount = shortcuts.Count + 1; // +1 for header

            // Set all rows to have absolute height
            int rowHeight = 30; // Set a fixed height for each row
            for (int i = 0; i < table.RowCount; i++)
            {
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));
            }

            int row = 1;
            foreach (var shortcut in shortcuts)
            {
                // Key label
                Label keyLabel = CreateStyledLabel(shortcut.Key, true);
                table.Controls.Add(keyLabel, 0, row);

                // Action label
                Label actionLabel = CreateStyledLabel(shortcut.Value, false);
                table.Controls.Add(actionLabel, 1, row);

                row++;
            }

            this.Controls.Add(table);
            this.StartPosition = FormStartPosition.CenterParent;
        }

        private Label CreateStyledLabel(string text, bool isBold)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(this.Font.FontFamily, 10, isBold ? FontStyle.Bold : FontStyle.Regular),
                Padding = new Padding(10, 5, 10, 5),
                AutoEllipsis = true  // This will show ... if text is too long
            };
        }

        private void AddTableHeader(TableLayoutPanel table)
        {
            Label shortcutHeader = CreateStyledLabel("Shortcut", true);
            Label actionHeader = CreateStyledLabel("Action", true);

            // Make headers slightly larger
            shortcutHeader.Font = new Font(this.Font.FontFamily, 11, FontStyle.Bold);
            actionHeader.Font = new Font(this.Font.FontFamily, 11, FontStyle.Bold);

            table.Controls.Add(shortcutHeader, 0, 0);
            table.Controls.Add(actionHeader, 1, 0);
        }
    }





}