using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace SuViPlayer
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        //[STAThread]
        //static void Main()
        //{
        //    Application.EnableVisualStyles();
        //    Application.SetCompatibleTextRenderingDefault(false);
        //    Application.Run(new MainForm());
        //}

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            MainForm mainForm = new MainForm();


            //// If a file was opened with the program, handle it
            //if (args.Length > 0 && File.Exists(args[0]))
            //{
            //    mainForm.OpenVideoFile(args[0]);
            //}

            // Store the file path if provided as an argument
            string videoFilePath = null;
            if (args.Length > 0 && File.Exists(args[0]))
            {
                videoFilePath = args[0];
            }

            // Subscribe to the Shown event
            mainForm.Shown += (sender, e) =>
            {
                // Open the video file after the form is shown
                if (!string.IsNullOrEmpty(videoFilePath))
                {
                    mainForm.OpenVideoFile(videoFilePath);
                }
            };

            Application.Run(mainForm);


        }
    }
}
