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
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Xabe.FFmpeg.Downloader;
using Xabe.FFmpeg;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace SuViPlayer
{
    public partial class MainForm : Form
    {
        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attribute,
                                                         ref int pvAttribute, uint cbAttribute);
        private enum DWMWINDOWATTRIBUTE : uint
        {
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20
        }
        private void EnableDarkMode()
        {
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
        private bool isNegativeTotalTime = false;
        private bool isTimelineBeingDragged = false;


        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private VideoView videoView;
        private Panel controlsPanel;
        //private Panel menuBar;
        private int currentSubtitleIndex = 0;
        private bool isLooping = false;
        private Timer loopTimer;
        private TrackBar volumeSlider;
        private Label volumeLabel;
        private Button playPauseButton;
        private Button stopButton;
        private Button loopButton;
        private Button loadButton;

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
        private SubtitleListForm subtitleListForm;

        private bool isVideoViewMaximized = false;
        private FormWindowState lastWindowState;
        // Variable to store the last known screen bounds before going fullscreen.
        private Rectangle lastScreenBounds;

        // Add these fields to the MainForm class
        private NumericUpDown repeatCountUpDown;
        private int currentRepeatCount = 1;
        private int targetRepeatCount = 0;


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
        private bool isHandlingT = false;
        private bool isHandlingR = false;

        private const int VOLUME_STEP = 10; // Adjust this value to change how much the volume changes with each key press

        // 1. Add this field to your MainForm class (alongside other private fields)
        private NumericUpDown speedNumericUpDown;

        private Label notificationLabel;
        private Timer notificationTimer;

        private const string APP_NAME = "SuVi Player";

        private bool isSubtitlesEnabled = true;
        private int currentSubtitleTrack_mediaPlayer_Spu;

        private CheckBox isPauseFeature;

        private UpdateChecker _updateChecker;


        private const string TempFileFingerprint = "SuViPlayerTemp_";

        private bool isMainFormLoaded = false;
        private bool isInitializeVLCLoaded = false;

        private Panel subtitlePanel;
        private RichTextBox subtitleRichTextBox; // Change from Label to RichTextBox

        //private string dbFilePath;
        private static string dbFilePath;

        private bool isSubtitlePanelEnabled = true;

        private ContextMenuStrip contextMenuStrip;
        private ToolStripMenuItem copyMenuItem;
        private ToolStripMenuItem saveMenuItem;
        private ToolStripMenuItem customizeMenuItem;

        private string currentVideoPath;

        private MenuStrip menuStrip;

        private ToolStripMenuItem audioTrackMenuItem;

        // In your MainForm class:
        private TimeSpan? pointA; // Nullable TimeSpan to store the start point (A)
        private TimeSpan? pointB; // Nullable TimeSpan to store the end point (B)

        // Declare the A-B repeat MenuItems as fields
        private ToolStripMenuItem setAMenuItem;
        private ToolStripMenuItem setBMenuItem;
        private ToolStripMenuItem resetABMenuItem;

        private Timer cursorHideTimer;
        private bool isCursorOverVideo = false;
        private Cursor blankCursor;
        //private Timer forceCursorHideTimer;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem playPauseMenuItem;
        private ToolStripMenuItem fullscreenMenuItem;

        private int subtitlePanelLineHight = 32;

        private ToolStripMenuItem loopMenuItem;
        private ToolStripMenuItem repeatAfterMeMenuItem;
        public MainForm()
        {

            InitializeComponent();

            EnsureDatabaseExists();

            SetupUI();


            // Initialize the subtitles list
            subtitles = new List<SubtitleParser.SubtitleEntry>();

            loopTimer = new Timer();
            loopTimer.Interval = 50; // Check every 100ms
            loopTimer.Tick += LoopTimer_Tick;

            // Initialize timeline update timer
            timeUpdateTimer = new Timer();
            timeUpdateTimer.Interval = 50; // Update every 500ms
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

            // Enable dark mode for the title bar
            EnableDarkMode();


            // Initialize update checker with your GitHub repo details
            _updateChecker = new UpdateChecker("ahmedismailc", "SuViPlayer");

            // Check For Updates At Startup
            _updateChecker.CheckForUpdatesAtStartup();
            //SetupFFmpeg();
            
            
            // Initialize the cursor hide timer
            cursorHideTimer = new Timer();
            cursorHideTimer.Interval = 1000; // 1 seconds
            cursorHideTimer.Tick += CursorHideTimer_Tick;



            isMainFormLoaded = true;
        }



        private void SetupUI()
        {
            // Main form settings
            this.Size = new Size(810, 600);
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
            this.Shown += MainForm_Shown;


            // Enable drag and drop for the entire form
            this.AllowDrop = true;

            // Add event handlers for drag and drop
            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDrop;
            this.FormClosing += MainForm_FormClosing;
            this.FormClosed += MainForm_FormClosed;

            //=================================================================================================
            // Video view
            videoView = new VideoView();
            videoView.Dock = DockStyle.Fill;
            videoView.Size = new Size(800, 400);
            videoView.DoubleClick += VideoView_DoubleClick;
            //videoView.Click += VideoView_Click;
            videoView.MouseEnter += VideoView_MouseEnter;
            videoView.MouseLeave += VideoView_MouseLeave;
            videoView.MouseMove += VideoView_MouseMove;

            ContextMenuStrip videoViewContextMenuStrip = new ContextMenuStrip();
            videoViewContextMenuStrip.Opening += VideoViewContextMenuStrip_Opening;

            audioTrackMenuItem = new ToolStripMenuItem("Audio Track");

            ToolStripMenuItem aToBLoopMenuItem = new ToolStripMenuItem("A to B Loop");
            setAMenuItem = new ToolStripMenuItem("Set Point A");
            setAMenuItem.Click += SetAMenuItem_Click;
            setBMenuItem = new ToolStripMenuItem("Set Point B");
            setBMenuItem.Click += SetBMenuItem_Click;
            resetABMenuItem = new ToolStripMenuItem("Reset A/B Loop");
            resetABMenuItem.Click += ResetABMenuItem_Click;

            //audioTrackMenuItem.DropDownItems.AddRange(new ToolStripItem[] {

            //});
            aToBLoopMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
                setAMenuItem,setBMenuItem,resetABMenuItem
            });

            exitToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += ExitToolStripMenuItem_Click;

            playPauseMenuItem = new ToolStripMenuItem("Play/Pause");
            playPauseMenuItem.Click += PlayPauseMenuItem_Click;
            ToolStripMenuItem stopMenuItem = new ToolStripMenuItem("Stop");
            stopMenuItem.Click += StopMenuItem_Click;
            ToolStripMenuItem previousMenuItem = new ToolStripMenuItem("Previous");
            previousMenuItem.Click += PreviousMenuItem_Click;
            ToolStripMenuItem nextMenuItem = new ToolStripMenuItem("Next");
            nextMenuItem.Click += NextMenuItem_Click;

            fullscreenMenuItem = new ToolStripMenuItem("Fullscreen");
            fullscreenMenuItem.Click += FullscreenMenuItem_Click;

            ToolStripMenuItem openMediaToolStripMenuItem = new ToolStripMenuItem();
            openMediaToolStripMenuItem.Text = "Open Media...";
            openMediaToolStripMenuItem.Click += OpenToolStripMenuItem_Click;

            loopMenuItem = new ToolStripMenuItem("Loop");
            loopMenuItem.Click += LoopMenuItem_Click;
            loopMenuItem.Checked = false;

            repeatAfterMeMenuItem = new ToolStripMenuItem("Repeat After Me");
            repeatAfterMeMenuItem.Click += RepeatAfterMeMenuItem_Click;
            repeatAfterMeMenuItem.Checked = false;

            ToolStripMenuItem subtitleContextToolStripMenuItem = new ToolStripMenuItem("Subtitle");

            ToolStripMenuItem subtitleListContextToolStripMenuItem = new ToolStripMenuItem();
            subtitleListContextToolStripMenuItem.Text = "Subtitle List";
            subtitleListContextToolStripMenuItem.Click += SubtitleListToolStripMenuItem_Click;

            ToolStripMenuItem addSubtitleFileContextToolStripMenuItem = new ToolStripMenuItem();
            addSubtitleFileContextToolStripMenuItem.Text = "Add Subtitle File";
            addSubtitleFileContextToolStripMenuItem.Click += AddSubtitleFileToolStripMenuItem_Click;

            ToolStripMenuItem addSecondSubtitleContextToolStripMenuItem = new ToolStripMenuItem();
            addSecondSubtitleContextToolStripMenuItem.Text = "Add Second Subtitle";
            addSecondSubtitleContextToolStripMenuItem.Click += AddSecondSubtitleToolStripMenuItem_Click;

            subtitleContextToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
            subtitleListContextToolStripMenuItem,addSubtitleFileContextToolStripMenuItem,
            addSecondSubtitleContextToolStripMenuItem,
            });

            videoViewContextMenuStrip.Items.AddRange(new ToolStripItem[] {
                playPauseMenuItem,stopMenuItem,previousMenuItem,nextMenuItem,new ToolStripSeparator(),
                openMediaToolStripMenuItem,new ToolStripSeparator(),
                subtitleContextToolStripMenuItem,new ToolStripSeparator(),
                fullscreenMenuItem,new ToolStripSeparator(),
                loopMenuItem,repeatAfterMeMenuItem,new ToolStripSeparator(),
                aToBLoopMenuItem,new ToolStripSeparator(),audioTrackMenuItem,
                new ToolStripSeparator(),exitToolStripMenuItem,
            });
            videoView.ContextMenuStrip = videoViewContextMenuStrip;


            //#if DEBUG
            //ToolStripMenuItem debugToolStripMenuItem = new ToolStripMenuItem();
            //debugToolStripMenuItem.Text = "Debug button";
            //debugToolStripMenuItem.Click += DebugToolStripMenuItem_Click;
            //videoViewContextMenuStrip.Items.Add(debugToolStripMenuItem);
            //#endif
            //=================================================================================================


            // Controls panel
            controlsPanel = new Panel();
            controlsPanel.Dock = DockStyle.Bottom;
            controlsPanel.Height = 105; // Increased height to accommodate timeline controls
            controlsPanel.AutoScroll = true;


            // Timeline controls
            timelineTrackBar = new TrackBar();
            timelineTrackBar.Location = new Point(58, 75);
            timelineTrackBar.Width = 677;
            timelineTrackBar.Maximum = 1000;
            timelineTrackBar.TickStyle = TickStyle.None;
            timelineTrackBar.AutoSize = false;
            timelineTrackBar.Height = 25;
            timelineTrackBar.TabStop = false;
            timelineTrackBar.MouseDown += TimelineTrackBar_MouseDown;
            timelineTrackBar.MouseUp += TimelineTrackBar_MouseUp;

            currentTimeLabel = new Label();
            currentTimeLabel.Location = new Point(10, 79);
            currentTimeLabel.Text = "00:00:00";
            currentTimeLabel.AutoSize = true;

            totalTimeLabel = new Label();
            totalTimeLabel.Location = new Point(735, 79);
            totalTimeLabel.Text = "00:00:00";
            totalTimeLabel.AutoSize = true;
            totalTimeLabel.Click += TotalTimeLabel_Click;

            // Existing controls...
            loadButton = new Button();
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
            volumeSlider.AutoSize = false;
            volumeSlider.Height = 25;
            volumeSlider.TabStop = false;
            volumeSlider.SmallChange = 10;
            volumeSlider.LargeChange = 10;
            volumeSlider.Scroll += VolumeSlider_Scroll;



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
            speedNumericUpDown.Minimum = (decimal)0.25;
            speedNumericUpDown.Maximum = (decimal)2.0;
            speedNumericUpDown.Value = (decimal)1.0;
            speedNumericUpDown.Increment = (decimal)0.05;
            speedNumericUpDown.DecimalPlaces = 2;
            speedNumericUpDown.TabStop = false;
            speedNumericUpDown.ValueChanged += SpeedNumericUpDown_ValueChanged;
            speedNumericUpDown.BackColor = this.BackColor;
            speedNumericUpDown.ForeColor = this.ForeColor;

            isPauseFeature = new CheckBox();
            isPauseFeature.Text = "Repeat After Me";
            isPauseFeature.Checked = false;
            isPauseFeature.TabStop = false;
            isPauseFeature.Location = new Point(565, 45);
            isPauseFeature.CheckedChanged += IsPauseFeature_CheckedChanged;

            // Add all controls
            controlsPanel.Controls.AddRange(new Control[] {
                loadButton, playPauseButton, stopButton, prevSubButton,
                nextSubButton, loopButton, volumeLabel, volumeSlider,
                timelineTrackBar, currentTimeLabel, totalTimeLabel,
                subUpDown, subtitleTrackingLabel,
                repeatLabel, repeatCountUpDown, speedLabel, speedNumericUpDown, isPauseFeature
            });






            // Subtitle Panel
            subtitlePanel = new Panel();
            subtitlePanel.Dock = DockStyle.Bottom;  // Or another suitable position
            subtitlePanel.Height = subtitlePanelLineHight * 2;          // Adjust as needed
            subtitlePanel.AutoScroll = true;    // Allow scrolling if the subtitle is long
            subtitlePanel.Visible = false;       // Initially hidden, show when subtitles are loaded
            //subtitlePanel.AutoSize = true;
            subtitlePanel.TabStop = false;
            subtitlePanel.BackColor = /*Color.Black*/ Color.FromArgb(16, 16, 16);
            subtitlePanel.ForeColor = Color.White;


            // Subtitle RichTextBox
            subtitleRichTextBox = new RichTextBox();
            subtitleRichTextBox.Dock = DockStyle.Fill;
            subtitleRichTextBox.BackColor = /*Color.Black*/ Color.FromArgb(16, 16, 16); // Match the Parent's background
            subtitleRichTextBox.ForeColor = Color.White; // Match the Parent's text color
            subtitleRichTextBox.BorderStyle = BorderStyle.None;
            subtitleRichTextBox.Font = new Font("Segoe UI", 16F);
            subtitleRichTextBox.ReadOnly = true; // Prevent user editing
            subtitleRichTextBox.DetectUrls = false; // Important to make it interpret text as words, not URLs
            subtitleRichTextBox.WordWrap = true;
            subtitleRichTextBox.MouseClick += SubtitleRichTextBox_MouseClick; // Add the click event handler
            subtitleRichTextBox.MouseDown += SubtitleRichTextBox_MouseDown; // Add the click event handler
            subtitleRichTextBox.SelectionAlignment = HorizontalAlignment.Center;
            subtitleRichTextBox.TabStop = false;


            // Add a ContextMenuStrip for right-click actions
            contextMenuStrip = new ContextMenuStrip();

            copyMenuItem = new ToolStripMenuItem("Copy");
            copyMenuItem.Click += CopyMenuItem_Click;

            saveMenuItem = new ToolStripMenuItem("Save to My List");
            saveMenuItem.Click += SaveMenuItem_Click;

            customizeMenuItem = new ToolStripMenuItem("Customize");
            customizeMenuItem.Click += CustomizeMenuItem_Click;

            contextMenuStrip.Items.AddRange(new ToolStripMenuItem[] {
                saveMenuItem, copyMenuItem,customizeMenuItem
            });

            contextMenuStrip.Items.Insert(2, new ToolStripSeparator());

            //Dynamically add dictionary links
            AddDictionaryMenuItems(contextMenuStrip);


            //contextMenuStrip.Items.Add(copyMenuItem);
            subtitleRichTextBox.ContextMenuStrip = contextMenuStrip;


            // Add everything to form
            this.Controls.Add(videoView);

            //this.Controls.Add(menuBar);

            // Add the RichTextBox to the panel, and the panel to the form
            subtitlePanel.Controls.Add(subtitleRichTextBox);
            this.Controls.Add(subtitlePanel);

            this.Controls.Add(controlsPanel);

            //=====================================================================================
            menuStrip = new MenuStrip();
            menuStrip.RenderMode = ToolStripRenderMode.Professional;
            menuStrip.Renderer = new DarkModeRenderer(new DarkModeColors());

            ToolStripMenuItem subtitleToolStripMenuItem = new ToolStripMenuItem();
            subtitleToolStripMenuItem.Text = "Subtitle";

            ToolStripMenuItem subtitleListToolStripMenuItem = new ToolStripMenuItem();
            subtitleListToolStripMenuItem.Text = "Subtitle List";
            subtitleListToolStripMenuItem.Click += SubtitleListToolStripMenuItem_Click;

            ToolStripMenuItem addSubtitleFileToolStripMenuItem = new ToolStripMenuItem();
            addSubtitleFileToolStripMenuItem.Text = "Add Subtitle File";
            addSubtitleFileToolStripMenuItem.Click += AddSubtitleFileToolStripMenuItem_Click;

            ToolStripMenuItem addSecondSubtitleToolStripMenuItem = new ToolStripMenuItem();
            addSecondSubtitleToolStripMenuItem.Text = "Add Second Subtitle";
            addSecondSubtitleToolStripMenuItem.Click += AddSecondSubtitleToolStripMenuItem_Click;

            ToolStripMenuItem addNonSrtSubtitleFileToolStripMenuItem = new ToolStripMenuItem();
            addNonSrtSubtitleFileToolStripMenuItem.Text = "Add Non-SRT Subtitle File (experimental)";
            addNonSrtSubtitleFileToolStripMenuItem.Click += AddNonSrtSubtitleFileToolStripMenuItem_Click;

            subtitleToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
            subtitleListToolStripMenuItem,addSubtitleFileToolStripMenuItem,
            addSecondSubtitleToolStripMenuItem,addNonSrtSubtitleFileToolStripMenuItem,
            });

            ToolStripMenuItem myListToolStripMenuItem = new ToolStripMenuItem();
            myListToolStripMenuItem.Text = "My List";
            myListToolStripMenuItem.Click += MyListToolStripMenuItem_Click;

            ToolStripMenuItem helpToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem.Text = "Help";

            ToolStripMenuItem shortcutsToolStripMenuItem = new ToolStripMenuItem();
            shortcutsToolStripMenuItem.Text = "Shortcuts";
            shortcutsToolStripMenuItem.Click += ShortcutsToolStripMenuItem_Click;

            ToolStripMenuItem checkUpdateToolStripMenuItem = new ToolStripMenuItem();
            checkUpdateToolStripMenuItem.Text = "Check for Updates";
            checkUpdateToolStripMenuItem.Click += CheckUpdateToolStripMenuItem_Click;

            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
            shortcutsToolStripMenuItem,checkUpdateToolStripMenuItem,
            });

            ToolStripMenuItem aboutToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem.Text = "About";
            aboutToolStripMenuItem.Click += AboutToolStripMenuItem_Click;

            ToolStripMenuItem fileToolStripMenuItem = new ToolStripMenuItem("File");

            ToolStripMenuItem openToolStripMenuItem = new ToolStripMenuItem();
            openToolStripMenuItem.Text = "Open";
            openToolStripMenuItem.Click += OpenToolStripMenuItem_Click;

            exitToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += ExitToolStripMenuItem_Click;

            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
            openToolStripMenuItem,exitToolStripMenuItem,
            });

            menuStrip.Items.AddRange(new ToolStripItem[] {
                fileToolStripMenuItem,
            subtitleToolStripMenuItem,myListToolStripMenuItem,
            helpToolStripMenuItem,aboutToolStripMenuItem
            });

            this.Controls.Add(menuStrip);
            //=====================================================================================

        }







        //private void DebugToolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    var subtitles = _mediaPlayer.SpuDescription;
        //    if (subtitles != null)
        //    {
        //        //foreach (var subtitle in subtitles)
        //        //{
        //        //    Debug.WriteLine($"Subtitle ID: {subtitle.Id}, Name: {subtitle.Name}");
        //        //}
        //        for (int i = 0; i < subtitles.Length; i++)
        //        {
        //            var subtitle = subtitles[i];
        //            Debug.WriteLine($"Index: {i}, Subtitle ID: {subtitle.Id}, Name: {subtitle.Name}");
        //        }
        //    }
        //    else
        //    {
        //        Debug.WriteLine("No subtitles available.");
        //    }
        //}



        private void TotalTimeLabel_Click(object sender, EventArgs e)
        {
            if (_mediaPlayer != null && _mediaPlayer.Length > 0)
            {
                isNegativeTotalTime = !isNegativeTotalTime;
                UpdateTotalDuration();
            }

        }

        private void RepeatAfterMeMenuItem_Click(object sender, EventArgs e)
        {
            isPauseFeature.Checked = !isPauseFeature.Checked;
            repeatAfterMeMenuItem.Checked = isPauseFeature.Checked;
        }

        private void LoopMenuItem_Click(object sender, EventArgs e)
        {
            //loopMenuItem.Checked = !loopMenuItem.Checked;
            LoopButton_Click(sender, e);
            loopMenuItem.Checked = isLooping;
        }

        private void FullscreenMenuItem_Click(object sender, EventArgs e)
        {
            EnterFullScreen();
        }

        private void NextMenuItem_Click(object sender, EventArgs e)
        {
            NextSubButton_Click(sender, e);
        }

        private void PreviousMenuItem_Click(object sender, EventArgs e)
        {
            PrevSubButton_Click(sender, e);
        }

        private void StopMenuItem_Click(object sender, EventArgs e)
        {
            StopButton_Click(sender, e);
        }

        private void PlayPauseMenuItem_Click(object sender, EventArgs e)
        {
            PlayPauseButton_Click(sender, e);
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
            //Application.Exit();
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadButton_Click(sender, e);
        }

        private void VideoView_MouseEnter(object sender, EventArgs e)
        {
            //Debug.WriteLine("VideoView_MouseEnter");
            isCursorOverVideo = true;
            cursorHideTimer.Start();

        }

        private void VideoView_MouseLeave(object sender, EventArgs e)
        {
            //Debug.WriteLine("VideoView_MouseLeave");
            isCursorOverVideo = false;
            cursorHideTimer.Stop();
            Cursor.Show();
            Cursor.Current = Cursors.Default;
        }

        private Point lastMousePos; // Field to store the last mouse position

        private void VideoView_MouseMove(object sender, MouseEventArgs e)
        {


            //Debug.WriteLine("VideoView_MouseMove");

            // Get the current mouse position relative to the VideoView
            Point currentMousePos = e.Location;

            // Check if the mouse position has changed since the last recorded position
            if (currentMousePos != lastMousePos)
            {
                // Update the last mouse position
                lastMousePos = currentMousePos;

                //Debug.WriteLine("VideoView_MouseMove & currentMousePos != lastMousePos");
                Cursor.Show();
                Cursor.Current = Cursors.Default;
                cursorHideTimer.Stop(); // Reset the timer on mouse move
                cursorHideTimer.Start();
                //=======================
                //if (isVideoViewMaximized)
                //{
                //    //controlsPanel.Visible = true;
                //    //sub.Visible = true;
                //}
                //=======================
            }
            else
            {
                if (isCursorOverVideo && _mediaPlayer.IsPlaying && _mediaPlayer != null)
                {
                    //i++;
                    //Debug.WriteLine("Cursor.Current = blankCursor;" + i.ToString());
                    Cursor.Current = blankCursor;
                }

            }

        }
        //int i = 0;

        private void CursorHideTimer_Tick(object sender, EventArgs e)
        {


            if (isCursorOverVideo && _mediaPlayer.IsPlaying && _mediaPlayer != null /*&& !notificationLabel.Visible*/)
            {
                cursorHideTimer.Stop();

                Cursor.Current = blankCursor;
            }
        }





        private void SetAMenuItem_Click(object sender, EventArgs e)
        {
            if (_mediaPlayer != null && !string.IsNullOrEmpty(currentVideoPath))
            {
                pointA = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
                ShowNotification("Point A set to: " + pointA.Value.ToString(@"hh\:mm\:ss"));

                // Start looping if A&B set and we're not already looping
                if (pointA.HasValue && pointB.HasValue && !isLooping)
                {
                    LoopButton_Click(); // Call your existing loop toggle method
                }
            }
        }

        private void SetBMenuItem_Click(object sender, EventArgs e)
        {
            if (_mediaPlayer != null && !string.IsNullOrEmpty(currentVideoPath))
            {
                pointB = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
                ShowNotification("Point B set to: " + pointB.Value.ToString(@"hh\:mm\:ss"));

                // Start looping if A&B set and we're not already looping
                if (pointA.HasValue && pointB.HasValue && !isLooping)
                {
                    LoopButton_Click(); // Call your existing loop toggle method
                }
            }
        }

        private void ResetABMenuItem_Click(object sender, EventArgs e)
        {
            if (pointA != null && pointB != null)
            {
                pointA = null;
                pointB = null;
                ShowNotification("A and B points cleared.");
            }

        }

        private void VideoViewContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            playPauseMenuItem.Text = playPauseButton.Text;
            fullscreenMenuItem.Text = isVideoViewMaximized ? "Leave Fullscreen" : "Fullscreen";
            loopMenuItem.Checked = isLooping;
            repeatAfterMeMenuItem.Checked = isPauseFeature.Checked;
            if (!string.IsNullOrEmpty(currentVideoPath))
            {
                //e.Cancel = true; // Prevent the context menu from opening
                //return;
                PopulateAudioTracks();
            }

        }



        private void VideoView_DoubleClick(object sender, EventArgs e)
        {

            EnterFullScreen();
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutForm aboutForm = new AboutForm();
            aboutForm.ShowDialog();
        }

        private void CheckUpdateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _updateChecker.CheckForUpdates();
        }

        private void ShortcutsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShortcutsForm shortcutsForm = new ShortcutsForm();
            shortcutsForm.ShowDialog();
        }

        private void MyListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MyListForm myListForm = new MyListForm(dbFilePath);
            myListForm.ShowDialog();
        }

        private void SubtitleListToolStripMenuItem_Click(object sender, EventArgs e)
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

        private void AddSubtitleFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_mediaPlayer.Media == null) { return; }
            OpenFileDialog openFileDialog = new OpenFileDialog();
            //openFileDialog.Filter = "Subtitle Files|*.srt;*.vtt;*.ass;*.sub;*.ssa";
            //openFileDialog.Filter = "Subtitle Files|*.srt|All files|*.*";
            openFileDialog.Filter = "Subtitle Files|*.srt";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                AddSubtitleToCurrentVideo(openFileDialog.FileName);
            }
        }


        private void AddSecondSubtitleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_mediaPlayer.Media == null) { return; }
            OpenFileDialog openFileDialog = new OpenFileDialog();
            //openFileDialog.Filter = "Subtitle Files|*.srt;*.ass;*.sub;*.vtt";
            openFileDialog.Filter = "Subtitle Files|*.srt|Other Subtitle Files|*.srt;*.vtt;*.ass;*.sub;*.ssa|All files|*.*";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                AddSubtitleToCurrentVideo(openFileDialog.FileName,true);
            }
        }


        private async void AddNonSrtSubtitleFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_mediaPlayer.Media == null) { return; }
            OpenFileDialog openFileDialog = new OpenFileDialog();
            //openFileDialog.Filter = "Subtitle Files|*.srt;*.ass;*.sub;*.vtt";
            //openFileDialog.Filter = "Subtitle Files|*.srt|All files|*.*";
            openFileDialog.Filter = "Subtitle Files|*.srt;*.vtt;*.ass;*.sub;*.ssa|All files|*.*";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {


                //AddSubtitleToCurrentVideo(openFileDialog.FileName);
                await ConvertNonSrtToSrt(openFileDialog.FileName);
            }
        }



        private void MainForm_Shown(object sender, EventArgs e)
        {
            string thisTitle = this.Text;
            this.Text += ": Loading...";
            this.Enabled = false;
            //MessageBox.Show("isMainFormLoaded " + isMainFormLoaded.ToString());
            loadButton.Enabled = false;
            //MessageBox.Show("isInitializeVLCLoaded " + isInitializeVLCLoaded.ToString());
            //await Task.Delay(10000); //testing somthing.
            InitializeVLC();
            loadButton.Enabled = true;
            this.Enabled = true;
            this.Text = thisTitle;
            //MessageBox.Show("isInitializeVLCLoaded " + isMainFormLoaded.ToString());
            _ = SetupFFmpeg();



        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Stop the player to allow deleting temp subtitle files
            StopButton_Click();
            notificationLabel.Visible = false;
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            DeleteTemporaryFiles();
        }

        // Modify the InitializeVLC method to handle video end state
        private void InitializeVLC()
        {
            Core.Initialize();
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);
            videoView.MediaPlayer = _mediaPlayer;
            _mediaPlayer.Volume = 100;
            _mediaPlayer.EnableMouseInput = false;
            _mediaPlayer.EnableKeyInput = false;

            _mediaPlayer.Playing += (s, e) =>
            {
                if (playPauseButton.InvokeRequired)
                {
                    playPauseButton.Invoke(new Action(() =>
                    {
                        playPauseButton.Text = "Pause";
                        playPauseButton.Enabled = true;
                        stopButton.Enabled = true;
                        UpdateTotalDuration();
                        timeUpdateTimer.Start();
                        if (isSubtitlePanelEnabled && subtitles.Count > 0) { subtitlePanel.Visible = true; }
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
                        if (isNegativeTotalTime && !string.IsNullOrEmpty(currentMediaTotalTime)) { totalTimeLabel.Text = currentMediaTotalTime; }
                        //Debug.WriteLine(currentMediaTotalTime);
                        timelineTrackBar.Value = 0;
                        subtitleTrackingLabel.Text = $"Subtitles: 1/{subtitles.Count}";
                        speedNumericUpDown.Value = 1.0m; // Reset to 1.0x
                        subtitlePanel.Visible = false;
                        subtitleRichTextBox.Text = "";
                        isSubtitlePanelEnabled = true;
                        pointA = null; pointB = null;
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
                        if (isNegativeTotalTime && !string.IsNullOrEmpty(currentMediaTotalTime)) { totalTimeLabel.Text = currentMediaTotalTime; }
                        //Debug.WriteLine(currentMediaTotalTime);
                        timelineTrackBar.Value = 0;
                        subtitleTrackingLabel.Text = $"Subtitles: 1/{subtitles.Count}";
                        speedNumericUpDown.Value = 1.0m; // Reset to 1.0x
                        subtitlePanel.Visible = false;
                        subtitleRichTextBox.Text = "";
                        isSubtitlePanelEnabled = true;
                        pointA = null; pointB = null;
                    }));
                }
            };
            isInitializeVLCLoaded = true;
        }


        private void EnsureDatabaseExists()
        {
            // 1. Get the path to the user's Documents folder
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // 2. Create the "SuViPlayer_Data" folder if it doesn't exist
            string dataFolderPath = Path.Combine(documentsPath, "SuViPlayer_Data");
            Directory.CreateDirectory(dataFolderPath); // This creates the folder if it's not already there

            // 3. Define the database file path within the "SuViPlayer_Data" folder
            string dbFileName = "SuViPlayer.db"; // You can name your database file whatever you like
            dbFilePath = Path.Combine(dataFolderPath, dbFileName);

            bool isFirstTime = false;
            // 4. Check if the database file exists, and create it if it doesn't
            if (!File.Exists(dbFilePath))
            {
                SQLiteConnection.CreateFile(dbFilePath);

                isFirstTime = true;
            }

            using (var connection = new SQLiteConnection($"Data Source={dbFilePath};Version=3;"))
            {
                connection.Open();

                // 5. Create the table for storing words/phrases
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS MyList (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    WordOrPhrase TEXT NOT NULL,  -- UNIQUE constraint removed
                    DateAdded TEXT DEFAULT (date('now'))
                    );
                    CREATE TABLE IF NOT EXISTS DictionaryLinks (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Url TEXT NOT NULL,
                        ListOrder INTEGER NOT NULL
                    );";

                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }

                if (isFirstTime)
                {
                    // 6. Insert default dictionary links ONLY if the table is empty
                    string checkLinksQuery = "SELECT COUNT(*) FROM DictionaryLinks";
                    using (var checkCommand = new SQLiteCommand(checkLinksQuery, connection))
                    {
                        int linkCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                        if (linkCount == 0)
                        {
                            // The table is empty, so insert the default links
                            string insertLinksQuery = @"
                            INSERT INTO DictionaryLinks (Name, Url, ListOrder) VALUES
                            ('Google Translate', 'https://translate.google.com/?text={word}', 1),
                            ('Google Search', 'https://www.google.com/search?q={word}', 2),
                            ('Google Definition', 'https://www.google.com/search?q={word}+definition', 3),
                            ('Google Pronunciation', 'https://www.google.com/search?q=how+to+pronounce+{word}', 4),
                            ('Youglish', 'https://youglish.com/pronounce/{word}/english', 5),
                            ('Oxford', 'https://www.oxfordlearnersdictionaries.com/definition/english/{word}', 6),
                            ('Merriam-Webster', 'https://www.merriam-webster.com/dictionary/{word}', 7),
                            ('Dictionary.com', 'https://www.dictionary.com/browse/{word}', 8),
                            ('Cambridge Dictionary', 'https://dictionary.cambridge.org/dictionary/english/{word}', 9);";

                            using (var insertCommand = new SQLiteCommand(insertLinksQuery, connection))
                            {
                                insertCommand.ExecuteNonQuery();
                            }
                        }
                    }
                }

            }
        }

        // Example of adding a word (duplicates allowed)
        private void AddWordToMyList(string word)
        {
            word = NormalizeText(word);
            try
            {
                using (var connection = new SQLiteConnection($"Data Source={dbFilePath};Version=3;"))
                {
                    connection.Open();
                    string insertQuery = "INSERT INTO MyList (WordOrPhrase) VALUES (@word)";
                    using (var command = new SQLiteCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@word", word);
                        command.ExecuteNonQuery();
                    }
                }
                ShowNotification("Word added successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding word: {ex.Message}");
            }
        }



        private void CustomizeMenuItem_Click(object sender, EventArgs e)
        {
            using (var dictLinksForm = new DictionaryLinksForm(dbFilePath))
            {
                dictLinksForm.ShowDialog();
                AddDictionaryMenuItems(contextMenuStrip);
            }
        }



        private void AddDictionaryMenuItems(ContextMenuStrip contextMenu)
        {
            EnsureDatabaseExists();
            // Get dictionary links using the static method
            List<DictionaryLink> dictionaryLinks = DictionaryLinksForm.GetDictionaryLinksFromDatabase(dbFilePath);

            contextMenu.Items.Clear();

            contextMenu.Items.AddRange(new ToolStripMenuItem[] { saveMenuItem, copyMenuItem });
            contextMenu.Items.Add(new ToolStripSeparator());

            foreach (var link in dictionaryLinks)
            {
                ToolStripMenuItem dictionaryMenuItem = new ToolStripMenuItem(link.Name);
                dictionaryMenuItem.Click += (sender, e) => OpenWebLookup(subtitleRichTextBox.SelectedText, link.Url);
                contextMenu.Items.Add(dictionaryMenuItem);
            }

            if (dictionaryLinks.Count > 0) { contextMenu.Items.Add(new ToolStripSeparator()); }

            contextMenu.Items.Add(customizeMenuItem);
        }

        private void OpenWebLookup(string word, string url)
        {
            try
            {
                // Validate the URL format and presence of {word} placeholder
                if (!IsValidUrl(url))
                {
                    MessageBox.Show("The URL format is invalid or does not contain the required '{word}' placeholder.", "Invalid URL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string finalUrl = url.Replace("{word}", Uri.EscapeDataString(NormalizeText(word)));
                Process.Start(new ProcessStartInfo(finalUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening website: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Helper method to validate URL format and presence of {word}
        private bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _) && url.Contains("{word}");
        }



        private void SaveMenuItem_Click(object sender, EventArgs e)
        {
            if (subtitleRichTextBox.SelectedText.Length > 0)
            {
                AddWordToMyList(NormalizeText(subtitleRichTextBox.SelectedText));
            }
        }



        private void SubtitleRichTextBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (_mediaPlayer.IsPlaying)
            {
                PlayPauseButton_Click();
            }
            HideCaret(subtitleRichTextBox.Handle); // Hide the caret on click
        }


        // Event handler for the "Copy" menu item
        private void CopyMenuItem_Click(object sender, EventArgs e)
        {
            //EscapeFullScreen();
            if (subtitleRichTextBox.SelectedText.Length > 0)
            {
                Clipboard.SetText(subtitleRichTextBox.SelectedText);
            }
        }


        // Add this to hide the caret (typing cursor)
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool HideCaret(IntPtr hWnd);

        private void SubtitleRichTextBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (_mediaPlayer.IsPlaying)
            {
                PlayPauseButton_Click();
            }
            HideCaret(subtitleRichTextBox.Handle); // Hide the caret on click
        }

        // You might want to hide it on focus as well
        private void SubtitleRichTextBox_Enter(object sender, EventArgs e)
        {
            if (_mediaPlayer.IsPlaying)
            {
                PlayPauseButton_Click();
            }
            HideCaret(subtitleRichTextBox.Handle);
        }



        public static string NormalizeText(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty; // Handle empty or null input
            }

            // 1. Replace all newline variations with a single space
            string textWithSpaces = input.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

            // 2. Use a regular expression to replace multiple spaces with a single space
            string normalizedText = Regex.Replace(textWithSpaces, @"\s+", " ");

            return normalizedText.Trim(); // Trim leading/trailing spaces
        }




        private void DeleteTemporaryFiles()
        {
            string tempPath = Path.GetTempPath();
            string[] tempFiles = Directory.GetFiles(tempPath, $"{TempFileFingerprint}*.srt");

            foreach (string filePath in tempFiles)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                catch (Exception ex)
                {
                    // Optional: Log the error or show a message
                    Console.WriteLine($"Error deleting temporary file {filePath}: {ex.Message}");
                }
            }

        }



        private async Task SetupFFmpeg()
        {
            // 1. Primary Location: Application's installation directory
            string primaryFFmpegFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FFmpeg");

            // 2. Secondary Location: Shared App Data (no admin rights needed for reading, only for initial setup/download)
            string secondaryFFmpegFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SuViPlayer", "FFmpeg");

            // 3. Check Primary Location First
            if (CheckFFmpegExists(primaryFFmpegFolderPath))
            {
                FFmpeg.SetExecutablesPath(primaryFFmpegFolderPath);
                return; // FFmpeg found in primary location
            }

            // 4. Check Secondary Location
            if (CheckFFmpegExists(secondaryFFmpegFolderPath))
            {
                FFmpeg.SetExecutablesPath(secondaryFFmpegFolderPath);
                return; // FFmpeg found in secondary location
            }

            // 5. FFmpeg Not Found - Prompt User
            DialogResult result = MessageBox.Show("FFmpeg executables not found. Do you want to download them?", "FFmpeg Missing", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                // 6. Download to Secondary Location (with error handling)
                try
                {
                    this.Cursor = Cursors.WaitCursor;

                    // Ensure secondary directory exists (create if needed - requires admin rights if not already created)
                    if (!Directory.Exists(secondaryFFmpegFolderPath))
                    {
                        Directory.CreateDirectory(secondaryFFmpegFolderPath);
                    }

                    await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Full, secondaryFFmpegFolderPath);
                    FFmpeg.SetExecutablesPath(secondaryFFmpegFolderPath);

                    // Success Message:
                    MessageBox.Show("FFmpeg downloaded successfully!", "Download Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error downloading FFmpeg: {ex.Message}", "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    this.Cursor = Cursors.Default;
                }
            }
            else
            {
                // Handle the case where the user chooses not to download FFmpeg.
                // You might want to disable features that rely on FFmpeg or show another message.
                MessageBox.Show("FFmpeg is required for certain features. These features will be disabled.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Helper Function to Check for FFmpeg Executables
        private bool CheckFFmpegExists(string ffmpegFolderPath)
        {
            return Directory.Exists(ffmpegFolderPath) &&
                   File.Exists(Path.Combine(ffmpegFolderPath, "ffmpeg.exe")) &&
                   File.Exists(Path.Combine(ffmpegFolderPath, "ffprobe.exe"));
        }










        // Method to add subtitle to already playing video
        private void AddSubtitleToCurrentVideo(string subtitlePath, bool secondSubtitle = false)
        {
            if (!secondSubtitle)
            {
                try
                {
                    if (_mediaPlayer.Media == null)
                    {
                        MessageBox.Show("No video is currently playing.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    long time = _mediaPlayer.Time;
                    PlayVideo(currentVideoPath, subtitlePath);

                    LoadSubtitles(subtitlePath);
                    _mediaPlayer.Time = time;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding subtitle: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                //AddSubtitleToCurrentVideo(openFileDialog.FileName);
                try
                {
                    if (_mediaPlayer.Media == null)
                    {
                        MessageBox.Show("No video is currently playing.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    long time = _mediaPlayer.Time;
                    PlayVideo(currentVideoPath, subtitlePath);

                    //LoadSubtitles(openFileDialog.FileName);
                    _mediaPlayer.Time = time;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding subtitle: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

        }




        private void IsPauseFeature_CheckedChanged(object sender, EventArgs e)
        {

            if (!isLooping && isPauseFeature.Checked)
            {
                LoopButton_Click(sender, e);
            }

            if (isPauseFeature.Checked)
            {
                ShowNotification("Repeat After Me: On");
            }
            else
            {
                ShowNotification("Repeat After Me: Off");
            }
        }





        private void MainForm_SizeChanged(object sender = null, EventArgs e = null)
        {
            if (this.Size.Width < 810)
            {

                controlsPanel.Height = 125;
            }
            else
            {
                controlsPanel.Height = 105;
            }

            timelineTrackBar.Width = controlsPanel.Width - 117;
            totalTimeLabel.Location = new Point(controlsPanel.Width - 59, totalTimeLabel.Location.Y);
        }







        private void NotificationTimer_Tick(object sender, EventArgs e)
        {
            notificationLabel.Visible = false;
            notificationTimer.Stop();
        }


        // Method to show notification
        public void ShowNotification(string message)
        {
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
                            isSubtitlesEnabled = true;
                            ShowNotification("Subtitles enabled");
                        }
                    }
                    break;

                case Keys.R:
                    if (!isHandlingR)
                    {
                        isHandlingR = true;
                        if (isPauseFeature.Checked)
                        {
                            isPauseFeature.Checked = false;
                            ShowNotification("Repeat After Me: Off");
                        }
                        else
                        {
                            isPauseFeature.Checked = true;
                            ShowNotification("Repeat After Me: On");
                        }
                        e.Handled = true;
                    }
                    break;

                case Keys.T:
                    if (!isHandlingT)
                    {
                        isHandlingT = true;
                        subtitlePanel.Visible = !subtitlePanel.Visible;
                        isSubtitlePanelEnabled = subtitlePanel.Visible;
                        ShowNotification(isSubtitlePanelEnabled ? "Subtitle Panel: On" : "Subtitle Panel: Off");
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void HideOrShowControlPanelAndMenuBar()
        {
            if (menuStrip.Visible && controlsPanel.Visible)
            {
                menuStrip.Visible = false;
                controlsPanel.Visible = false;
            }
            else
            {
                menuStrip.Visible = true;
                controlsPanel.Visible = true;
            }
            MainForm_SizeChanged();
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

                case Keys.R:
                    isHandlingR = false;
                    break;

                case Keys.T:
                    isHandlingT = false;
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

            if (!isLooping)
            {
                LoopButton_Click(sender, e);
            }

            targetRepeatCount = (int)repeatCountUpDown.Value;
            currentRepeatCount = 1;
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
                //menuBar.Visible = true;
                menuStrip.Visible = true;
                ShowNotification("Fullscreen: Off");
                MainForm_SizeChanged();
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
                //menuBar.Visible = false;
                menuStrip.Visible = false;
                // Make the form always stay on top.
                //this.TopMost = true;

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

                ShowNotification("Fullscreen: On");
                MainForm_SizeChanged();
            }
            else
            {

                EscapeFullScreen();

            }
            //============
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
                    if (!isSubtitlePanelEnabled)
                    {
                        subtitlePanel.Visible = true;
                    }
                }
                else
                {

                    if (hasVideoEnded)
                    {
                        hasVideoEnded = false;
                        _mediaPlayer.Stop();
                    }

                    if (pauseTimer.Enabled)
                    {

                        pauseTimer.Stop();


                        playPauseButton.Text = "Play";
                        ShowNotification("Pause");

                        loopTimer.Start();
                    }
                    else
                    {
                        _mediaPlayer.Play();
                        playPauseButton.Text = "Pause";
                        ShowNotification("Play");
                        if (!isSubtitlePanelEnabled)
                        {
                            subtitlePanel.Visible = false;
                        }
                    }

                }

            }
        }

        // Add this to your StopButton_Click method
        private void StopButton_Click(object sender = null, EventArgs e = null)
        {
            if (!stopButton.Enabled) { return; }
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Stop();
                timeUpdateTimer.Stop();
                playPauseButton.Text = "Play";
                currentSubtitleIndex = 0;
                currentRepeatCount = 1;           // And this

                currentTimeLabel.Text = "00:00:00";
                if (isNegativeTotalTime && !string.IsNullOrEmpty(currentMediaTotalTime)) { totalTimeLabel.Text = currentMediaTotalTime; }
                //Debug.WriteLine(currentMediaTotalTime);
                timelineTrackBar.Value = 0;
                subtitleTrackingLabel.Text = $"Subtitles: 1/{subtitles.Count}";
                speedNumericUpDown.Value = 1.0m; // Reset to 1.0x

                subtitlePanel.Visible = false;
                subtitleRichTextBox.Text = "";
                isSubtitlePanelEnabled = true;
                //=====================================
                if (isLooping)
                {
                    LoopButton_Click();
                    isPauseFeature.Checked = false;
                }

                subUpDown.Value = subUpDown.Minimum;
                //=====================================
                pointA = null; pointB = null;
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




        private async void LoadButton_Click(object sender = null, EventArgs e = null)
        {
            //InitializeVLC();
            //===========================================================
            // This is irrelevant code here I know but it's necessary for
            // open file from the keyboard function 
            isHandlingO = false;
            if (!loadButton.Enabled) { return; }
            if (_mediaPlayer.IsPlaying) { PlayPauseButton_Click(); notificationLabel.Visible = false; }

            //===========================================================

            using (OpenFileDialog videoDialog = new OpenFileDialog())
            {
                videoDialog.Filter = "Video files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.flv;*.webm|All files|*.*";
                if (videoDialog.ShowDialog() == DialogResult.OK)
                {



                    string videoPath = videoDialog.FileName;
                    string srtPath = Path.ChangeExtension(videoPath, ".srt");

                    // Clear subtitles from any previously loaded video
                    subtitles.Clear();
                    subUpDown.Minimum = 0;
                    subUpDown.Maximum = 0;
                    subUpDown.Value = 0;
                    subtitleTrackingLabel.Text = "Subtitles: 0/0";
                    currentSubtitleIndex = 0;
                    pointA = null; pointB = null;


                    if (File.Exists(srtPath))
                    {
                        // Load external SRT file
                        LoadSubtitles(srtPath);
                        PlayVideo(videoPath);
                        ShowNotification(videoDialog.SafeFileName);
                        this.Text = $"{APP_NAME}: {videoDialog.SafeFileName}";

                        subtitlePanel.Visible = true;
                    }
                    else
                    {
                        this.Cursor = Cursors.WaitCursor;

                        // Check for embedded subtitles
                        await ExtractEmbeddedSubtitles(videoPath);

                        this.Cursor = Cursors.Default;

                        subtitlePanel.Visible = true;
                        // No matching SRT file found, and no embedded subtitles were extracted
                        if (subtitles.Count == 0)
                        {
                            //// Option to play video without subtitles
                            MessageBox.Show("No matching SRT file found! The video will play without subtitles.",
                                "Play Without Subtitles", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            PlayVideo(videoPath);
                            ShowNotification(videoDialog.SafeFileName);
                            this.Text = $"{APP_NAME}: {videoDialog.SafeFileName}";

                            subtitlePanel.Visible = false;
                            subtitleRichTextBox.Text = "";
                        }
                    }
                }
            }
        }





        private async Task ExtractEmbeddedSubtitles(string videoPath)
        {

            await SetupFFmpeg();
            string tempSrtPath = null;
            try
            {
                var mediaInfo = await FFmpeg.GetMediaInfo(videoPath);
                var subtitleStreams = mediaInfo.SubtitleStreams.ToList();

                if (subtitleStreams.Count == 0)
                {
                    MessageBox.Show("No embedded subtitles found in the video.", "No Subtitles",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ISubtitleStream selectedSubtitleStream;
                if (subtitleStreams.Count > 1)
                {
                    using (var subtitleSelectionForm = new SubtitleSelectionForm(subtitleStreams))
                    {
                        if (subtitleSelectionForm.ShowDialog() == DialogResult.OK)
                        {
                            selectedSubtitleStream = subtitleSelectionForm.SelectedSubtitleStream;
                        }
                        else
                        {

                            selectedSubtitleStream = subtitleStreams.First();
                        }
                    }
                }
                else
                {
                    selectedSubtitleStream = subtitleStreams.First();
                }

                // Create temporary file path
                tempSrtPath = Path.Combine(Path.GetTempPath(), $"{TempFileFingerprint}{Guid.NewGuid()}.srt");

                using (var progressForm = new Form())
                {
                    progressForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    progressForm.ControlBox = false;
                    progressForm.Text = "Processing...";
                    progressForm.StartPosition = FormStartPosition.CenterScreen;
                    progressForm.Size = new Size(300, 100);

                    ProgressBar progressBar = new ProgressBar();
                    progressBar.Style = ProgressBarStyle.Marquee;
                    progressBar.Dock = DockStyle.Fill;
                    progressForm.Controls.Add(progressBar);

                    // Show the progress form non-modally
                    progressForm.Show(this);

                    try
                    {
                        loadButton.Enabled = false;
                        var conversion = await FFmpeg.Conversions.New()
                            .AddStream(selectedSubtitleStream)
                            .SetOutput(tempSrtPath)
                            .Start();
                    }
                    finally
                    {
                        loadButton.Enabled = true;
                        progressForm.Close();
                    }
                }

                if (File.Exists(tempSrtPath))
                {
                    // Prompt user to save permanently
                    DialogResult saveResult = MessageBox.Show(
                        "Subtitles extracted. Do you want to save the subtitle file?",
                        "Save Subtitles",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );

                    switch (saveResult)
                    {
                        case DialogResult.Yes:
                            string savePath = Path.Combine(
                                Path.GetDirectoryName(videoPath),
                                $"{Path.GetFileNameWithoutExtension(videoPath)}.srt"
                            );

                            try
                            {
                                File.Copy(tempSrtPath, savePath, true);
                            }
                            catch (Exception e)
                            {
                                MessageBox.Show($"Could not save file to '{savePath}'.\n\nError: {e.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                                {
                                    saveFileDialog.Filter = "Subtitle files (*.srt)|*.srt|All files (*.*)|*.*";
                                    saveFileDialog.Title = "Save Subtitle File";
                                    saveFileDialog.FileName = Path.GetFileName(savePath);

                                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                                    {
                                        try
                                        {
                                            File.Copy(tempSrtPath, saveFileDialog.FileName, true);
                                        }
                                        catch (Exception ex2)
                                        {
                                            MessageBox.Show($"Could not save file to '{saveFileDialog.FileName}'.\n\nError: {ex2.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        }
                                    }
                                }
                            }

                            LoadSubtitles(tempSrtPath); // Load into your 'subtitles' list
                            PlayVideo(videoPath, tempSrtPath);

                            string fileName = Path.GetFileName(videoPath);
                            ShowNotification(fileName);
                            this.Text = $"{APP_NAME}: {fileName}";

                            break;

                        case DialogResult.No:
                            // User chose not to save, load and set temp subtitles
                            LoadSubtitles(tempSrtPath); // Load into your 'subtitles' list
                            PlayVideo(videoPath, tempSrtPath);

                            fileName = Path.GetFileName(videoPath);
                            ShowNotification(fileName);
                            this.Text = $"{APP_NAME}: {fileName}";
                            break;

                    }

                }
                else
                {
                    MessageBox.Show("Failed to extract subtitles.", "Extraction Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //PlayVideo(videoPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error extracting subtitles: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                //PlayVideo(videoPath);
            }
            finally
            {

            }
        }






        // Please open video file function it's for opening file with open with feature from program.cs 
        public async void OpenVideoFile(string filePath)
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

                this.Cursor = Cursors.WaitCursor;
                // Check for embedded subtitles
                await ExtractEmbeddedSubtitles(videoPath);
                this.Cursor = Cursors.Default;

                // No matching SRT file found, and no embedded subtitles were extracted
                if (subtitles.Count == 0)
                {

                    MessageBox.Show("No matching SRT file found! The video will play without subtitles.",
                        "Play Without Subtitles", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    PlayVideo(videoPath);
                    ShowNotification(fileName);
                    this.Text = $"{APP_NAME}: {fileName}";

                    subtitlePanel.Visible = false;
                    subtitleRichTextBox.Text = "";
                }

            }
        }




        private async void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            if (!loadButton.Enabled) { return; }

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

                // Clear subtitles from any previously loaded video
                subtitles.Clear();
                subUpDown.Minimum = 0;
                subUpDown.Maximum = 0;
                subUpDown.Value = 0;
                subtitleTrackingLabel.Text = "Subtitles: 0/0";
                currentSubtitleIndex = 0;
                pointA = null; pointB = null;

                if (File.Exists(srtPath))
                {
                    LoadSubtitles(srtPath);
                    PlayVideo(videoPath);
                    ShowNotification(Path.GetFileName(videoPath));
                    this.Text = $"{APP_NAME}: {Path.GetFileName(videoPath)}";
                }
                else
                {

                    this.Cursor = Cursors.WaitCursor;
                    // Check for embedded subtitles
                    await ExtractEmbeddedSubtitles(videoPath);
                    this.Cursor = Cursors.Default;

                    // No matching SRT file found, and no embedded subtitles were extracted
                    if (subtitles.Count == 0)
                    {
                        MessageBox.Show("No matching SRT file found! The video will play without subtitles.",
                            "Play Without Subtitles", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        PlayVideo(videoPath);
                        ShowNotification(Path.GetFileName(videoPath));
                        this.Text = $"{APP_NAME}: {Path.GetFileName(videoPath)}";

                        subtitlePanel.Visible = false;
                        subtitleRichTextBox.Text = "";
                    }
                }
            }

            // Check if an SRT file was dropped
            string addedSrtPath = files.FirstOrDefault(file =>
                Path.GetExtension(file).ToLower() == ".srt");
            if (addedSrtPath != null)
            {

                // SRT file was dropped
                if (_mediaPlayer.Media != null)
                {
                    // Video is already playing, add subtitle
                    AddSubtitleToCurrentVideo(addedSrtPath);
                    ShowNotification("Subtitle added");
                }
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

                //====================================================
                // Check if it's an SRT file
                bool isSrtFile = files.Any(file =>
                    Path.GetExtension(file).ToLower() == ".srt");
                //====================================================

                // If it's a video file or SRT, allow the drop
                if (isVideoFile || isSrtFile)
                {

                    if (_mediaPlayer.IsPlaying) { PlayPauseButton_Click(); notificationLabel.Visible = false; }
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

        }


        private async Task ConvertNonSrtToSrt(string filePath)
        {

            await SetupFFmpeg();
            string tempSrtPath = null;
            try
            {
                var mediaInfo = await FFmpeg.GetMediaInfo(filePath);
                var subtitleStreams = mediaInfo.SubtitleStreams.ToList();

                if (subtitleStreams.Count == 0)
                {
                    return;
                }

                ISubtitleStream selectedSubtitleStream;
                if (subtitleStreams.Count > 1)
                {
                    using (var subtitleSelectionForm = new SubtitleSelectionForm(subtitleStreams))
                    {
                        if (subtitleSelectionForm.ShowDialog() == DialogResult.OK)
                        {
                            selectedSubtitleStream = subtitleSelectionForm.SelectedSubtitleStream;
                        }
                        else
                        {

                            selectedSubtitleStream = subtitleStreams.First();
                        }
                    }
                }
                else
                {
                    selectedSubtitleStream = subtitleStreams.First();
                }

                // Create temporary file path
                tempSrtPath = Path.Combine(Path.GetTempPath(), $"{TempFileFingerprint}{Guid.NewGuid()}.srt");

                using (var progressForm = new Form())
                {
                    progressForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    progressForm.ControlBox = false;
                    progressForm.Text = "Processing...";
                    progressForm.StartPosition = FormStartPosition.CenterScreen;
                    progressForm.Size = new Size(300, 100);

                    ProgressBar progressBar = new ProgressBar();
                    progressBar.Style = ProgressBarStyle.Marquee;
                    progressBar.Dock = DockStyle.Fill;
                    progressForm.Controls.Add(progressBar);

                    // Show the progress form non-modally
                    progressForm.Show(this);

                    try
                    {
                        loadButton.Enabled = false;
                        var conversion = await FFmpeg.Conversions.New()
                            .AddStream(selectedSubtitleStream)
                            .SetOutput(tempSrtPath)
                            .Start();
                    }
                    finally
                    {
                        loadButton.Enabled = true;
                        progressForm.Close();
                    }
                }

                if (File.Exists(tempSrtPath))
                {
                    //LoadSubtitles(tempSrtPath);

                    //string srtFilePath = "path/to/your/subtitle.srt"; // Replace with the actual path

                    try
                    {
                        SubtitleCleaner.CleanSrtFileInPlace(tempSrtPath);
                        Console.WriteLine("SRT file cleaned and updated successfully!");
                    }
                    catch (FileNotFoundException ex)
                    {
                        Console.WriteLine($"Error: File not found - {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred: {ex.Message}");
                    }
                    AddSubtitleToCurrentVideo(tempSrtPath);
                }
                else
                {
                    MessageBox.Show("Failed to Open subtitles.", "Opening Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error Opening subtitles: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
            finally
            {

            }
        }


        private void PlayVideo(string videoPath, string srtpath = null)
        {
            //==============================
            currentVideoPath = videoPath;
            //==============================
            try
            {
                using (var media = new Media(_libVLC, videoPath))
                {

                    //4 = subtitle file matching the movie name exactly
                    media.AddOption(":sub-autodetect-fuzzy=4");
                    //========================================================================

                    if (_mediaPlayer.Play(media))
                    {


                        if (srtpath != null)
                        {
                            // Add options to disable automatic subtitle loading and force the specified subtitle file
                            media.AddOption(":no-sub-autodetect-file");


                            _mediaPlayer.Media.AddOption($":sub-file={srtpath}");
                            // Reload media with new subtitle
                            _mediaPlayer.Media = _mediaPlayer.Media;
                            _mediaPlayer.Play();

                        }


                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error playing video: {ex.Message}", "Playback Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PopulateAudioTracks()
        {
            audioTrackMenuItem.DropDownItems.Clear();

            if (_mediaPlayer != null && _mediaPlayer.Media != null)
            {
                var audioTracks = _mediaPlayer.Media.Tracks.Where(t => t.TrackType == TrackType.Audio).ToList();

                if (audioTracks.Count > 1)
                {
                    for (int i = 0; i < audioTracks.Count; i++)
                    {
                        var track = audioTracks[i];
                        var item = new ToolStripMenuItem();
                        //item.Text = $"Track {i + 1}: {LanguageCodeConverter.ConvertLanguageCode(track.Language ?? "Unknown")}";
                        item.Text = $"Track {i + 1}: {LanguageMapper.GetLanguageName(track.Language ?? "Unknown")}";
                        item.Tag = track.Id;
                        item.Click += AudioTrackMenuItem_Click;

                        if (_mediaPlayer.AudioTrack == track.Id)
                        {
                            item.Checked = true;
                        }

                        audioTrackMenuItem.DropDownItems.Add(item);
                    }
                }
                else
                {
                    var noTrackItem = new ToolStripMenuItem("No additional audio tracks");
                    noTrackItem.Enabled = false;
                    audioTrackMenuItem.DropDownItems.Add(noTrackItem);
                }
            }
        }



        private void AudioTrackMenuItem_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem && menuItem.Tag is int trackId)
            {
                _mediaPlayer.SetAudioTrack(trackId);

                // Update the checked state
                foreach (ToolStripMenuItem item in audioTrackMenuItem.DropDownItems)
                {
                    item.Checked = (item.Tag is int id && id == trackId);
                }
            }
        }

        // Update the PrevSubButton_Click method to keep NumericUpDown in sync
        private void PrevSubButton_Click(object sender = null, EventArgs e = null)
        {

            this.ActiveControl = null;

            if (subtitles != null && subtitles.Count != 0 && currentSubtitleIndex > 0)
            {
                UpdateCurrentSubtitleIndex();
                if (currentSubtitleIndex <= 0) { return; }
                //if (currentSubtitleIndex > 0)
                //{
                currentSubtitleIndex--;
                subUpDown.Value = currentSubtitleIndex + 1; // This will trigger SubUpDown_ValueChanged
                ShowNotification("Previous subtitle");
                //}
            }
            else
            {
                long mediaTime = _mediaPlayer.Time;
                long mediaLength = _mediaPlayer.Length;
                if (mediaTime != -1 && mediaLength != -1)
                {
                    if (mediaTime > 10000)
                    {
                        _mediaPlayer.Time = mediaTime - 10000;
                    }
                    else
                    {
                        _mediaPlayer.Time = 0;
                    }

                }
            }

        }

        // Update the NextSubButton_Click method to keep NumericUpDown in sync
        private void NextSubButton_Click(object sender = null, EventArgs e = null)
        {
            this.ActiveControl = null;
            if (subtitles != null && subtitles.Count != 0 && currentSubtitleIndex < subtitles.Count - 1)
            {
                UpdateCurrentSubtitleIndex();
                if (currentSubtitleIndex >= subtitles.Count - 1) { return; }
                //if (currentSubtitleIndex < subtitles.Count - 1)
                //{
                currentSubtitleIndex++;
                subUpDown.Value = currentSubtitleIndex + 1; // This will trigger SubUpDown_ValueChanged
                ShowNotification("Next subtitle");
                //}
            }
            else
            {
                long mediaTime = _mediaPlayer.Time;
                long mediaLength = _mediaPlayer.Length;
                if (mediaTime != -1 && mediaLength != -1 && mediaLength > mediaTime + 10000)
                {
                    _mediaPlayer.Time = mediaTime + 10000;
                }
            }
        }

        // Update the SubUpDown_ValueChanged method to handle navigation
        private void SubUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (subtitles != null && subtitles.Count > 0)
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
                pointA = null; pointB = null;
            }
        }

        private void UpdateCurrentSubtitleIndex()
        {
            if (subtitles != null && subtitles.Count != 0)
            {
                // Check if values are NOT within 2 positions of each other
                if (Math.Abs(currentSubtitleIndex - realTimeSubtitleIndex) >= 2)
                {
                    // Execute code only when values are far apart
                    subUpDown.Value = realTimeSubtitleIndex + 1;

                }
            }

        }

        private void PlaySubtitle(int index)
        {
            if (subtitles != null && subtitles.Count > 0 && index >= 0 && index < subtitles.Count)
            {
                var subtitle = subtitles[index];
                //===================================
                var currentTime = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
                if (_mediaPlayer.Time != -1 &&/* _mediaPlayer.IsPlaying &&*/ currentTime < subtitles[0].StartTime)
                {
                    _mediaPlayer.Time = (long)subtitles[0].StartTime.TotalMilliseconds + 11;
                    subUpDown.Value = 1;
                    return;
                }
                //===================================
                _mediaPlayer.Time = (long)subtitle.StartTime.TotalMilliseconds + 11;
                //_mediaPlayer.SeekTo(subtitle.StartTime);
            }
        }



        private void LoopTimer_Tick(object sender, EventArgs e)
        {


            if (pointA == null || pointB == null)
            {
                if (!isLooping || subtitles == null || subtitles.Count == 0 || _mediaPlayer == null)
                    return;

                var currentTime = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
                var currentIndex = currentSubtitleIndex;

                // First, verify if we're still within the current subtitle's time range
                if (currentIndex >= 0 && currentIndex < subtitles.Count)
                {
                    var currentSub = subtitles[currentIndex];
                    // If we've passed the end time of current subtitle
                    if (currentTime > currentSub.EndTime)
                    {

                        if (isPauseFeature.Checked)
                        {
                            int subTimeRange = (int)(currentSub.EndTime.TotalMilliseconds - currentSub.StartTime.TotalMilliseconds);
                            //Debug.WriteLine("\n"+subTimeRange.ToString());

                            //this following line for Compensating the difference between the real time and the speed time
                            subTimeRange = (int)((float)subTimeRange / (float)speedNumericUpDown.Value);
                            //Debug.WriteLine(subTimeRange.ToString());

                            //this following line for adding extra margin to the time range
                            subTimeRange = (int)(subTimeRange * 1.25);
                            //Debug.WriteLine(subTimeRange.ToString());

                            subTimeRange = (int)Math.Ceiling(subTimeRange / 100.0) * 100;
                            //Debug.WriteLine(subTimeRange.ToString());
                            PauseAndResume(subTimeRange);

                            //=====================================
                            // Check if we should repeat
                            if (targetRepeatCount == 0 || currentRepeatCount < targetRepeatCount)
                            {
                                _mediaPlayer.Time = (long)currentSub.StartTime.TotalMilliseconds + 11;
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
                                }
                                else
                                {
                                    // We've reached the end of subtitles
                                    isLooping = false;
                                    loopTimer.Stop();
                                    loopButton.Text = "Loop: OFF";
                                }
                            }
                            //=====================================
                        }
                        else
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
                }
            }
            else
            {
                if (!isLooping || _mediaPlayer == null)
                    return;

                var currentTime = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
                TimeSpan startTime = (TimeSpan)pointA;
                TimeSpan endTime = (TimeSpan)pointB;
                if (pointA > pointB)
                {
                    startTime = (TimeSpan)pointB;
                    endTime = (TimeSpan)pointA;
                }


                if (currentTime > endTime)
                {

                    if (isPauseFeature.Checked)
                    {
                        int subTimeRange = (int)(endTime.TotalMilliseconds - startTime.TotalMilliseconds);
                        subTimeRange = (int)((float)subTimeRange / (float)speedNumericUpDown.Value);
                        subTimeRange = (int)Math.Ceiling(subTimeRange / 100.0) * 100;
                        PauseAndResume(subTimeRange);
                    }
                    _mediaPlayer.Time = (long)startTime.TotalMilliseconds;



                }

            }
        }

        Timer pauseTimer = new Timer();



        private void PauseAndResume(int timerInterval)
        {
            // Pause the main timer
            loopTimer.Stop();
            _mediaPlayer.Pause();
            if (subtitles != null && subtitles.Count != 0 && _mediaPlayer != null)
            {
                _mediaPlayer.Time = (long)subtitles[currentSubtitleIndex].EndTime.TotalMilliseconds - 11;
            }

            // Create a new timer to resume after 5 seconds

            pauseTimer.Interval = timerInterval; // 5000 = 5 seconds
            pauseTimer.Tick += ResumePausedTimer;
            pauseTimer.Start();
        }

        private void ResumePausedTimer(object sender, EventArgs e)
        {
            pauseTimer.Stop();

            if (playPauseButton.Text == "Pause")
            {
                _mediaPlayer.Play();
            }

            loopTimer.Start();
        }




        // New methods for timeline control
        private void TimeUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (_mediaPlayer != null && !isTimelineBeingDragged)
            {
                UpdateTimeDisplay();

                // Add this line to update subtitle display
                UpdateSubtitleDisplay();
            }
        }

        private void UpdateSubtitleDisplay()
        {
            // Check if we have subtitles loaded
            if (subtitles != null && subtitles.Count > 0)
            {
                // Convert the video's current time from milliseconds to a TimeSpan
                var currentTimeSpan = TimeSpan.FromMilliseconds(_mediaPlayer.Time + 10);


                if (currentTimeSpan <= subtitles[0].StartTime)
                {

                    subtitleTrackingLabel.Text = $"Subtitles: {0 + 1}/{subtitles.Count}";
                    realTimeSubtitleIndex = 0;
                    //====================================================
                    // Update the subtitle text in the RichTextBox
                    if (subtitleRichTextBox.Text != subtitles[0].Text)
                    {
                        subtitleRichTextBox.Text = subtitles[0].Text;
                        if (subtitleRichTextBox.Focused)
                        {
                            //subtitlePanel.Focus();
                            this.ActiveControl = null;
                        }
                        if (subtitleRichTextBox.Lines.Length > 1)
                        {
                            subtitlePanel.Height = subtitleRichTextBox.Lines.Length * subtitlePanelLineHight;
                        }
                        else
                        {
                            subtitlePanel.Height = 2 * subtitlePanelLineHight;
                        }
                    }
                    //====================================================
                    return;
                }

                if (currentTimeSpan >= subtitles[subtitles.Count - 1].StartTime)
                {

                    subtitleTrackingLabel.Text = $"Subtitles: {subtitles.Count - 1 + 1}/{subtitles.Count}";
                    realTimeSubtitleIndex = subtitles.Count - 1;
                    //====================================================
                    // Update the subtitle text in the RichTextBox
                    if (subtitleRichTextBox.Text != subtitles[subtitles.Count - 1].Text)
                    {
                        subtitleRichTextBox.Text = subtitles[subtitles.Count - 1].Text;
                        if (subtitleRichTextBox.Focused)
                        {
                            this.ActiveControl = null;
                        }
                        if (subtitleRichTextBox.Lines.Length > 1)
                        {
                            subtitlePanel.Height = subtitleRichTextBox.Lines.Length * subtitlePanelLineHight;
                        }
                        else
                        {
                            subtitlePanel.Height = 2 * subtitlePanelLineHight;
                        }
                    }
                    //====================================================
                    return;
                }


                // Loop through all subtitles
                for (int i = 0; i < subtitles.Count + 1; i++)
                {

                    // Check if current video time falls within this subtitle's time range
                    if (currentTimeSpan >= subtitles[i].StartTime &&
                        currentTimeSpan < subtitles[i + 1].StartTime)
                    {

                        subtitleTrackingLabel.Text = $"Subtitles: {i + 1}/{subtitles.Count}";
                        realTimeSubtitleIndex = i;
                        //====================================================
                        // Update the subtitle text in the RichTextBox
                        if (subtitleRichTextBox.Text != subtitles[i].Text)
                        {
                            subtitleRichTextBox.Text = subtitles[i].Text;
                            if (subtitleRichTextBox.Focused)
                            {
                                this.ActiveControl = null;
                            }
                            if (subtitleRichTextBox.Lines.Length > 1)
                            {
                                subtitlePanel.Height = subtitleRichTextBox.Lines.Length * subtitlePanelLineHight;
                            }
                            else
                            {
                                subtitlePanel.Height = 2 * subtitlePanelLineHight;
                            }
                        }
                        //====================================================
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

                if (isNegativeTotalTime)
                {
                    TimeSpan totalTime = TimeSpan.FromMilliseconds(_mediaPlayer.Length - _mediaPlayer.Time);
                    totalTimeLabel.Text = "-" + totalTime.ToString(@"hh\:mm\:ss");
                    //totalTimeLabel.Text = "-" + totalTimeLabel.Text;
                }
            }
        }

        string currentMediaTotalTime;

        private void UpdateTotalDuration()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(UpdateTotalDuration));
                return;
            }

            if (_mediaPlayer != null && _mediaPlayer.Length > 0)
            {
                if (isNegativeTotalTime)
                {
                    TimeSpan totalTime = TimeSpan.FromMilliseconds(_mediaPlayer.Length - _mediaPlayer.Time);
                    totalTimeLabel.Text = totalTime.ToString(@"hh\:mm\:ss");
                    totalTimeLabel.Text = "-" + totalTimeLabel.Text;
                }
                else
                {
                    TimeSpan totalTime = TimeSpan.FromMilliseconds(_mediaPlayer.Length);
                    totalTimeLabel.Text = totalTime.ToString(@"hh\:mm\:ss");
                }

                currentMediaTotalTime = "-";
                currentMediaTotalTime += TimeSpan.FromMilliseconds(_mediaPlayer.Length).ToString(@"hh\:mm\:ss");

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

                    // Remove ASS/SSA and HTML tags:
                    text = RemoveAssTags(text);
                    text = RemoveHtmlTags(text);
                    text = ReplaceMultipleSpacesWithSingleSpace(text);

                    if (!string.IsNullOrEmpty(text.Trim()))
                    {
                        subtitles.Add(new SubtitleEntry
                        {
                            Index = index,
                            StartTime = startTime,
                            EndTime = endTime,
                            Text = text.Trim()
                        });
                    }


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


        private static string RemoveHtmlTags(string input)
        {
            return Regex.Replace(input, "<.*?>", string.Empty);
        }
        private static string RemoveAssTags(string input)
        {
            // This regex matches ASS/SSA tags enclosed in curly braces {}
            return Regex.Replace(input, @"\{\\.*?\}", string.Empty);
        }

        public static string ReplaceMultipleSpacesWithSingleSpace(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            // Use a regular expression to replace multiple spaces with a single space,
            // but exclude newline characters.
            return Regex.Replace(input, @"[ ]{2,}", " ");
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

        private TextBox searchTextBox;
        private List<SubtitleParser.SubtitleEntry> allSubtitles; // Store the original full list

        private List<int> filteredIndices = new List<int>(); // Add this field to store original indices

        public SubtitleListForm(List<SubtitleParser.SubtitleEntry> subtitles, Action<int> onSubtitleSelected)
        {
            this.onSubtitleSelected = onSubtitleSelected;
            InitializeSubtitleList(subtitles);
            SetupForm();


            EnableDarkMode();
            this.Shown += SubtitleListForm_Shown;
        }

        private void SubtitleListForm_Shown(object sender, EventArgs e)
        {
            searchTextBox.Focus(); // Set focus to the searchTextBox
            searchTextBox.SelectAll(); // Select all text in the searchTextBox
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




            searchTextBox = new TextBox
            {
                //Location = new Point(10, 10), // Adjust as needed
                Dock = DockStyle.Top,
                //Size = new Size(this.ClientSize.Width - 20, 25),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            searchTextBox.TextChanged += SearchTextBox_TextChanged;
            this.Controls.Add(searchTextBox);

            Label searchLabel = new Label();
            searchLabel.Text = "Search:";
            searchLabel.Dock = DockStyle.Top;
            searchLabel.AutoSize = true;
            this.Controls.Add(searchLabel);



        }

        private void SearchTextBox_TextChanged(object sender, EventArgs e)
        {
            searchTextBox.Text = NormalizeTextForSearch(searchTextBox.Text);
            FilterSubtitles(searchTextBox.Text);
        }






        private void FilterSubtitles(string searchText)
        {
            subtitleListBox.Items.Clear();
            filteredIndices.Clear();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                for (int i = 0; i < allSubtitles.Count; i++)
                {
                    // Normalize text for display in the ListBox
                    string displayText = NormalizeTextForSearch(allSubtitles[i].Text);
                    subtitleListBox.Items.Add($"{i + 1}. {displayText}");
                    filteredIndices.Add(i);
                }
            }
            else
            {
                for (int i = 0; i < allSubtitles.Count; i++)
                {
                    // Normalize for searching
                    string normalizedSubtitleText = NormalizeTextForSearch(allSubtitles[i].Text);

                    if (normalizedSubtitleText.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Normalize text for display in the ListBox
                        string displayText = NormalizeTextForSearch(allSubtitles[i].Text);
                        subtitleListBox.Items.Add($"{i + 1}. {displayText}");
                        filteredIndices.Add(i);
                    }
                }
            }
        }




        private void InitializeSubtitleList(List<SubtitleParser.SubtitleEntry> subtitles)
        {
            allSubtitles = subtitles; // Store the original list

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
            }






            subtitleListBox.DoubleClick += (s, e) =>
            {
                if (subtitleListBox.SelectedIndex != -1)
                {
                    // Get the original index from filteredIndices
                    int originalIndex = filteredIndices[subtitleListBox.SelectedIndex];
                    onSubtitleSelected(originalIndex);
                }
            };



            subtitleListBox.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    int index = subtitleListBox.IndexFromPoint(e.X, e.Y);
                    if (index != ListBox.NoMatches)
                    {
                        subtitleListBox.SelectedIndex = index;
                    }
                }
            };







            // In your SubtitleListForm class, in the context menu setup:
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

                    // Get the original index from filteredIndices
                    int originalIndex = filteredIndices[subtitleListBox.SelectedIndex];

                    if (!string.IsNullOrEmpty(allSubtitles[originalIndex].Text))
                    {
                        // Use the original text with newline characters for copying
                        Clipboard.SetText(allSubtitles[originalIndex].Text);
                    }

                }
            };
            contextMenu.Items.Add(copyItem);
            subtitleListBox.ContextMenuStrip = contextMenu;


            this.Controls.Add(subtitleListBox);
            FilterSubtitles(""); // Initial filtering (show all)
        }


        private string NormalizeTextForSearch(string text)
        {
            return text.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
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


            Panel linksPanel = new Panel();
            //linksPanel.BorderStyle = BorderStyle.FixedSingle;
            linksPanel.BackColor = Color.FromArgb(32, 32, 32);
            linksPanel.Size = new Size(400, 50);
            linksPanel.Location = new Point(Convert.ToInt32(this.ClientSize.Width / 2 - aboutPanel.Width / 2),
                       Convert.ToInt32(this.ClientSize.Height / 2 - aboutPanel.Height / 2) + aboutPanel.Height + 4);



            PictureBox logoPictureBox = new PictureBox();
            logoPictureBox.Image = Properties.Resources.suvi_high_resolution_logo2_modified;
            logoPictureBox.Location = new Point(10, 16);
            logoPictureBox.Size = new Size(120, 120);
            logoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;


            TextBox aboutTextBox = new TextBox();
            aboutTextBox.ReadOnly = true;
            aboutTextBox.Multiline = true;
            aboutTextBox.BorderStyle = BorderStyle.None;
            aboutTextBox.BackColor = aboutPanel.BackColor;
            aboutTextBox.ForeColor = Color.White;
            aboutTextBox.Font = new Font("Segoe UI Semibold", 12, FontStyle.Bold);
            aboutTextBox.Location = new Point(132, 30);
            aboutTextBox.Size = new Size(260, 110);
            aboutTextBox.TextAlign = HorizontalAlignment.Center;

            aboutTextBox.TabStop = false;
            // Get the version from the current assembly
            Version version = Assembly.GetExecutingAssembly().GetName().Version;

            aboutTextBox.Text = "Product Name: SuVi Player \r\n" +
                "Created by: Ahmed Ismail\r\n" +
                "Email: elcoder01@gmail.com\r\n" +
                $"Version: {version}";


            // GitHub Link
            LinkLabel githubLinkLabel = new LinkLabel();
            githubLinkLabel.Text = "GitHub Repository";
            githubLinkLabel.Text = "https://github.com/ahmedismailc/SuViPlayer";
            githubLinkLabel.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            githubLinkLabel.LinkColor = Color.White; // Or your preferred link color
            githubLinkLabel.ActiveLinkColor = Color.Gray;
            githubLinkLabel.VisitedLinkColor = Color.White;
            githubLinkLabel.Location = new Point(0, 0); // Adjust position as needed
            githubLinkLabel.AutoSize = true;
            githubLinkLabel.LinkClicked += (sender, e) =>
            {
                Process.Start("https://github.com/ahmedismailc/SuViPlayer"); // Your GitHub URL
                //githubLinkLabel.LinkVisited = true;
            };

            // YouTube Link
            LinkLabel youtubeLinkLabel = new LinkLabel();
            youtubeLinkLabel.Text = "YouTube Channel";
            youtubeLinkLabel.Text = "https://www.youtube.com/@ahmedismailc";
            youtubeLinkLabel.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            youtubeLinkLabel.LinkColor = Color.White;
            youtubeLinkLabel.ActiveLinkColor = Color.Gray;
            youtubeLinkLabel.VisitedLinkColor = Color.White;
            youtubeLinkLabel.Location = new Point(0, 20); // Adjust position as needed
            youtubeLinkLabel.AutoSize = true;
            youtubeLinkLabel.LinkClicked += (sender, e) =>
            {
                Process.Start("https://www.youtube.com/@ahmedismailc"); // Your YouTube URL
                //youtubeLinkLabel.LinkVisited = true;
            };



            aboutPanel.Controls.Add(logoPictureBox);
            aboutPanel.Controls.Add(aboutTextBox);
            this.Controls.Add(aboutPanel);

            linksPanel.Controls.Add(githubLinkLabel);
            linksPanel.Controls.Add(youtubeLinkLabel);
            this.Controls.Add(linksPanel);

            this.Resize += (sender, e) =>
            {
                aboutPanel.Location = new Point(Convert.ToInt32(this.ClientSize.Width / 2 - aboutPanel.Width / 2),
                    Convert.ToInt32(this.ClientSize.Height / 2 - aboutPanel.Height / 2));

                linksPanel.Location = new Point(Convert.ToInt32(this.ClientSize.Width / 2 - aboutPanel.Width / 2),
                      Convert.ToInt32(this.ClientSize.Height / 2 - aboutPanel.Height / 2) + 154);
            };

        }


    }





    public class ShortcutsForm : MyTemplateForm
    {
        public ShortcutsForm()
        {
            SetupForm();
        }

        private void SetupForm()
        {
            this.Size = new Size(620, 600);
            this.Text = "Shortcuts";
            this.AutoScroll = true;
            this.DoubleBuffered = true;

            this.Scroll += (sender, e) =>
            {
                // Invalidate and update the entire form
                this.Invalidate();
                this.Update();
            };

            TableLayoutPanel table = new TableLayoutPanel
            {
                //Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(10),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
                ,
                AutoSize = true
                ,
                Width = 560
                ,
                Location = new Point(10, 10)
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
            {"V","Hide or show subtitles" },
            {"R","Toggle Repeat After Me" },
            {"T","Hide or show Subtitle Panel" },
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




    public class MyListForm : MyTemplateForm
    {
        private ListBox wordsListBox;
        private Button copyAllButton;
        private Button copyTodaysButton;
        private string _dbFilePath;
        private Button clearListButton;

        public MyListForm(string dbFilePath)
        {
            _dbFilePath = dbFilePath; // Assign the passed value
            InitializeComponents();
            LoadWordsIntoListBox();
        }

        private void InitializeComponents()
        {
            this.SuspendLayout();

            // Main form settings (adjust as needed)
            this.Size = new Size(500, 600);
            this.Text = "My List";

            // ListBox to display words
            wordsListBox = new ListBox();
            wordsListBox.Dock = DockStyle.Top;
            wordsListBox.Font = new Font(this.Font.FontFamily, 11);
            wordsListBox.Height = this.ClientSize.Height - 40; // Adjust height to make room for buttons
            wordsListBox.SelectionMode = SelectionMode.MultiExtended; // Allow multiple selection for copying
            wordsListBox.BackColor = this.BackColor;
            wordsListBox.ForeColor = this.ForeColor;
            wordsListBox.BorderStyle = BorderStyle.FixedSingle;
            wordsListBox.SelectedIndexChanged += WordsListBox_SelectedIndexChanged;

            // Button to copy all words
            copyAllButton = new Button();
            copyAllButton.Text = "Copy All Words";
            copyAllButton.Location = new Point(10, this.ClientSize.Height - 37); // Adjust position
            copyAllButton.Width = 150;
            copyAllButton.Click += CopyAllButton_Click;
            copyAllButton.ApplyCustomStyle();

            // Button to copy today's words
            copyTodaysButton = new Button();
            copyTodaysButton.Text = "Copy Today's Words";
            copyTodaysButton.Location = new Point(170, this.ClientSize.Height - 37); // Adjust position
            copyTodaysButton.Width = 150;
            copyTodaysButton.Click += CopyTodaysButton_Click;
            copyTodaysButton.ApplyCustomStyle();

            // Button to clear the list
            clearListButton = new Button();
            clearListButton.Text = "Clear List";
            clearListButton.Location = new Point(330, this.ClientSize.Height - 37); // Adjust position
            clearListButton.Width = 150;
            clearListButton.Click += ClearListButton_Click;
            clearListButton.ApplyCustomStyle();

            // Add controls to the form
            this.Controls.Add(wordsListBox);
            this.Controls.Add(copyAllButton);
            this.Controls.Add(copyTodaysButton);
            this.Controls.Add(clearListButton);

            this.ResumeLayout(false);
        }

        private void WordsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Enable copy functionality if items are selected
            if (wordsListBox.SelectedItems.Count > 0)
            {
                wordsListBox.ContextMenuStrip = new ContextMenuStrip();
                wordsListBox.ContextMenuStrip.Items.Add("Copy", null, CopySelectedWords_Click);
            }
            else
            {
                wordsListBox.ContextMenuStrip = null;
            }
        }

        private void CopySelectedWords_Click(object sender, EventArgs e)
        {
            CopySelectedWordsToClipboard();
        }



        private void ClearListButton_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to clear the entire list?", "Confirm Clear",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                ClearAllWordsFromDatabase(); // Implement this method
                LoadWordsIntoListBox(); // Refresh the listbox
            }
        }


        private void ClearAllWordsFromDatabase()
        {
            if (string.IsNullOrEmpty(_dbFilePath)) return;

            try
            {
                using (var connection = new SQLiteConnection($"Data Source={_dbFilePath};Version=3;"))
                {
                    connection.Open();

                    string deleteQuery = "DELETE FROM MyList"; // Clear all data
                    using (var command = new SQLiteCommand(deleteQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing list: {ex.Message}");
            }
        }


        private void LoadWordsIntoListBox()
        {
            List<string> allWords = GetAllWordsFromDatabase(); // Assuming you have this method from before

            // Group words by the date they were added
            var groupedWords = allWords.GroupBy(word =>
            {
                // Extract the date part from the combined "word;date" string
                string datePart = word.Split(';')[1];
                return DateTime.Parse(datePart).Date; // This gives you just the date (ignoring time)
            })
            .OrderByDescending(group => group.Key); // Order groups by date in descending order

            wordsListBox.Items.Clear(); // Clear existing items

            foreach (var group in groupedWords)
            {
                // Add a date separator
                wordsListBox.Items.Add($"--- {group.Key.ToShortDateString()} ---");

                // Add the words for that date
                foreach (string wordEntry in group)
                {
                    // Extract the word part from the combined "word;date" string
                    string wordPart = wordEntry.Split(';')[0];
                    wordsListBox.Items.Add(wordPart);
                }
            }
        }

        private void CopyAllButton_Click(object sender, EventArgs e)
        {
            string allWords = string.Join(Environment.NewLine, wordsListBox.Items.Cast<string>().Where(item => !item.StartsWith("---")));
            if (!string.IsNullOrEmpty(allWords))
            {
                Clipboard.SetText(allWords);
                MessageBox.Show("All words copied to clipboard!", "Copy All", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void CopyTodaysButton_Click(object sender, EventArgs e)
        {
            string todaysDate = $"--- {DateTime.Today.ToShortDateString()} ---";
            List<string> todaysWords = new List<string>();
            bool foundTodaysSection = false;

            foreach (string item in wordsListBox.Items)
            {
                if (item == todaysDate)
                {
                    foundTodaysSection = true;
                }
                else if (foundTodaysSection && !item.StartsWith("---"))
                {
                    todaysWords.Add(item);
                }
                else if (foundTodaysSection && item.StartsWith("---"))
                {
                    break; // Stop when we reach the next date section
                }
            }

            if (todaysWords.Any())
            {
                Clipboard.SetText(string.Join(Environment.NewLine, todaysWords));
                MessageBox.Show("Today's words copied to clipboard!", "Copy Today's Words", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("No words found for today.", "Copy Today's Words", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void CopySelectedWordsToClipboard()
        {
            if (wordsListBox.SelectedItems.Count > 0)
            {
                // Filter out date separators and join selected words
                string selectedWords = string.Join(Environment.NewLine, wordsListBox.SelectedItems.Cast<string>().Where(item => !item.StartsWith("---")));
                if (!string.IsNullOrEmpty(selectedWords))
                {
                    Clipboard.SetText(selectedWords);
                    MessageBox.Show("Selected words copied to clipboard!", "Copy Selected Words", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        private List<string> GetAllWordsFromDatabase()
        {
            List<string> words = new List<string>();
            if (string.IsNullOrEmpty(_dbFilePath)) { return words; }

            try
            {
                using (var connection = new SQLiteConnection($"Data Source={_dbFilePath};Version=3;"))
                {
                    connection.Open();

                    string selectQuery = "SELECT WordOrPhrase, DateAdded FROM MyList ORDER BY DateAdded DESC";
                    using (var command = new SQLiteCommand(selectQuery, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string word = reader.GetString(0);
                                string dateAdded = reader.GetString(1);
                                // Combine word and date into a single string
                                words.Add($"{word};{dateAdded}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error retrieving words: {ex.Message}");
            }

            return words;
        }
    }

    public class DictionaryLink
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public int ListOrder { get; set; }
        public bool IsDeleted { get; set; } = false; // Add this property
    }

    public class DictionaryLinksForm : MyTemplateForm
    {

        private ListView dictionariesListView;
        private Button addDictionaryButton;
        private Button deleteDictionaryButton;
        private Button upButton; // New button
        private Button downButton; // New button
        private Button saveButton; // New button
        private Button resetButton;
        private string dbFilePath;
        private List<DictionaryLink> dictionaryLinks; // Store the data
        private bool isDirty = false;

        public DictionaryLinksForm(string dbFilePath)
        {
            this.dbFilePath = dbFilePath;
            InitializeComponents();
            LoadDictionaryLinks();
        }

        private void InitializeComponents()
        {
            // ... (Similar UI setup as in MyListForm)
            this.SuspendLayout();

            // Main form settings
            this.Size = new Size(600, 600);
            this.Text = "Links";
            this.FormClosing += DictionaryLinksForm_FormClosing;


            // ListView setup
            dictionariesListView = new ListView();
            dictionariesListView.Dock = DockStyle.Top;
            dictionariesListView.Height = this.ClientSize.Height - 50; // Adjust height
            dictionariesListView.View = View.Details; // Important: Set the view to Details
            dictionariesListView.FullRowSelect = true;
            //dictionariesListView.GridLines = true;
            dictionariesListView.MultiSelect = false;
            dictionariesListView.BackColor = this.BackColor;
            dictionariesListView.ForeColor = this.ForeColor;
            dictionariesListView.BorderStyle = BorderStyle.FixedSingle;
            dictionariesListView.HeaderStyle = ColumnHeaderStyle.None;

            // Add columns (adjust as needed)
            dictionariesListView.Columns.Add("Name", 150, HorizontalAlignment.Left);
            dictionariesListView.Columns.Add("Url", 432, HorizontalAlignment.Left); // You might want to make URL visible here
            //dictionariesListView.Columns.Add("ListOrder", 0, HorizontalAlignment.Left); // Keep a hidden ListOrder column for sorting

            // Add event handlers (modify as needed)
            //dictionariesListView.SelectedIndexChanged += DictionariesListView_SelectedIndexChanged;
            dictionariesListView.MouseDoubleClick += DictionariesListView_MouseDoubleClick;


            // Up button
            upButton = new Button();
            upButton.Text = "Up";
            upButton.Location = new Point(10, this.ClientSize.Height - 37);
            upButton.Width = 75;
            upButton.Click += UpButton_Click;
            upButton.ApplyCustomStyle();

            // Down button
            downButton = new Button();
            downButton.Text = "Down";
            downButton.Location = new Point(95, this.ClientSize.Height - 37);
            downButton.Width = 75;
            downButton.Click += DownButton_Click;
            downButton.ApplyCustomStyle();

            // Add Dictionary button
            addDictionaryButton = new Button();
            addDictionaryButton.Text = "Add";
            addDictionaryButton.Location = new Point(180, this.ClientSize.Height - 37);
            addDictionaryButton.Width = 75;
            addDictionaryButton.Click += AddDictionaryButton_Click;
            addDictionaryButton.ApplyCustomStyle();

            // Delete Dictionary button
            deleteDictionaryButton = new Button();
            deleteDictionaryButton.Text = "Delete";
            deleteDictionaryButton.Location = new Point(265, this.ClientSize.Height - 37);
            deleteDictionaryButton.Width = 75;
            deleteDictionaryButton.Click += DeleteDictionaryButton_Click;
            deleteDictionaryButton.ApplyCustomStyle();

            // Save button
            saveButton = new Button();
            saveButton.Text = "Save";
            saveButton.Location = new Point(350, this.ClientSize.Height - 37);
            saveButton.Width = 75;
            saveButton.Click += SaveButton_Click;
            saveButton.ApplyCustomStyle();

            // Save button
            resetButton = new Button();
            resetButton.Text = "Reset";
            resetButton.Location = new Point(435, this.ClientSize.Height - 37);
            resetButton.Width = 75;
            resetButton.Click += ResetButton_Click;
            resetButton.ApplyCustomStyle();

            // Add controls to form
            this.Controls.Add(dictionariesListView);
            this.Controls.Add(upButton);
            this.Controls.Add(downButton);
            this.Controls.Add(addDictionaryButton);
            this.Controls.Add(deleteDictionaryButton);
            this.Controls.Add(saveButton);
            this.Controls.Add(resetButton);

            this.ResumeLayout(false);
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to reset the links to their default values?", "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                ResetDictionaryLinksToDefault();
                LoadDictionaryLinks(); // Refresh the list view
                isDirty = false;
            }
        }

        private void ResetDictionaryLinksToDefault()
        {
            if (string.IsNullOrEmpty(dbFilePath)) return;

            try
            {
                using (var connection = new SQLiteConnection($"Data Source={dbFilePath};Version=3;"))
                {
                    connection.Open();

                    // 1. Clear existing links
                    string clearTableQuery = "DELETE FROM DictionaryLinks";
                    using (var clearCommand = new SQLiteCommand(clearTableQuery, connection))
                    {
                        clearCommand.ExecuteNonQuery();
                    }

                    // 2. Insert default links
                    string insertLinksQuery = @"
                    INSERT INTO DictionaryLinks (Name, Url, ListOrder) VALUES
                    ('Google Translate', 'https://translate.google.com/?text={word}', 1),
                    ('Google Search', 'https://www.google.com/search?q={word}', 2),
                    ('Google Definition', 'https://www.google.com/search?q={word}+definition', 3),
                    ('Google Pronunciation', 'https://www.google.com/search?q=how+to+pronounce+{word}', 4),
                    ('Youglish', 'https://youglish.com/pronounce/{word}/english', 5),
                    ('Oxford', 'https://www.oxfordlearnersdictionaries.com/definition/english/{word}', 6),
                    ('Merriam-Webster', 'https://www.merriam-webster.com/dictionary/{word}', 7),
                    ('Dictionary.com', 'https://www.dictionary.com/browse/{word}', 8),
                    ('Cambridge Dictionary', 'https://dictionary.cambridge.org/dictionary/english/{word}', 9);";

                    using (var insertCommand = new SQLiteCommand(insertLinksQuery, connection))
                    {
                        insertCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting links: {ex.Message}");
            }
        }

        private void DictionaryLinksForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isDirty)
            {
                DialogResult result = MessageBox.Show("You have unsaved changes. Do you want to save them before closing?", "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (result == DialogResult.Yes)
                {
                    SaveButton_Click(sender, e); // Save changes
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true; // Prevent the form from closing
                }
            }
        }



        private void DictionariesListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (dictionariesListView.SelectedItems.Count == 1)
            {
                ListViewItem selectedItem = dictionariesListView.SelectedItems[0];
                DictionaryLink selectedLink = (DictionaryLink)selectedItem.Tag;

                // Open AddDictionaryLinkForm in "edit mode"
                using (var editForm = new AddDictionaryLinkForm(selectedLink.Name, selectedLink.Url))
                {
                    if (editForm.ShowDialog() == DialogResult.OK && (selectedLink.Name != editForm.DictionaryName || selectedLink.Url != editForm.DictionaryUrl))
                    {
                        // Update the DictionaryLink object
                        selectedLink.Name = editForm.DictionaryName;
                        selectedLink.Url = editForm.DictionaryUrl;

                        // Update the ListViewItem
                        selectedItem.Text = selectedLink.Name;
                        selectedItem.SubItems[1].Text = selectedLink.Url; // Assuming the second column is the URL

                        // Mark changes as unsaved
                        isDirty = true;
                    }
                }
            }
        }

        private void LoadDictionaryLinks()
        {
            // Get the links from the database (ordered by ListOrder)
            dictionaryLinks = GetDictionaryLinksFromDatabase(dbFilePath).OrderBy(x => x.ListOrder).ToList();

            dictionariesListView.Items.Clear(); // Clear existing items

            foreach (DictionaryLink link in dictionaryLinks)
            {
                if (!link.IsDeleted)
                {
                    ListViewItem item = new ListViewItem(link.Name);
                    item.SubItems.Add(link.Url);
                    item.SubItems.Add(link.ListOrder.ToString());
                    item.Tag = link;

                    dictionariesListView.Items.Add(item);
                }
            }

            // Sort the ListViewItems based on ListOrder
            dictionariesListView.Sort();
        }


        // Make this method public static
        public static List<DictionaryLink> GetDictionaryLinksFromDatabase(string dbFilePath)
        {
            List<DictionaryLink> links = new List<DictionaryLink>();
            if (string.IsNullOrEmpty(dbFilePath))
            {
                return links;
            }

            try
            {
                using (var connection = new SQLiteConnection($"Data Source={dbFilePath};Version=3;"))
                {
                    connection.Open();

                    string selectQuery = "SELECT Id, Name, Url, ListOrder FROM DictionaryLinks ORDER BY ListOrder";
                    using (var command = new SQLiteCommand(selectQuery, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                links.Add(new DictionaryLink
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader.GetString(1),
                                    Url = reader.GetString(2),
                                    ListOrder = reader.GetInt32(3)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error retrieving links: {ex.Message}");
            }

            return links;
        }





        private void UpButton_Click(object sender, EventArgs e)
        {
            MoveSelectedRow(-1);
        }

        private void DownButton_Click(object sender, EventArgs e)
        {
            MoveSelectedRow(1);
        }



        private void MoveSelectedRow(int direction)
        {
            if (dictionariesListView.SelectedItems.Count > 0)
            {
                int currentIndex = dictionariesListView.SelectedItems[0].Index;
                int newIndex = currentIndex + direction;

                if (newIndex >= 0 && newIndex < dictionariesListView.Items.Count)
                {
                    // Get the item to be moved
                    ListViewItem itemToMove = dictionariesListView.SelectedItems[0];

                    // Get the DictionaryLink objects for the items being swapped
                    DictionaryLink linkToMove = (DictionaryLink)itemToMove.Tag;
                    DictionaryLink linkToSwapWith = (DictionaryLink)dictionariesListView.Items[newIndex].Tag;

                    // Swap the ListOrder values in the in-memory list (dictionaryLinks)
                    int tempOrder = linkToMove.ListOrder;
                    linkToMove.ListOrder = linkToSwapWith.ListOrder;
                    linkToSwapWith.ListOrder = tempOrder;

                    // **Visually swap the items in the ListView**
                    dictionariesListView.Items.RemoveAt(currentIndex);
                    dictionariesListView.Items.Insert(newIndex, itemToMove);

                    // Update ListViewItem text to reflect new ListOrder (if visible)
                    itemToMove.SubItems[2].Text = linkToMove.ListOrder.ToString();
                    dictionariesListView.Items[currentIndex].SubItems[2].Text = linkToSwapWith.ListOrder.ToString();

                    // Reselect the moved item
                    dictionariesListView.Items[newIndex].Selected = true;

                    // Mark changes as unsaved
                    isDirty = true;
                }
                dictionariesListView.Focus();
            }
        }



        private void AddDictionaryButton_Click(object sender, EventArgs e)
        {
            using (var addForm = new AddDictionaryLinkForm())
            {
                if (addForm.ShowDialog() == DialogResult.OK)
                {
                    // 1. Find the maximum ListOrder in the IN-MEMORY LIST (dictionaryLinks)
                    int maxListOrder = 0;
                    if (dictionaryLinks.Count > 0)
                    {
                        maxListOrder = dictionaryLinks.Max(l => l.ListOrder);
                    }

                    // 2. Determine the next ListOrder value (maxListOrder + 1)
                    int nextListOrder = maxListOrder + 1;

                    // 3. Create the new DictionaryLink object
                    DictionaryLink newLink = new DictionaryLink
                    {
                        Name = addForm.DictionaryName,
                        Url = addForm.DictionaryUrl,
                        ListOrder = nextListOrder
                    };

                    // 4. Add the new link to the in-memory list
                    dictionaryLinks.Add(newLink);

                    // 5. Create a new ListViewItem
                    ListViewItem newItem = new ListViewItem(newLink.Name);
                    newItem.SubItems.Add(newLink.Url);
                    newItem.SubItems.Add(newLink.ListOrder.ToString()); // Add ListOrder (will be hidden)
                    newItem.Tag = newLink; // Store the DictionaryLink object in the item's Tag

                    // 6. Add the new ListViewItem to the ListView
                    dictionariesListView.Items.Add(newItem);

                    // 7. Mark changes as unsaved
                    isDirty = true;
                }
            }
        }



        private void DeleteDictionaryButton_Click(object sender, EventArgs e)
        {
            if (dictionariesListView.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = dictionariesListView.SelectedItems[0];
                DictionaryLink selectedLink = (DictionaryLink)selectedItem.Tag;

                DialogResult result = MessageBox.Show($"Are you sure you want to delete '{selectedLink.Name}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.Yes)
                {
                    // 1. Mark the item as deleted (visually)
                    selectedItem.Font = new Font(selectedItem.Font, FontStyle.Strikeout); // Add a strikethrough effect
                    selectedItem.ForeColor = Color.Gray; // Change the color to gray

                    // 2. Mark the corresponding DictionaryLink object as deleted
                    selectedLink.IsDeleted = true; // We'll add an "IsDeleted" property to DictionaryLink

                    // 3. Mark changes as unsaved
                    isDirty = true;
                }
            }
            else
            {
                MessageBox.Show("Please select a link to delete.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void SaveButton_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. Update ListOrder in the dictionaryLinks list based on the ListView's current order:
                for (int i = 0; i < dictionariesListView.Items.Count; i++)
                {
                    ListViewItem item = dictionariesListView.Items[i];
                    DictionaryLink link = (DictionaryLink)item.Tag;
                    link.ListOrder = i + 1; // Update ListOrder to match the visual order
                }

                // 2. Sort the dictionaryLinks list based on ListOrder:
                dictionaryLinks = dictionaryLinks.OrderBy(l => l.ListOrder).ToList();

                // 3. Handle new/modified and deleted links in the database:
                using (var connection = new SQLiteConnection($"Data Source={dbFilePath};Version=3;"))
                {
                    connection.Open();
                    foreach (DictionaryLink link in dictionaryLinks)
                    {
                        // Update or insert the link
                        if (!link.IsDeleted)
                        {
                            if (link.Id == 0) // New link
                            {
                                string insertQuery = "INSERT INTO DictionaryLinks (Name, Url, ListOrder) VALUES (@name, @url, @listOrder)";
                                using (var command = new SQLiteCommand(insertQuery, connection))
                                {
                                    command.Parameters.AddWithValue("@name", link.Name);
                                    command.Parameters.AddWithValue("@url", link.Url);
                                    command.Parameters.AddWithValue("@listOrder", link.ListOrder);
                                    command.ExecuteNonQuery();

                                    // Get the newly inserted ID and update the link object
                                    command.CommandText = "SELECT last_insert_rowid()";
                                    link.Id = Convert.ToInt32(command.ExecuteScalar());
                                }
                            }
                            else // Existing link
                            {
                                string updateQuery = "UPDATE DictionaryLinks SET Name = @name, Url = @url, ListOrder = @listOrder WHERE Id = @id";
                                using (var command = new SQLiteCommand(updateQuery, connection))
                                {
                                    command.Parameters.AddWithValue("@name", link.Name);
                                    command.Parameters.AddWithValue("@url", link.Url);
                                    command.Parameters.AddWithValue("@listOrder", link.ListOrder);
                                    command.Parameters.AddWithValue("@id", link.Id);
                                    command.ExecuteNonQuery();
                                }
                            }
                        }
                    }

                    // Handle deleted items
                    List<DictionaryLink> deletedLinks = dictionaryLinks.Where(l => l.IsDeleted).ToList();
                    foreach (DictionaryLink link in deletedLinks)
                    {
                        DeleteDictionaryLink(link.Id); // Delete from the database
                    }
                }

                // 4. Remove deleted items from the in-memory list:
                dictionaryLinks.RemoveAll(l => l.IsDeleted);

                // 5. Refresh the ListView:
                LoadDictionaryLinks();

                MessageBox.Show("Changes saved successfully!", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
                isDirty = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving changes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteDictionaryLink(int id)
        {
            if (string.IsNullOrEmpty(dbFilePath)) return;

            try
            {
                using (var connection = new SQLiteConnection($"Data Source={dbFilePath};Version=3;"))
                {
                    connection.Open();
                    string deleteQuery = "DELETE FROM DictionaryLinks WHERE Id = @id";
                    using (var command = new SQLiteCommand(deleteQuery, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting link: {ex.Message}");
            }
        }




    }





    // A simple form to get name and URL for a new dictionary link
    public class AddDictionaryLinkForm : MyTemplateForm
    {
        public string DictionaryName { get; private set; }
        public string DictionaryUrl { get; private set; }

        private TextBox nameTextBox;
        private TextBox urlTextBox;
        private Button okButton;
        private Button cancelButton;

        public AddDictionaryLinkForm()
        {
            InitializeComponents();
        }

        // Constructor for edit mode
        public AddDictionaryLinkForm(string name, string url)
        {
            InitializeComponents();
            this.Text = "Edit Link";
            nameTextBox.Text = name;
            urlTextBox.Text = url;
        }

        private void InitializeComponents()
        {
            this.Text = "Add Link";
            this.Size = new Size(400, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            Label nameLabel = new Label();
            nameLabel.Text = "Name:";
            nameLabel.Location = new Point(10, 10);

            nameTextBox = new TextBox();
            nameTextBox.Location = new Point(10, 30);
            nameTextBox.Width = 360;
            nameTextBox.BackColor = this.BackColor;
            nameTextBox.ForeColor = Color.White;
            nameTextBox.BorderStyle = BorderStyle.FixedSingle;

            Label urlLabel = new Label();
            urlLabel.Text = "URL:";
            urlLabel.Location = new Point(10, 60);

            urlTextBox = new TextBox();
            urlTextBox.Location = new Point(10, 80);
            urlTextBox.Width = 360;
            urlTextBox.BackColor = this.BackColor;
            urlTextBox.ForeColor = Color.White;
            urlTextBox.BorderStyle = BorderStyle.FixedSingle;

            // Add a label to show an example URL format
            Label exampleUrlLabel = new Label();
            exampleUrlLabel.Text = "Example: https://www.google.com/search?q={word}";
            exampleUrlLabel.Location = new Point(10, 104);
            exampleUrlLabel.AutoSize = true;
            exampleUrlLabel.ForeColor = Color.Gray;

            okButton = new Button();
            okButton.Text = "OK";
            okButton.Location = new Point(220, 130); // Adjusted position
            okButton.DialogResult = DialogResult.OK;
            okButton.Click += OkButton_Click;
            okButton.ApplyCustomStyle();

            cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Location = new Point(300, 130); // Adjusted position
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.ApplyCustomStyle();


            this.Controls.Add(nameTextBox);
            this.Controls.Add(urlTextBox);
            this.Controls.Add(urlLabel);
            this.Controls.Add(nameLabel);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);
            this.Controls.Add(exampleUrlLabel); // Add the example label

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
            //this.Height = 210; // Adjusted height

            // Set initial focus to the nameTextBox
            this.Shown += (s, e) => nameTextBox.Focus();
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            // Validation: Check if both name and URL are filled
            if (string.IsNullOrWhiteSpace(nameTextBox.Text) || string.IsNullOrWhiteSpace(urlTextBox.Text))
            {
                MessageBox.Show("Please enter both a name and a URL for the link.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                this.DialogResult = DialogResult.None;
                return; // Don't close the form
            }

            // Validation: Check if the URL is in a valid format (optional)
            if (!Uri.TryCreate(urlTextBox.Text, UriKind.Absolute, out _))
            {
                MessageBox.Show("Please enter a valid URL.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                this.DialogResult = DialogResult.None;
                return; // Don't close the form
            }


            // Validation: Check if the URL is in a valid format and contains {word}
            if (!IsValidUrl(urlTextBox.Text))
            {
                MessageBox.Show("Please enter a valid URL that contains the '{word}' placeholder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                this.DialogResult = DialogResult.None;
                return; // Don't close the form
            }

            // If validation passes, set the properties and close the form
            DictionaryName = nameTextBox.Text;
            DictionaryUrl = urlTextBox.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }


        // Helper method to validate URL format and presence of {word}
        private bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _) && url.Contains("{word}");
        }


    }




    public class UpdateChecker
    {
        private string _githubRepoOwner;
        private string _githubRepoName;

        public UpdateChecker(string repoOwner, string repoName)
        {
            _githubRepoOwner = repoOwner;
            _githubRepoName = repoName;
        }

        public async void CheckForUpdates()
        {
            try
            {
                // Get current application version
                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                using (var client = new HttpClient())
                {
                    // GitHub API requires a user agent
                    client.DefaultRequestHeaders.Add("User-Agent", "UpdateChecker");

                    // Fetch latest release
                    var response = await client.GetStringAsync(
                        $"https://api.github.com/repos/{_githubRepoOwner}/{_githubRepoName}/releases/latest"
                    );

                    var latestRelease = JObject.Parse(response);
                    string latestVersionString = latestRelease["tag_name"]?.ToString().TrimStart('v');

                    // Parse version
                    Version latestVersion = new Version(latestVersionString);

                    // Compare versions
                    if (latestVersion > currentVersion)
                    {
                        DialogResult result = MessageBox.Show(
                            $"New version available! Current version: {currentVersion}\n" +
                            $"Latest version: {latestVersion}\n\n" +
                            "Do you want to download the update?",
                            "Update Available",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information
                        );

                        if (result == DialogResult.Yes)
                        {
                            // Open the release page
                            Process.Start(latestRelease["html_url"]?.ToString());
                        }
                    }
                    else if (latestVersion < currentVersion)
                    {
                        MessageBox.Show(
                            $"Current version: {currentVersion}\n" +
                            $"Latest version: {latestVersion}",
                            "No Updates Available - currentVersion > latestVersion",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                    else
                    {
                        MessageBox.Show(
                            "You are running the latest version of the application.",
                            "No Updates Available",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error checking for updates: {ex.Message}",
                    "Update Check Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }


        public async void CheckForUpdatesAtStartup()
        {
            try
            {
                // Get current application version
                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                using (var client = new HttpClient())
                {
                    // GitHub API requires a user agent
                    client.DefaultRequestHeaders.Add("User-Agent", "UpdateChecker");

                    // Fetch latest release
                    var response = await client.GetStringAsync(
                        $"https://api.github.com/repos/{_githubRepoOwner}/{_githubRepoName}/releases/latest"
                    );

                    var latestRelease = JObject.Parse(response);
                    string latestVersionString = latestRelease["tag_name"]?.ToString().TrimStart('v');

                    // Parse version
                    Version latestVersion = new Version(latestVersionString);

                    // Compare versions
                    if (latestVersion > currentVersion)
                    {
                        DialogResult result = MessageBox.Show(
                            $"New version available! Current version: {currentVersion}\n" +
                            $"Latest version: {latestVersion}\n\n" +
                            "Do you want to download the update?",
                            "Update Available",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information
                        );

                        if (result == DialogResult.Yes)
                        {
                            // Open the release page
                            Process.Start(latestRelease["html_url"]?.ToString());
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
        }


    }


    // This will be an inner class within your MainForm class
    class SubtitleSelectionForm : /*MyTemplateForm*/ Form
    {
        private List<ISubtitleStream> subtitleStreams; // Change type here
        private ComboBox subtitleTrackComboBox;
        private Button btnOK;
        private Button btnCancel;

        public ISubtitleStream SelectedSubtitleStream { get; private set; }

        public SubtitleSelectionForm(List<ISubtitleStream> streams) // Change type here
        {
            subtitleStreams = streams;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Select Subtitle Track";
            this.Size = new System.Drawing.Size(400, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            Label lblInstructions = new Label
            {
                Text = "Please select the subtitle track you want to use:",
                Location = new System.Drawing.Point(10, 10),
                Width = 380,
                Height = 30
            };

            subtitleTrackComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(10, 50),
                Width = 360,
                DropDownStyle = ComboBoxStyle.DropDownList
            };




            var subtitleInfo = subtitleStreams.Select((stream, index) =>
            {
                string titlePart = string.IsNullOrEmpty(stream.Title) ? "" : $" {stream.Title} -";
                //string languagePart = $"[{LanguageCodeConverter.ConvertLanguageCode(stream.Language ?? "Unknown")}]";
                string languagePart = $"[{LanguageMapper.GetLanguageName(stream.Language ?? "Unknown")}]";
                return $"Track {index + 1}:{titlePart} {languagePart}";
            })
                .ToArray();


            subtitleTrackComboBox.Items.AddRange(subtitleInfo);
            subtitleTrackComboBox.SelectedIndex = 0;








            btnOK = new Button
            {
                Text = "OK",
                Location = new System.Drawing.Point(220, 120),
                DialogResult = DialogResult.OK
            };
            btnOK.Click += BtnOK_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(300, 120),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.Add(lblInstructions);
            this.Controls.Add(subtitleTrackComboBox);
            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            SelectedSubtitleStream = subtitleStreams[subtitleTrackComboBox.SelectedIndex];
            DialogResult = DialogResult.OK;
            Close();
        }
    }








public class LanguageData
    {
        public string Id { get; set; }
        public string Part2B { get; set; }
        public string Part2T { get; set; }
        public string Part1 { get; set; }
        public string Scope { get; set; }
        public string LanguageType { get; set; }
        public string RefName { get; set; }
        public string Comment { get; set; }
        public string CleanedName { get; set; } // Add a property for the cleaned name

        public LanguageData(string[] parts)
        {
            Id = parts[0];
            Part2B = parts[1];
            Part2T = parts[2];
            Part1 = parts[3];
            Scope = parts[4];
            LanguageType = parts[5];
            RefName = parts[6];
            Comment = parts[7];

            CleanedName = CleanLanguageName(RefName); // Clean the name during object creation
        }

        private string CleanLanguageName(string name)
        {
            // 1. Remove parentheses and their content
            string cleanedName = Regex.Replace(name, @"\(.*?\)", "").Trim();

            //// 2. Remove "(macrolanguage)"
            //cleanedName = cleanedName.Replace("(macrolanguage)", "").Trim();

            //// 3. Handle commas (replace with space for simplicity in this example)
            //cleanedName = cleanedName.Replace(",", " ").Trim();

            // Remove any double spaces that might have been introduced
            cleanedName = Regex.Replace(cleanedName, @"\s+", " ");

            return cleanedName;
        }
    }

    public static class LanguageMapper
    {
        private static readonly Dictionary<string, LanguageData> LanguageDataMap = LoadLanguageData();

        private static Dictionary<string, LanguageData> LoadLanguageData()
        {
            var map = new Dictionary<string, LanguageData>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // Load the embedded resource
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "SuViPlayer.iso-639-3.tab"; // Replace with your resource name

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
                    }

                    using (var reader = new StreamReader(stream))
                    {
                        // Skip the header line
                        reader.ReadLine();

                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            var parts = line.Split('\t');

                            // Validate the line structure
                            if (parts.Length < 8)
                            {
                                Console.WriteLine($"Skipping malformed line (not enough columns): {line}");
                                continue;
                            }

                            // Use the constructor to create and clean the LanguageData object
                            var languageData = new LanguageData(parts);

                            // Add mappings for all valid ISO codes
                            if (!string.IsNullOrEmpty(languageData.Id)) map[languageData.Id] = languageData;
                            if (!string.IsNullOrEmpty(languageData.Part2B)) map[languageData.Part2B] = languageData;
                            if (!string.IsNullOrEmpty(languageData.Part2T)) map[languageData.Part2T] = languageData;
                            if (!string.IsNullOrEmpty(languageData.Part1)) map[languageData.Part1] = languageData;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading language map: {ex}");
                // Handle exceptions appropriately
            }

            return map;
        }

        public static string GetFullLanguageName(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return string.Empty;
            }

            // Return the uncleaned name
            return LanguageDataMap.TryGetValue(code, out var languageData) ? languageData.RefName : code;
        }

        public static string GetLanguageName(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return string.Empty;
            }

            // Return the cleaned name
            return LanguageDataMap.TryGetValue(code, out var languageData) ? languageData.CleanedName : code;
        }

        public static LanguageData GetLanguageData(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            LanguageDataMap.TryGetValue(code, out var languageData);
            return languageData;
        }
    }





    public class DarkModeRenderer : ToolStripProfessionalRenderer
    {
        public DarkModeRenderer(ColorTable colors) : base(colors) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var colorTable = (ColorTable)ColorTable;
            Rectangle rc = new Rectangle(Point.Empty, e.Item.Size);
            Color backColor = e.Item.Selected ? colorTable.MenuItemBackgroundColorSelected : colorTable.MenuItemBackgroundColor;
            using (SolidBrush brush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(brush, rc);
            }
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            var colorTable = (ColorTable)ColorTable;
            Rectangle rc = new Rectangle(Point.Empty, e.ToolStrip.Size);
            using (SolidBrush brush = new SolidBrush(colorTable.MenuBackgroundColor))
            {
                e.Graphics.FillRectangle(brush, rc);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            var colorTable = (ColorTable)ColorTable;
            Rectangle rc = new Rectangle(Point.Empty, e.Item.Size);
            using (SolidBrush brush = new SolidBrush(colorTable.SeparatorColor))
            {
                e.Graphics.FillRectangle(brush, rc);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            var colorTable = (ColorTable)ColorTable;
            e.TextColor = e.Item.Selected ? colorTable.MenuItemTextColorSelected : colorTable.MenuItemTextColor;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            var colorTable = (ColorTable)ColorTable;
            e.ArrowColor = colorTable.ArrowColor;
            base.OnRenderArrow(e);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            var colorTable = (ColorTable)ColorTable;
            Rectangle rc = e.AffectedBounds;
            using (SolidBrush brush = new SolidBrush(colorTable.ImageMarginBackgroundColor))
            {
                e.Graphics.FillRectangle(brush, rc);
            }
        }
    }

    public class ColorTable : ProfessionalColorTable
    {
        // General Colors
        public virtual Color MenuBackgroundColor { get; set; } = Color.FromArgb(32, 32, 32);
        public virtual Color MenuItemBackgroundColor { get; set; } = Color.FromArgb(32, 32, 32);


        //public virtual Color MenuItemBackgroundColorSelected { get; set; } = Color.FromArgb(0, 80, 200);
        public virtual Color MenuItemBackgroundColorSelected { get; set; } = Color.FromArgb(0, 128, 128);


        public virtual Color MenuItemTextColor { get; set; } = Color.White;
        public virtual Color MenuItemTextColorSelected { get; set; } = Color.White;
        public virtual Color SeparatorColor { get; set; } = Color.FromArgb(80, 80, 80);
        public virtual Color ArrowColor { get; set; } = Color.White;
        public virtual Color ImageMarginBackgroundColor { get; set; } = Color.FromArgb(32, 32, 32);

        // ProfessionalColorTable Overrides (for finer control)
        public override Color MenuItemSelected => MenuItemBackgroundColorSelected;
        public override Color ToolStripDropDownBackground => MenuBackgroundColor;
        public override Color ImageMarginGradientBegin => ImageMarginBackgroundColor;
        public override Color ImageMarginGradientMiddle => ImageMarginBackgroundColor;
        public override Color ImageMarginGradientEnd => ImageMarginBackgroundColor;
        public override Color MenuItemSelectedGradientBegin => MenuItemBackgroundColorSelected;
        public override Color MenuItemSelectedGradientEnd => MenuItemBackgroundColorSelected;
        public override Color MenuItemPressedGradientBegin => MenuItemBackgroundColor;
        public override Color MenuItemPressedGradientEnd => MenuItemBackgroundColor;
        public override Color MenuBorder => SeparatorColor;
        public override Color MenuItemBorder => SeparatorColor;
    }

    public class DarkModeColors : ColorTable
    {
        // You can customize specific colors for dark mode here
        // (if you want them different from the defaults)

    }

    public class LightModeColors : ColorTable
    {
        public override Color MenuBackgroundColor { get; set; } = SystemColors.Control;
        public override Color MenuItemBackgroundColor { get; set; } = SystemColors.Control;
        public override Color MenuItemBackgroundColorSelected { get; set; } = SystemColors.Highlight;
        public override Color MenuItemTextColor { get; set; } = SystemColors.ControlText;
        public override Color MenuItemTextColorSelected { get; set; } = SystemColors.HighlightText;
        public override Color SeparatorColor { get; set; } = SystemColors.GrayText;
        public override Color ArrowColor { get; set; } = SystemColors.ControlText;
        public override Color ImageMarginBackgroundColor { get; set; } = SystemColors.Control;
    }







public class SubtitleCleaner
    {
        public class SubtitleEntry
        {
            public int Index { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
            public List<string> Lines { get; set; } = new List<string>();
        }

        public static void CleanSrtFileInPlace(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The specified SRT file was not found.", filePath);
            }

            var subtitles = ParseAndCleanSRT(filePath);

            // Merge subtitles with the same time range
            subtitles = MergeSubtitles(subtitles);

            // Re-index the subtitles
            subtitles = ReIndexSubtitles(subtitles);

            // Write the cleaned, merged, and re-indexed subtitles back to the file
            WriteSrtFile(filePath, subtitles);
        }

        private static List<SubtitleEntry> ParseAndCleanSRT(string filePath)
        {
            var subtitles = new List<SubtitleEntry>();
            var fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            // Normalize line endings
            fileContent = fileContent.Replace("\r\n", "\n").Replace("\r", "\n");
            var blocks = fileContent.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var block in blocks)
            {
                try
                {
                    var lines = block.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
                    if (lines.Count < 3) continue;

                    // Parse index (but we'll re-index later)
                    if (!int.TryParse(lines[0].Trim(), out int index))
                        continue;

                    // Parse timestamp line
                    var timestamps = lines[1].Split(new[] { " --> " }, StringSplitOptions.None);
                    if (timestamps.Length != 2)
                        continue;

                    if (!TryParseTimeStamp(timestamps[0].Trim(), out TimeSpan startTime) ||
                        !TryParseTimeStamp(timestamps[1].Trim(), out TimeSpan endTime))
                        continue;

                    // Parse and clean text lines
                    var textLines = lines.Skip(2).Select(RemoveTags).Where(line => !string.IsNullOrEmpty(line)).ToList();

                    if (textLines.Any())
                    {
                        subtitles.Add(new SubtitleEntry
                        {
                            Index = index, // Store the original index temporarily
                            StartTime = startTime,
                            EndTime = endTime,
                            Lines = textLines
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing subtitle block: {ex.Message}");
                    continue;
                }
            }

            return subtitles;
        }

        private static List<SubtitleEntry> MergeSubtitles(List<SubtitleEntry> subtitles)
        {
            if (subtitles == null || subtitles.Count == 0)
            {
                return new List<SubtitleEntry>();
            }

            var mergedSubtitles = new List<SubtitleEntry>();
            SubtitleEntry previous = null;

            foreach (var current in subtitles)
            {
                if (previous == null)
                {
                    previous = current;
                    continue;
                }

                if (previous.StartTime == current.StartTime && previous.EndTime == current.EndTime)
                {
                    // Merge lines
                    previous.Lines.AddRange(current.Lines);
                }
                else
                {
                    mergedSubtitles.Add(previous);
                    previous = current;
                }
            }

            mergedSubtitles.Add(previous);
            return mergedSubtitles;
        }

        private static List<SubtitleEntry> ReIndexSubtitles(List<SubtitleEntry> subtitles)
        {
            for (int i = 0; i < subtitles.Count; i++)
            {
                subtitles[i].Index = i + 1;
            }
            return subtitles;
        }

        private static void WriteSrtFile(string filePath, List<SubtitleEntry> subtitles)
        {
            var sb = new StringBuilder();
            foreach (var subtitle in subtitles)
            {
                sb.AppendLine(subtitle.Index.ToString());
                sb.AppendLine($"{subtitle.StartTime:hh\\:mm\\:ss\\,fff} --> {subtitle.EndTime:hh\\:mm\\:ss\\,fff}");
                foreach (var line in subtitle.Lines)
                {
                    sb.AppendLine(line);
                }
                sb.AppendLine();
            }

            // Write to file using UTF-8 *without* BOM
            File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(false)); // false means no BOM
        }

        private static bool TryParseTimeStamp(string timestamp, out TimeSpan result)
        {
            // Same implementation as before
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

        private static string RemoveTags(string input)
        {
            // Same implementation as before
            return Regex.Replace(Regex.Replace(input, "<.*?>", string.Empty), @"\{\\.*?\}", string.Empty).Trim();
        }
    }











}