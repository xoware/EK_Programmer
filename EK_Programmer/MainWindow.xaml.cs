using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Management;
using System.Net;
using System.Collections.Specialized;
using System.Net.Security;
using System.Security.Cryptography;


namespace EK_Programmer
{

    public enum ProgrammingState : int
    {
        ProgrammingState_Init = 0,
        ProgrammingState_Blank_Detected,
        ProgrammingState_LoadingFW,
        ProgrammingState_UnplugNeeded,
        ProgrammingState_Wait_For_Boot,
        ProgrammingState_Good_FW_Detected,
        ProgrammingState_Error_Programming,
        ProgrammingState_Error_Already_Done,
        ProgrammingState_Error,
        ProgrammingState_Last,
    }



    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ExoKey ek = null;
        ProgrammingState Programming_State = ProgrammingState.ProgrammingState_Init;
        private readonly BackgroundWorker state_machine_worker = new BackgroundWorker();
        private readonly BackgroundWorker samba_worker = new BackgroundWorker();
        bool keep_running = true;
        String Samba_Errors;
        String Samba_Log;

//        System.Windows.Shapes.Ellipse Blank_Device_Detected_Elipse = null;

        public MainWindow()
        {
            InitializeComponent();
            ek = new ExoKey();
            this.DataContext = ek;  // EK is the datasource
            Loaded += MainWindowLoaded;
            Blank_USB_Detected_Circle.Fill = new SolidColorBrush(Colors.Red);
            Closing += MainWindow_Closing;

        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            keep_running = false;
             
            for (int i = 0; i < 20 && (samba_worker.IsBusy || state_machine_worker.IsBusy); i++) {
                System.Threading.Thread.Sleep(100);
            }
        }

        static void Send_Log_Msg(String Msg)
        {
            Console.WriteLine(Msg);
        }

        private void MainWindowLoaded(object sender, RoutedEventArgs e)
        {/*
            try
            {
                Blank_Device_Detected_Elipse = (System.Windows.Shapes.Ellipse)Application.Current.FindResource("Blank_USB_Detected_Circle");
            }
            catch
            {
                MessageBox.Show("Resource not found.");
            }*/
            ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(bypass_all_cert_security);

            state_machine_worker.DoWork += state_machine_worker_DoWork;
            state_machine_worker.RunWorkerCompleted += state_machine_worker_RunWorkerCompleted;
            state_machine_worker.RunWorkerAsync();
            samba_worker.DoWork += samba_worker_DoWork;
            samba_worker.RunWorkerCompleted += samba_worker_RunWorkerCompleted;
        }
        private static bool bypass_all_cert_security(object sender,
            System.Security.Cryptography.X509Certificates.X509Certificate cert,
            System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors error)
        {
            return true;
        }
        void Post_Log_To_Server(String Status_Code, String Mesage, String Samba_Log )
        {
            try
            {
                using (WebClient client = new WebClient())
                {

                    NameValueCollection vals = new NameValueCollection();
                    vals.Add("status", Status_Code);
                    vals.Add("message", Mesage);
                    vals.Add("samba_log", Samba_Log);


                    client.UploadValues("https://updates.vpex.org/ek/production/log_ek.cgi", vals);
                }
            }
            catch (Exception e)
            {
                Send_Log_Msg(String.Format("Post_Log_To_Server Exception {0} Trace {1}", e.Message, e.StackTrace));
            }
        }

        void samba_worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            System.Media.SystemSounds.Beep.Play();
            System.Media.SystemSounds.Question.Play();

