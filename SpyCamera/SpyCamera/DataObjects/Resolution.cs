using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpyCamera.DataObjects
{
    public class Resolution : BaseDataObject
    {
        public Resolution(Windows.Foundation.Size resolution)
        {
            CameraResolution = resolution;
            Display = resolution.Height + " x " + resolution.Width;
        }
        
        private string _display;
        public string Display
        {
            get { return _display; }
            set { _display = value; base.RaisePropertyChanged("Display"); }
        }
        

        private Windows.Foundation.Size _cameraResolution;
        public Windows.Foundation.Size CameraResolution
        {
            get { return _cameraResolution; }
            set { _cameraResolution = value; base.RaisePropertyChanged("CameraResolution"); }
        }
        
    }
}
