using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace EK_Programmer
{

    public enum ModelState
    {
        Fectching,  // The model is fetching data
        Invalid,    // The model is in an invalid state
        Active      // The model has fetched its data
    }
    public class ExoKey //: INotifyPropertyChanged
    {
      //  public event PropertyChangedEventHandler PropertyChanged;
        System.Windows.Shapes.Ellipse Blank_Device_Detected_Elipse = null;
        public ExoKey()
        {

            // Create a new binding 
            // TheDate is a property of type DateTime on MyData class
            //Binding myNewBindDef = new Binding("TheDate");
         //   _dispatcher = Dispatcher.CurrentDispatcher;
           // Blank_Device_Detected_Elipse = (System.Windows.Shapes.Ellipse) Application.FindResource("ExoKeyNotifyIcon");
        }

        /*
        [Conditional("Debug")]
        protected void VerifyCalledOnUIThread()
        {
            Debug.Assert(Dispatcher.CurrentDispatcher == this.Dispatcher,
                "Call must be made on UI thread.");
        }
         */
    }
}
