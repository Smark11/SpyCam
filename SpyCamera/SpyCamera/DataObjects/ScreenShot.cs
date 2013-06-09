using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace SpyCamera.DataObjects
{
    public class ScreenShot : BaseDataObject
    {
        public ScreenShot(string name, Uri uri, Image imageToDisplay)
        {
            Name = name;
            Uri = uri;
            ImageToDisplay = imageToDisplay;
            SetFormattedName();
        }

        private void SetFormattedName()
        {
            try
            {
                if (Name != null)
                {
                    if (Name != string.Empty)
                    {
                        if (Name.ToUpper().StartsWith("WP_SS_"))
                        {
                            FormattedName = Name.Substring(6, Name.Length - 6);
                        }
                        else
                        {
                            FormattedName = Name;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FormattedName = Name;
            }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; base.RaisePropertyChanged("Name"); }
        }

        private string _formattedName;
        public string FormattedName
        {
            get { return _formattedName; }
            set { _formattedName = value; base.RaisePropertyChanged("FormattedName"); }
        }
        

        private Uri _uri;
        public Uri Uri
        {
            get { return _uri; }
            set { _uri = value; base.RaisePropertyChanged("Uri"); }
        }

        private Image _imageToDisplay;
        public Image ImageToDisplay
        {
            get { return _imageToDisplay; }
            set { _imageToDisplay = value; base.RaisePropertyChanged("ImageToDisplay"); }
        }
    }
}