            if (Samba_Errors.Length > 3)
            {
                MessageBox.Show("Error Programming: \r\n" + Samba_Errors);
                Programming_State = ProgrammingState.ProgrammingState_Error_Programming;
                Post_Log_To_Server("Error_Programming", Samba_Errors, Samba_Log);
            }
        }

        void samba_worker_DoWork(object sender, DoWorkEventArgs e)
        {
//            System.Threading.Thread.Sleep(5555);
            Run_Samba_Cmd();
            Programming_State = ProgrammingState.ProgrammingState_UnplugNeeded;
        }
        void Update_LEDs()
        {
            switch (Programming_State)
            {
                case ProgrammingState.ProgrammingState_Init:
                    Blank_USB_Detected_Circle.Fill = Brushes.Red;
                    Loading_Firmware_Circle.Fill = Brushes.Red;
                    USB_Replug_Circle.Fill = Brushes.Red;
                    EK_Detected_Circle.Fill = Brushes.Red;
                    break;
                case ProgrammingState.ProgrammingState_Blank_Detected:
                    Blank_USB_Detected_Circle.Fill = Brushes.Green;
                    break;
                case ProgrammingState.ProgrammingState_LoadingFW:

                    // Flash
                    if (Loading_Firmware_Circle.Fill.Equals(Brushes.Yellow))
                        Loading_Firmware_Circle.Fill = Brushes.Transparent;
                    else
                        Loading_Firmware_Circle.Fill = Brushes.Yellow;
                    break;
                case ProgrammingState.ProgrammingState_UnplugNeeded:
                    Loading_Firmware_Circle.Fill = Brushes.Green;
                    if (USB_Replug_Circle.Fill.Equals(Brushes.Yellow))
                        USB_Replug_Circle.Fill = Brushes.Transparent;
                    else
                    {
                        USB_Replug_Circle.Fill = Brushes.Yellow;
                        System.Media.SystemSounds.Beep.Play();
                    }
                    break;
                case ProgrammingState.ProgrammingState_Wait_For_Boot:
                    USB_Replug_Circle.Fill = Brushes.Green;

                    if (EK_Detected_Circle.Fill.Equals(Brushes.Yellow))
                        EK_Detected_Circle.Fill = Brushes.Transparent;
                    else
                        EK_Detected_Circle.Fill = Brushes.Yellow;
                    break;
                case ProgrammingState.ProgrammingState_Good_FW_Detected:
                    EK_Detected_Circle.Fill = Brushes.Green;
                    break;
                case ProgrammingState.ProgrammingState_Error_Already_Done:
                    Blank_USB_Detected_Circle.Fill = Brushes.Red;
                    Loading_Firmware_Circle.Fill = Brushes.Red;
                    USB_Replug_Circle.Fill = Brushes.Red;
                    EK_Detected_Circle.Fill = Brushes.Green;
                    break;
                case ProgrammingState.ProgrammingState_Error_Programming:
                    Blank_USB_Detected_Circle.Fill = Brushes.Red;
                    Loading_Firmware_Circle.Fill = Brushes.Red;
                    USB_Replug_Circle.Fill = Brushes.Red;
                    break;

            }
        }

        void state_machine_worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            if (keep_running)
            {
                Update_LEDs();
                state_machine_worker.RunWorkerAsync();
            }
        }

        void state_machine_worker_DoWork(object sender, DoWorkEventArgs e)
        {
            switch(Programming_State)
            {
                case ProgrammingState.ProgrammingState_Init:
                    Search_USB_Devices();
                    break;
                case ProgrammingState.ProgrammingState_Blank_Detected:
                    Programming_State = ProgrammingState.ProgrammingState_LoadingFW;
                    samba_worker.RunWorkerAsync();
                    break;
                case ProgrammingState.ProgrammingState_LoadingFW:
                    // do nothing here wait for samba to finish
                    break;
                case ProgrammingState.ProgrammingState_UnplugNeeded:
                    // Wait for user to uplug
                    Search_USB_Devices();
                    break;
                case ProgrammingState.ProgrammingState_Wait_For_Boot:
                    // Wait for bootup
                    Search_USB_Devices();
                    break;
                case ProgrammingState.ProgrammingState_Good_FW_Detected:
                    // Wait for bootup
                    Search_USB_Devices();
                    break;
                case ProgrammingState.ProgrammingState_Error_Already_Done:
                case ProgrammingState.ProgrammingState_Error_Programming:
                    Programming_State = ProgrammingState.ProgrammingState_Init;
                    System.Threading.Thread.Sleep(250);
                    break;
            }

            System.Threading.Thread.Sleep(250);
        }


        // parse something like // "\\KARL-PC\root\cimv2:Win32_PnPEntity.DeviceID="USB\\VID_29B7&PID_0101\\123"
        private void Check_Dev_Desc(String id_str)
        {
            string Device_ID_Str = "DeviceID=";
            int pos = id_str.LastIndexOf(Device_ID_Str);

            if (pos < 0)
                return;

            pos += Device_ID_Str.Length;
            String ID_Value = id_str.Substring(pos);
            ID_Value = ID_Value.Replace("\"", "");

            ManagementObjectCollection collection;
            using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_PnPSignedDriver Where DeviceID = '" + ID_Value + "'")) // Win32_USBDevice Win32_USBHub
                collection = searcher.Get();

            foreach (var entity in collection)
            {
                Send_Log_Msg("entity=" + entity.ToString());
            }
            try
            {
                string ComputerName = "localhost";
                ManagementScope Scope;
                Scope = new ManagementScope(String.Format("\\\\{0}\\root\\CIMV2", ComputerName), null);

                Scope.Connect();
                ObjectQuery Query = new ObjectQuery("SELECT * FROM Win32_PnPSignedDriver Where DeviceID = '" + ID_Value + "'");
                ManagementObjectSearcher Searcher = new ManagementObjectSearcher(Scope, Query);

                foreach (ManagementObject WmiObject in Searcher.Get())
                {
                    Send_Log_Msg(String.Format("{0,-35} {1,-40}", "ClassGuid", WmiObject["ClassGuid"]));// String
                    Send_Log_Msg(String.Format("{0,-35} {1,-40}", "DeviceClass", WmiObject["DeviceClass"]));// String
                    Send_Log_Msg(String.Format("{0,-35} {1,-40}", "DeviceID", WmiObject["DeviceID"]));// String
                    Send_Log_Msg(String.Format("{0,-35} {1,-40}", "DeviceName", WmiObject["DeviceName"]));// String
                    Send_Log_Msg(String.Format("{0,-35} {1,-40}", "Manufacturer", WmiObject["Manufacturer"]));// String
                    Send_Log_Msg(String.Format("{0,-35} {1,-40}", "Name", WmiObject["Name"]));// String
                    Send_Log_Msg(String.Format("{0,-35} {1,-40}", "Status", WmiObject["Status"]));// String
                    Send_Log_Msg(String.Format("{0,-35} {1,-40}", "DriverName", WmiObject["DriverName"]));// String
                    Send_Log_Msg(String.Format("{0,-35} {1,-40}", "DriverVersion", WmiObject["DriverVersion"]));// String
                    Send_Log_Msg(String.Format("{0,-35} {1,-40}", "FriendlyName", WmiObject["FriendlyName"]));// String
                    Send_Log_Msg(String.Format("{0,-35} {1,-40}", "Started", WmiObject["Started"]));// String
                    /*
                    if (WmiObject["DeviceName"] != null && WmiObject["DriverVersion"] != null
                       && WmiObject["DeviceName"].ToString().Length > 4 && WmiObject["DriverVersion"].ToString().Length > 3)
                        ExoKey_Driver_Found = true;*/
                }
            }
            catch (Exception e)
            {
                Send_Log_Msg(String.Format("Exception {0} Trace {1}", e.Message, e.StackTrace));
            }
        }

        private void Search_USB_Devices()
        {
            bool Blank_EK_Found = false;
            bool Programmed_EK_Found = false;
            ManagementObjectCollection collection;
            using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_USBControllerDevice")) // Win32_USBDevice Win32_USBHub
                collection = searcher.Get();

            foreach (var device in collection)
            {

                //                Send_Log_Msg(device.ToString());
                PropertyDataCollection properties = device.Properties;
                foreach (PropertyData property in properties)
                {
                    Send_Log_Msg("Name=" + property.Name + " Value = " +
                        (property.Value == null ? "null" : property.Value.ToString()));

                    if (property.Value != null)
                    {

                        // If ExoKey ID
                        if (property.Value.ToString().Contains("VID_29B7&PID_0101"))
                        {
                            Programmed_EK_Found = true;

                        }
                        if (property.Value.ToString().Contains("VID_03EB&PID_6124"))
                        {
                            Blank_EK_Found = true;
                            Check_Dev_Desc(property.Value.ToString());
                        }
                    }

                }

            }
            collection.Dispose();

            if (Programming_State == ProgrammingState.ProgrammingState_Wait_For_Boot && Programmed_EK_Found)
            {
                Programming_State = ProgrammingState.ProgrammingState_Good_FW_Detected;
                Post_Log_To_Server("OK", "Good FW Detected", Samba_Log);
            }
            else if (Programming_State == ProgrammingState.ProgrammingState_Init)
            {
                if (Blank_EK_Found)
                    Programming_State = ProgrammingState.ProgrammingState_Blank_Detected;
                else if (Programmed_EK_Found)
                    Programming_State = ProgrammingState.ProgrammingState_Error_Already_Done;
            }
            else if (Programming_State == ProgrammingState.ProgrammingState_UnplugNeeded && !Blank_EK_Found)
            {
                Programming_State = ProgrammingState.ProgrammingState_Wait_For_Boot;
            }
            else if (Programming_State == ProgrammingState.ProgrammingState_Good_FW_Detected && !Blank_EK_Found && !Programmed_EK_Found)
            {
                // we finished programming, then removed o back to init
                Programming_State = ProgrammingState.ProgrammingState_Init;
            }/*
            else if (Programming_State == ProgrammingState.ProgrammingState_Error_Already_Done && !Blank_EK_Found && !Programmed_EK_Found)
            {
                // we finished programming, then removed o back to init
                Programming_State = ProgrammingState.ProgrammingState_Init;
            }*/
        }
            
        private void Reset_Button_Click(object sender, RoutedEventArgs e)
        {
            Programming_State = ProgrammingState.ProgrammingState_Init;
        }
        private void Run_Samba_Cmd()
        {
            System.Diagnostics.Process proc;
            StringBuilder std_out = new StringBuilder();
            proc = new System.Diagnostics.Process();
            string samba_Path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            proc.StartInfo.FileName = samba_Path + "\\sam-ba.exe ";
            proc.StartInfo.Arguments = "\\usb\\ARM0 at91sama5d3x-ek exokey_main.tcl";
            Send_Log_Msg("Run_Cmd" + proc.StartInfo.FileName );
            Send_Log_Msg("Run_Cmd" + proc.StartInfo.FileName );
            Samba_Errors = ""; // 
            Samba_Log = DateTime.Now.ToString() + " \r\n";
            // Set UseShellExecute to false for redirection.
            proc.StartInfo.UseShellExecute = false;

            // Redirect the standard output of the sort command.   
            // This stream is read asynchronously using an event handler.
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.CreateNoWindow = true;
            //Output = new StringBuilder("");

            // Set our event handler to asynchronously read the sort output.
         //   proc.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(SambaOutputHandler);

            // Redirect standard input as well.  This stream 
            // is used synchronously.
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardError = true;

            // Start the process.
            proc.Start();

            // Start the asynchronous read of the sort output stream.
          //  proc.BeginOutputReadLine();
        //    string error = proc.StandardError.ReadToEnd()

            while (!proc.HasExited)
            {
                std_out.Append(proc.StandardOutput.ReadToEnd());
            }

            if (!proc.WaitForExit(123456))
            {
                Programming_State = ProgrammingState.ProgrammingState_Error_Programming;
                Post_Log_To_Server("Error_Programming", "Timed out", Samba_Log);
            }
            proc.Close();
            Send_Log_Msg("Done Programming");

            Samba_Log = std_out.ToString();

            foreach (var line in Samba_Log.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Contains("-E- "))
                {
                    Samba_Errors += line + "\r\n";
                }
            }
              
        }

        private void SambaOutputHandler(object sendingProcess, System.Diagnostics.DataReceivedEventArgs outLine)
        {
            // Collect the command output.

            if (!String.IsNullOrEmpty(outLine.Data))
            {
                // numOutputLines++;

                // Add the text to the collected output.
                //   Output.Append(Environment.NewLine +  "[" + numOutputLines.ToString() + "] - " + outLine.Data);
                Send_Log_Msg("samba Output:" + outLine.Data);
                Samba_Log +=   outLine.Data + "\r\n";
                if (outLine.Data.Contains("-E- "))
                {
                    Samba_Errors += outLine.Data + "\r\n";
                }
            }
        }
    }
}
