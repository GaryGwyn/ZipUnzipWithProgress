using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ZipUtil.ZipUtil;

namespace ZipTest
{
    class Test
    {
        //Output folder:
        private static string currentDesktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        //Small test file:
        private static string fileToZip = @"C:\Windows\System32\drivers\etc\hosts";

        //Big test file:
        private static string bigFile = @"C:\Windows\System32\shell32.dll"; //20MB file
       
        private static double _fileProgress { get; set; }
        
        //Bound property for updating progress:
        private static double FileProgress
        {
            get { return _fileProgress; }
            set
            {
                if (_fileProgress >= 0 && _fileProgress < 100)
                {
                    _fileProgress = value;
                    Console.WriteLine($"Progress:  {_fileProgress}%");
                }
                else
                    _fileProgress = 0;
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Zipping hosts file to desktop....");
            ZipTest();
            Console.WriteLine("Done....");


            Console.WriteLine("Press a key to unzip hosts file to desktop....");
            Console.ReadKey();
            Console.WriteLine(Environment.NewLine);
            UnZipTest();
            Console.WriteLine("Done....");


            Console.WriteLine("Press a key to zip a big file....");
            Console.ReadKey();
            Console.WriteLine(Environment.NewLine);
            BigZipTest();

            Console.ReadKey();

        }

        /// <summary>
        /// Fetch the hosts file in Windows, zip it, and put on current user's desktop
        /// </summary>
        private static async void ZipTest()
        {
            await Zip($"{currentDesktop}\\Hosts.zip", fileToZip);
        }

        /// <summary>
        /// Unzip the Hosts.zip file and extract contents to the current user's desktop
        /// </summary>
        private static async void UnZipTest()
        {
            await UnZip($"{currentDesktop}\\Hosts.zip", currentDesktop);
        }

        /// <summary>
        /// Zip the big test file to the current user's desktop and show incremental progress
        /// </summary>
        private static async void BigZipTest()
        {
            var progress = new Progress<double>(f => FileProgress = f);
            Console.WriteLine("Zipping big file to desktop....");
            await Zip($"{currentDesktop}\\BigFile.zip", bigFile, progress);
            Console.WriteLine("Done....");
            Console.WriteLine("Press a key to quit....");
        }

    }
}
