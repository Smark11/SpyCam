using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using SpyCamera.Resources;
using Common;
using System.ComponentModel;
using Microsoft.Xna.Framework.Media;
using Windows.Phone.Media.Capture;
using System.IO;
using System.Diagnostics;
using SpyCamera.DataObjects;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework.Media.PhoneExtensions;
using Microsoft.Phone;
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;
using Windows.ApplicationModel.Store;
using Microsoft.Phone.Tasks;
using System.Windows.Media;
using Common.Licencing;

namespace SpyCamera
{
    public partial class MainPage : PhoneApplicationPage, INotifyPropertyChanged, IDisposable
    {
        public static MainPage _mainPageInstance;
        bool _trialExpired = false;
        MarketplaceDetailTask _marketPlaceDetailTask = new MarketplaceDetailTask();
        MarketplaceReviewTask _review = new MarketplaceReviewTask();
        bool _isFirstTimeOpening;

        public MainPage()
        {
            BuildLocalizedApplicationBar();
            _mainPageInstance = this;
            CenterX = 0;
            CenterY = 0;
            
            try
            {
                InitializeComponent();
                _isFirstTimeOpening = IsFirstTimeOpeningApplication();
                AvailableResolutions = new ObservableCollection<Resolution>();
                ScreenShots = new ObservableCollection<ScreenShot>();
                DataContext = this;
                NoScreenShotVisibility = Visibility.Collapsed;
                LoadShowRate();

                ScreenWidth = Convert.ToInt32(Application.Current.Host.Content.ActualWidth);


                if ((Application.Current as App).IsTrial)
                {
                    SaveStartDateOfTrial();

                    if (IsTrialExpired())
                    {
                        MessageBox.Show("Your trial has expired.  Please purchase this application.");
                        _trialExpired = true;
                        _marketPlaceDetailTask.Show();
                    }
                    else
                    {
                        MessageBox.Show("You have " + GetDaysLeftInTrial() + " days remaining in your trial.");
                    }
                }

                InitializeEverything();

            }
            catch (Exception ex)
            {

            }
        }

        // Sample code for building a localized ApplicationBar
        private void BuildLocalizedApplicationBar()
        {
            // Set the page's ApplicationBar to a new instance of ApplicationBar.
            ApplicationBar = new ApplicationBar();

            // Create a new menu item with the localized string from AppResources.
            ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.MainScreen);
            appBarMenuItem.Click += MainScreenHandler;
            ApplicationBar.MenuItems.Add(appBarMenuItem);

            // Create a new menu item with the localized string from AppResources.
            ApplicationBarMenuItem appBarMenuItem2 = new ApplicationBarMenuItem(AppResources.Configuration);
            appBarMenuItem2.Click += ConfigurationHandler;
            ApplicationBar.MenuItems.Add(appBarMenuItem2);

            ApplicationBarMenuItem appBarMenuItem3 = new ApplicationBarMenuItem(AppResources.Instructions);
            appBarMenuItem3.Click += InstructionsHandler;
            ApplicationBar.MenuItems.Add(appBarMenuItem3);

            ApplicationBarMenuItem appBarMenuItem4 = new ApplicationBarMenuItem(AppResources.About);
            appBarMenuItem4.Click += AboutClicked;
            ApplicationBar.MenuItems.Add(appBarMenuItem4);

            ApplicationBar.Mode = ApplicationBarMode.Minimized;
        }

        private void InitializeEverything()
        {
            if (!_trialExpired)
            {
                GetScreenShots();

                InitializeDisplay();

                if (_isFirstTimeOpening)
                {
                    GoToScreen(Screen.Instructions);
                    _isFirstTimeOpening = false;
                }
                else if ((Application.Current as App).IsTrial)
                {
                    GoToScreen(Screen.BuyOrRate);
                    _rateOrBuyScreenSelected = true;
                }
                else
                {
                    GoToScreen(Screen.Main);
                }

                ToggleCameraLocationVisibility();
                LoadSettings();
            }
            else
            {
                GoToScreen(Screen.Trial);
            }
        }

        private bool IsTrialExpired()
        {
            bool trialExpired = false;

            trialExpired = Trial.IsTrialExpired();

            return trialExpired;
        }

        public void GetScreenShots()
        {
            try
            {
                PictureAlbumCollection allAlbums = _mediaLibrary.RootPictureAlbum.Albums;
                ScreenShots.Clear();

                foreach (var album in allAlbums)
                {
                    if (album.Name.ToUpper().Contains("SCREENSHOT"))
                    {
                        PictureCollection screenShots = album.Pictures;

                        foreach (var picture in screenShots)
                        {
                            Stream picToDisplay = picture.GetImage();

                            BitmapImage bmImage = new BitmapImage();
                            bmImage.SetSource(picToDisplay);

                            var imageToShow = new Image
                            {
                                Source = PictureDecoder.DecodeJpeg(picToDisplay, picture.Width, picture.Height)
                            };


                            ScreenShots.Add(new ScreenShot(picture.Name, new Uri(MediaLibraryExtensions.GetPath(picture), UriKind.Absolute), imageToShow));
                        }
                    }
                }

                if (ScreenShots.Count() >= 1)
                {
                    SelectedScreenShot = ScreenShots[ScreenShots.Count - 1];
                }
                else
                {
                    NoScreenShotVisibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void ToggleCameraLocationVisibility()
        {
            if (PhotoCaptureDevice.AvailableSensorLocations.Count() > 1)
            {
                CameraLocationVisibility = Visibility.Visible;
            }
            else
            {
                CameraLocationVisibility = Visibility.Collapsed;
            }
        }

        private void LoadResolutions(CameraPosition cameraPosition)
        {
            IReadOnlyList<Windows.Foundation.Size> availableResolutions;

            AvailableResolutions.Clear();

            if (cameraPosition == CameraPosition.Back)
            {
                availableResolutions = PhotoCaptureDevice.GetAvailableCaptureResolutions(CameraSensorLocation.Back);
            }
            else
            {
                availableResolutions = PhotoCaptureDevice.GetAvailableCaptureResolutions(CameraSensorLocation.Front);
            }

            if (availableResolutions != null)
            {
                foreach (var resolution in availableResolutions)
                {
                    AvailableResolutions.Add(new Resolution(resolution));
                }
            }

            if (AvailableResolutions != null)
            {
                if (AvailableResolutions.Count > 0)
                {
                    SelectedResolution = AvailableResolutions[AvailableResolutions.Count() - 1];
                }
            }

            OpenCameraDevice(cameraPosition);
        }

        private async void OpenCameraDevice(CameraPosition position)
        {
            try
            {
                if (_cameraCaptureDevice != null)
                {
                    _cameraCaptureDevice.Dispose();
                }

                if (position == CameraPosition.Back)
                {
                    _cameraCaptureDevice = await PhotoCaptureDevice.OpenAsync(CameraSensorLocation.Back, SelectedResolution.CameraResolution);
                }
                else
                {
                    _cameraCaptureDevice = await PhotoCaptureDevice.OpenAsync(CameraSensorLocation.Front, SelectedResolution.CameraResolution);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void InitializeDisplay()
        {
            AboutVisibility = System.Windows.Visibility.Collapsed;
        }

        public void TogglePictureCounterVisibility(bool on)
        {
            if (on)
            {
                PictureCountVisibility = System.Windows.Visibility.Visible;
            }
            else
            {
                PictureCountVisibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void GoToScreen(Screen screen)
        {
            if (screen == Screen.About)
            {
                ConfigScreenVisibility = System.Windows.Visibility.Collapsed;
                MainScreenVisibility = System.Windows.Visibility.Collapsed;
                AboutVisibility = System.Windows.Visibility.Visible;
                TrialExpiredVisibility = System.Windows.Visibility.Collapsed;
                InstructionsVisibility = System.Windows.Visibility.Collapsed;
                BuyOrRateVisibility = System.Windows.Visibility.Collapsed;
            }
            else if (screen == Screen.Main)
            {
                ConfigScreenVisibility = System.Windows.Visibility.Collapsed;
                MainScreenVisibility = System.Windows.Visibility.Visible;
                AboutVisibility = System.Windows.Visibility.Collapsed;
                TrialExpiredVisibility = System.Windows.Visibility.Collapsed;
                InstructionsVisibility = System.Windows.Visibility.Collapsed;
                BuyOrRateVisibility = System.Windows.Visibility.Collapsed;
            }
            else if (screen == Screen.Configuration)
            {
                ConfigScreenVisibility = System.Windows.Visibility.Visible;
                MainScreenVisibility = System.Windows.Visibility.Collapsed;
                AboutVisibility = System.Windows.Visibility.Collapsed;
                TrialExpiredVisibility = System.Windows.Visibility.Collapsed;
                InstructionsVisibility = System.Windows.Visibility.Collapsed;
                BuyOrRateVisibility = System.Windows.Visibility.Collapsed;
            }
            else if (screen == Screen.Trial)
            {
                ConfigScreenVisibility = System.Windows.Visibility.Collapsed;
                MainScreenVisibility = System.Windows.Visibility.Collapsed;
                AboutVisibility = System.Windows.Visibility.Collapsed;
                TrialExpiredVisibility = System.Windows.Visibility.Visible;
                InstructionsVisibility = System.Windows.Visibility.Collapsed;
                BuyOrRateVisibility = System.Windows.Visibility.Collapsed;
            }
            else if (screen == Screen.Instructions)
            {
                ConfigScreenVisibility = System.Windows.Visibility.Collapsed;
                MainScreenVisibility = System.Windows.Visibility.Collapsed;
                AboutVisibility = System.Windows.Visibility.Collapsed;
                TrialExpiredVisibility = System.Windows.Visibility.Collapsed;
                InstructionsVisibility = System.Windows.Visibility.Visible;
                BuyOrRateVisibility = System.Windows.Visibility.Collapsed;
            }
            else if (screen == Screen.BuyOrRate)
            {
                ConfigScreenVisibility = System.Windows.Visibility.Collapsed;
                MainScreenVisibility = System.Windows.Visibility.Collapsed;
                AboutVisibility = System.Windows.Visibility.Collapsed;
                TrialExpiredVisibility = System.Windows.Visibility.Collapsed;
                InstructionsVisibility = System.Windows.Visibility.Collapsed;
                BuyOrRateVisibility = System.Windows.Visibility.Visible;
            }
        }

        #region properties
        private bool _aboutHasBeenClicked;
        private bool _configHasBeenClicked;
        private bool _cameraInUse;
        private bool _camcorderInUse;
        private bool _instructionsHasBeenClicked;
        private bool _rateOrBuyScreenSelected;

        private object _lockObject = new object();

        private MediaLibrary _mediaLibrary = new MediaLibrary();
        private VideoBrush _videoRecorderBrush;

        private CaptureSource _captureSource;
        private VideoCaptureDevice _videoCaptureDevice;

        private PhotoCaptureDevice _cameraCaptureDevice;

        private int _screenWidth;
        public int ScreenWidth
        {
            get { return _screenWidth; }
            set { _screenWidth = value; RaisePropertyChanged("ScreenWidth"); }
        }
        

        private ObservableCollection<Resolution> _availableResolutions;
        public ObservableCollection<Resolution> AvailableResolutions
        {
            get { return _availableResolutions; }
            set { _availableResolutions = value; RaisePropertyChanged("AvailableResolutions"); }
        }

        private ObservableCollection<ScreenShot> _screenShots;
        public ObservableCollection<ScreenShot> ScreenShots
        {
            get { return _screenShots; }
            set { _screenShots = value; RaisePropertyChanged("ScreenShots"); }
        }

        private ScreenShot _selectedScreenShot;
        public ScreenShot SelectedScreenShot
        {
            get { return _selectedScreenShot; }
            set 
            { 
                _selectedScreenShot = value; 
                RaisePropertyChanged("SelectedScreenShot");
                if (SelectedScreenShot != null)
                {
                    NoScreenShotVisibility = Visibility.Collapsed;
                }
            }
        }

        private Resolution _selectedResolution;
        public Resolution SelectedResolution
        {
            get { return _selectedResolution; }
            set { _selectedResolution = value; RaisePropertyChanged("SelectedResolution"); }
        }

        private Visibility _aboutVisibility;
        public Visibility AboutVisibility
        {
            get { return _aboutVisibility; }
            set 
            { 
                _aboutVisibility = value; 
                RaisePropertyChanged("AboutVisibility");
            }
        }

        private Visibility _configScreenVisibility;
        public Visibility ConfigScreenVisibility
        {
            get { return _configScreenVisibility; }
            set { _configScreenVisibility = value; RaisePropertyChanged("ConfigScreenVisibility"); }
        }

        private Visibility _mainScreenVisibility;
        public Visibility MainScreenVisibility
        {
            get { return _mainScreenVisibility; }
            set { _mainScreenVisibility = value; RaisePropertyChanged("MainScreenVisibility"); }
        }

        private Visibility _buyOrRateVisibility;
        public Visibility BuyOrRateVisibility
        {
            get { return _buyOrRateVisibility; }
            set { _buyOrRateVisibility = value; RaisePropertyChanged("BuyOrRateVisibility"); }
        }

        private Visibility _rateVisibility;
        public Visibility RateVisibility
        {
            get { return _rateVisibility; }
            set { _rateVisibility = value; RaisePropertyChanged("RateVisibility"); }
        }
        
        private bool _flashOn;
        public bool FlashOn
        {
            get { return _flashOn; }
            set
            {
                _flashOn = value;
                RaisePropertyChanged("FlashOn");
            }
        }

        private bool _flashOff;
        public bool FlashOff
        {
            get { return _flashOff; }
            set
            {
                _flashOff = value;
                RaisePropertyChanged("FlashOff");
            }
        }

        private string _flashText;
        public string FlashText
        {
            get { return _flashText; }
            set { _flashText = value; RaisePropertyChanged("FlashText"); }
        }

        private bool _soundOn;
        public bool SoundOn
        {
            get { return _soundOn; }
            set { _soundOn = value; RaisePropertyChanged("SoundOn"); }
        }

        private bool _soundOff;
        public bool SoundOff
        {
            get { return _soundOff; }
            set { _soundOff = value; RaisePropertyChanged("SoundOff"); }
        }

        private bool _focusOn;
        public bool FocusOn
        {
            get { return _focusOn; }
            set { _focusOn = value; RaisePropertyChanged("FocusOn"); }
        }

        private bool _focusOff;
        public bool FocusOff
        {
            get { return _focusOff; }
            set { _focusOff = value; RaisePropertyChanged("FocusOff"); }
        }

        private BitmapImage _lastPictureTaken;
        public BitmapImage LastPictureTaken
        {
            get { return _lastPictureTaken; }
            set { _lastPictureTaken = value; RaisePropertyChanged("LastPictureTaken"); }
        }

        private bool _frontCamera;
        public bool FrontCamera
        {
            get { return _frontCamera; }
            set 
            { 
                _frontCamera = value; 
                RaisePropertyChanged("FrontCamera");
                if (value)
                {
                    LoadResolutions(CameraPosition.Front);
                }
            }
        }

        private bool _backCamera;
        public bool BackCamera
        {
            get { return _backCamera; }
            set 
            { 
                _backCamera = value; 
                RaisePropertyChanged("BackCamera");
                if (value)
                {
                    LoadResolutions(CameraPosition.Back);
                }
            }
        }

        private Visibility _cameraLocationVisibility;
        public Visibility CameraLocationVisibility
        {
            get { return _cameraLocationVisibility; }
            set { _cameraLocationVisibility = value; RaisePropertyChanged("CameraLocationVisibility"); }
        }

        public enum CameraPosition
        {
            Front,
            Back
        }

        private int _pictureCount;
        public int PictureCount
        {
            get { return _pictureCount; }
            set { _pictureCount = value; RaisePropertyChanged("PictureCount"); }
        }

        private Visibility _pictureCountVisibility;
        public Visibility PictureCountVisibility
        {
            get { return _pictureCountVisibility; }
            set { _pictureCountVisibility = value; RaisePropertyChanged("PictureCountVisibility"); }
        }

        private Visibility _noScreenShotVisibility;
        public Visibility NoScreenShotVisibility
        {
            get { return _noScreenShotVisibility; }
            set { _noScreenShotVisibility = value; RaisePropertyChanged("NoScreenShotVisibility");}
        }
        

        private bool _pictureCountVisiblityOn;
        public bool PictureCountVisibilityOn
        {
            get { return _pictureCountVisiblityOn; }
            set 
            { 
                _pictureCountVisiblityOn = value; 
                RaisePropertyChanged("PictureCountVisibilityOn");
                if (value)
                {
                    TogglePictureCounterVisibility(value);
                }
            }
        }

        private bool _pictureCountVisiblityOff;
        public bool PictureCountVisibilityOff
        {
            get { return _pictureCountVisiblityOff; }
            set 
            { 
                _pictureCountVisiblityOff = value; 
                RaisePropertyChanged("PictureCountVisibilityOff");
                if (value)
                {
                    TogglePictureCounterVisibility(false);
                }
            }
        }

        private string _daysLeftInTrialString;
        public string DaysLeftInTrialString
        {
            get { return _daysLeftInTrialString; }
            set { _daysLeftInTrialString = value; RaisePropertyChanged("DaysLeftInTrialString"); }
        }

        private Visibility _trialExpiredVisibility;
        public Visibility TrialExpiredVisibility
        {
            get { return _trialExpiredVisibility; }
            set { _trialExpiredVisibility = value; RaisePropertyChanged("TrialExpiredVisibility"); }
        }

        private Visibility _instructionsVisibility;
        public Visibility InstructionsVisibility
        {
            get { return _instructionsVisibility; }
            set { _instructionsVisibility = value; RaisePropertyChanged("InstructionsVisibility"); }
        }

        private string _takingPictureText;
        public string TakingPictureText
        {
            get { return _takingPictureText; }
            set { _takingPictureText = value; RaisePropertyChanged("TakingPictureText"); }
        }

        private int _centerY;
        public int CenterY
        {
            get { return _centerY; }
            set { _centerY = value; RaisePropertyChanged("CenterY"); }
        }

        private int _rotateAngle;
        public int RotateAngle
        {
            get { return _rotateAngle; }
            set { _rotateAngle = value; RaisePropertyChanged("RotateAngle"); }
        }

        private int _centerX;
        public int CenterX
        {
            get { return _centerX; }
            set { _centerX = value; RaisePropertyChanged("CenterX"); }
        }

        private string _step1;
        public string Step1
        {
            get { return _step1; }
            set { _step1 = value; RaisePropertyChanged("Step1"); }
        }
        
        #endregion properties

        #region PICTURE CAPTURE

        private void StartTakingPicture()
        {
            try
            {
                if (!_cameraInUse)
                {
                    TakeSnapShot();
                }
            }
            catch (Exception ex)
            {

            }
        }

        private async void TakeSnapShot()
        {
            lock (_lockObject)
            {
                _cameraInUse = true;
                TakingPictureText = AppResources.TakingPictureText;
            }
            try
            {
                CameraCaptureSequence seq = _cameraCaptureDevice.CreateCaptureSequence(1);
                if (!FrontCamera)
                {
                    if (FlashOn)
                    {
                        _cameraCaptureDevice.SetProperty(KnownCameraPhotoProperties.FlashMode, FlashState.On);
                    }
                    else
                    {
                        _cameraCaptureDevice.SetProperty(KnownCameraPhotoProperties.FlashMode, FlashState.Off);
                    }
                }

                if (SoundOn)
                {
                    _cameraCaptureDevice.SetProperty(KnownCameraGeneralProperties.PlayShutterSoundOnCapture, true);
                }
                else
                {
                    _cameraCaptureDevice.SetProperty(KnownCameraGeneralProperties.PlayShutterSoundOnCapture, false);
                }


                await _cameraCaptureDevice.SetCaptureResolutionAsync(SelectedResolution.CameraResolution);

                MemoryStream stream = new MemoryStream();
                seq.Frames[0].CaptureStream = stream.AsOutputStream();

                if (FocusOn)
                {
                    await _cameraCaptureDevice.FocusAsync();
                }

                await _cameraCaptureDevice.PrepareCaptureSequenceAsync(seq);

                await seq.StartCaptureAsync();

                int countOfPictures = _mediaLibrary.SavedPictures.Count();

                //Not sure what this is, but we need it to save to library
                stream.Seek(0, SeekOrigin.Begin);
                _mediaLibrary.SavePictureToCameraRoll("spycam" + countOfPictures, stream);

                stream.Position = 0;

                var imageSource = new BitmapImage();
                imageSource.SetSource(stream);

                SetCenterXAndCenterY(imageSource);
                LastPictureTaken = imageSource;

            }
            catch (Exception ex)
            {

            }
            lock (_lockObject)
            {
                _cameraInUse = false;
                PictureCount = PictureCount + 1;
                TakingPictureText = string.Empty;
            }
        }

        private void SetCenterXAndCenterY(BitmapImage imageSource)
        {
            if (FrontCamera)
            {
                RotateAngle = 270;
            }
            else
            {
                RotateAngle = 90;
            }

            CenterX = imageSource.PixelWidth / 2;
            CenterY = imageSource.PixelHeight / 2;
        }

        private void RefreshLastImageTaken()
        {
            PictureAlbumCollection allAlbums = _mediaLibrary.RootPictureAlbum.Albums;

            foreach(var album in allAlbums)
            {
                if (album.Name.ToUpper().Contains("CAMERA"))
                {
                    if (album.Pictures != null)
                    {
                        if (album.Pictures.Count() > 0)
                        {
                            PictureCollection allPictures = album.Pictures;
                            Picture picture = allPictures[allPictures.Count() - 1];
                            Stream picToDisplay = picture.GetImage();

                            var imageToShow = new Image
                            {
                                Source = PictureDecoder.DecodeJpeg(picToDisplay, picture.Width, picture.Height)
                            };

                            //if (imageToShow != null)
                            //{
                            //    LastPictureTaken = imageToShow;
                            //}
                        }
                    }
                }
            }
        }

        #endregion PICTURE CAPTURE

        #region clickevents

        #region mainscreenhandler

        private void MainScreenHandler(object sender, EventArgs e)
        {
            if (!_trialExpired)
            {
                _aboutHasBeenClicked = false;
                _configHasBeenClicked = false;
                ConfigScreenVisibility = System.Windows.Visibility.Collapsed;
                AboutVisibility = System.Windows.Visibility.Collapsed;
                MainScreenVisibility = System.Windows.Visibility.Visible;
                InstructionsVisibility = System.Windows.Visibility.Collapsed;
            }
        }

        #endregion mainscreenhandler

        #region aboutclicked

        private void AboutClicked(object sender, EventArgs e)
        {
            if (!_trialExpired)
            {
                _aboutHasBeenClicked = true;
                GoToScreen(Screen.About);
            }
        }

        #endregion aboutclicked

        #region backclicked

        private void BackKeyPressed(object sender, CancelEventArgs e)
        {
            if (_trialExpired)
            {
                _aboutHasBeenClicked = false;
                _configHasBeenClicked = false;
                _instructionsHasBeenClicked = false;
                GoToScreen(Screen.Trial);
            }
            else if (_aboutHasBeenClicked || _configHasBeenClicked || _instructionsHasBeenClicked || _rateOrBuyScreenSelected)
            {
                _aboutHasBeenClicked = false;
                _configHasBeenClicked = false;
                _instructionsHasBeenClicked = false;
                _rateOrBuyScreenSelected = false;

                GoToScreen(Screen.Main);
                e.Cancel = true;
            }
        }

        #endregion backclicked

        #region takepictureclicked

        private void TakePictureHandler(object sender, RoutedEventArgs e)
        {
            if (!_trialExpired)
            StartTakingPicture();
        }

        #endregion takepictureclicked

        #region configurationclicked

        private void ConfigurationHandler(object sender, EventArgs e)
        {
            if (!_trialExpired)
            {
                _configHasBeenClicked = true;
                GoToScreen(Screen.Configuration);
            }
        }

        #endregion configurationclicked

        #region flashradiobutton

        private void FlashClick(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;

            if (rb.Name == "FlashOnRb")
            {
                FlashOn = true;
                FlashOff = false;
            }
            else
            {
                FlashOn = false;
                FlashOff = true;
            }
        }

        #endregion flashradiobutton

        #region purchasehandler

        private void PurchaseHandler(object sender, RoutedEventArgs e)
        {
            _marketPlaceDetailTask.Show();
        }

        #endregion purchasehandler

        #region instructionshandler

        private void InstructionsHandler(object sender, EventArgs e)
        {
            if (!_trialExpired)
            {
                _instructionsHasBeenClicked = true;
                GoToScreen(Screen.Instructions);
            }

        }

        #endregion instructionshandler

        #region rateclicked

        private void RateNowClicked(object sender, RoutedEventArgs e)
        {
            SetAppRated();
            Trial.Add10DaysToTrial();
            _review.Show();
        }

        #endregion rateclicked

        #region buyclicked

        private void BuyNowClicked(object sender, RoutedEventArgs e)
        {
            _marketPlaceDetailTask.Show();
        }

        #endregion buyclicked

        #region skipbuyrate

        private void SkipBuyOrRateClicked(object sender, RoutedEventArgs e)
        {
            GoToScreen(Screen.Main);
        }

        #endregion skipbuyrate

        #endregion clickevents

        #region INotifyPropertyChagned
        public void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion INotifyPropertyChagned

        #region dispose
        
        public void Dispose()
        {
            if (_cameraCaptureDevice != null)
            {
                _cameraCaptureDevice.Dispose();
            }

            if (_mediaLibrary != null)
            {
                _mediaLibrary.Dispose();
            }

            SaveSettings();
        }

        #endregion dispose

        #region appsettings

        public void SaveStartDateOfTrial()
        {
            Trial.SaveStartDateOfTrial();
        }

        public DateTime GetStartDateOfTrial()
        {
            DateTime returnValue = DateTime.Today;

            returnValue = Trial.GetStartDateOfTrial();

            return returnValue;
        }

        public int GetDaysLeftInTrial()
        {
            int returnValue = 0;
            returnValue = Trial.GetDaysLeftInTrial();
            return returnValue;
        }

        public bool IsFirstTimeOpeningApplication()
        {
            bool returnValue = true;

            IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

            if (appSettings.Count() > 0)
            {
                returnValue = false;
            }

            return returnValue;
        }

        private void SetAppRated()
        {
            try
            {
                IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
                if (!appSettings.Contains("apphasbeenrated"))
                {
                    appSettings.Add("apphasbeenrated", true);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void LoadShowRate()
        {
            try
            {
                IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
                if (appSettings.Contains("apphasbeenrated"))
                {
                    bool rated = (bool)appSettings["apphasbeenrated"];
                    if (rated)
                    {
                        RateVisibility = System.Windows.Visibility.Collapsed;
                    }
                    else
                    {
                        RateVisibility = System.Windows.Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        public void SaveSettings()
        {
            MainPage page = MainPage._mainPageInstance;
            try
            {
                IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
                if (!appSettings.Contains("flashon"))
                {
                    if (page.FlashOn != null)
                    appSettings.Add("flashon", page.FlashOn);
                }
                else
                {
                    if (page.FlashOn != null)
                    appSettings["flashon"] = page.FlashOn;
                }

                if (!appSettings.Contains("frontcameraselected"))
                {
                    if (page.FrontCamera != null)
                    appSettings.Add("frontcameraselected", page.FrontCamera);
                }
                else
                {
                    if (page.FrontCamera != null)
                    appSettings["frontcameraselected"] = page.FrontCamera;
                }

                if (!appSettings.Contains("selectedresolution"))
                {
                    if (page.SelectedResolution != null)
                    appSettings.Add("selectedresolution", page.SelectedResolution.Display);
                }
                else
                {
                    if (page.SelectedResolution != null)
                    appSettings["selectedresolution"] = page.SelectedResolution.Display;
                }

                if (!appSettings.Contains("mainscreenimage"))
                {
                    if (page.SelectedScreenShot != null)
                    appSettings.Add("mainscreenimage", page.SelectedScreenShot.Name);
                }
                else
                {
                    if (page.SelectedScreenShot != null)
                    appSettings["mainscreenimage"] = page.SelectedScreenShot.Name;
                }

                if (!appSettings.Contains("soundon"))
                {
                    if (page.SoundOn != null)
                    appSettings.Add("soundon", page.SoundOn);
                }
                else
                {
                    if (page.SoundOn != null)
                    appSettings["soundon"] = page.SoundOn;
                }

                if (!appSettings.Contains("focuson"))
                {
                    if (page.FocusOn != null)
                    appSettings.Add("focuson", page.FocusOn);
                }
                else
                {
                    if (page.FocusOn != null)
                    appSettings["focuson"] = page.FocusOn;
                }

                if (!appSettings.Contains("picturecountvisibilityon"))
                {
                    if (page.PictureCountVisibilityOn != null)
                    appSettings.Add("picturecountvisibilityon", page.PictureCountVisibilityOn);
                }
                else
                {
                    if (page.PictureCountVisibilityOn != null)
                    appSettings["picturecountvisibilityon"] = page.PictureCountVisibilityOn;
                }
                
                appSettings.Save();
            }
            catch (Exception ex)
            {

            }
        }

        private void LoadSettings()
        {
            try
            {
                IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

                if (appSettings.Count() > 3)
                {
                    //Flash
                    if (appSettings.Contains("flashon"))
                    {
                        bool flashOn = (bool)appSettings["flashon"];
                        if (flashOn)
                        {
                            FlashOn = true;
                            FlashOff = false;
                        }
                        else
                        {
                            FlashOff = true;
                            FlashOn = false;
                        }
                    }

                    //camera location
                    if (appSettings.Contains("frontcameraselected"))
                    {
                        bool frontCamSelected = (bool)appSettings["frontcameraselected"];
                        if (frontCamSelected)
                        {
                            FrontCamera = true;
                            BackCamera = false;
                        }
                        else
                        {
                            BackCamera = true;
                            FrontCamera = false;
                        }
                    }

                    if (appSettings.Contains("selectedresolution"))
                    {
                        //picture resolution
                        string selectedResolution = (string)appSettings["selectedresolution"];
                        foreach (Resolution row in AvailableResolutions)
                        {
                            if (row.Display == selectedResolution)
                            {
                                SelectedResolution = row;
                            }
                        }
                        //set resolution to default if there is no resolution
                        if (SelectedResolution == null)
                        {
                            SelectedResolution = AvailableResolutions[AvailableResolutions.Count() - 1];
                        }
                    }

                    if (appSettings.Contains("mainscreenimage"))
                    {
                        //main image
                        string mainImage = (string)appSettings["mainscreenimage"];
                        if (mainImage != null)
                        {
                            foreach (var row in ScreenShots)
                            {
                                if (row.Name == mainImage)
                                {
                                    SelectedScreenShot = row;
                                }
                            }
                        }
                    }

                    if (appSettings.Contains("soundon"))
                    {
                        //sound on or off
                        bool sound = (bool)appSettings["soundon"];
                        if (sound)
                        {
                            SoundOn = true;
                            SoundOff = false;
                        }
                        else
                        {
                            SoundOff = true;
                            SoundOn = false;
                        }
                    }

                    if (appSettings.Contains("picturecountvisibilityon"))
                    {
                        //sound on or off
                        bool picCntVis = (bool)appSettings["picturecountvisibilityon"];
                        if (picCntVis)
                        {
                            PictureCountVisibilityOn = true;
                            PictureCountVisibilityOff = false;
                        }
                        else
                        {
                            PictureCountVisibilityOff = true;
                            PictureCountVisibilityOn = false;
                        }
                    }

                    if (appSettings.Contains("focuson"))
                    {
                        //focus
                        bool focusOn = (bool)appSettings["focuson"];
                        if (focusOn)
                        {
                            FocusOn = true;
                            FocusOff = false;
                        }
                        else
                        {
                            FocusOff = true;
                            FocusOn = false;
                        }
                    }
                }
                else
                {
                    FlashOn = false;
                    SoundOn = false;
                    FlashOff = true;
                    SoundOff = true;
                    FocusOff = false;
                    FocusOn = true;
                    FrontCamera = false;
                    BackCamera = true;
                    PictureCountVisibilityOn = true;
                    PictureCountVisibilityOff = false;
                }
            }
            catch (Exception ex)
            {

            }
        }

        #endregion appsettings

        #region camera disposing properly

        public void DisposeCamera()
        {
            if (_cameraCaptureDevice != null)
            {
                _cameraCaptureDevice.Dispose();
            }
        }

        public void InitializeCameraAfterActivation()
        {
            if (!_trialExpired || !(Application.Current as App).IsTrial)
            {
                GoToScreen(Screen.Main);
                if (BackCamera)
                {
                    OpenCameraDevice(CameraPosition.Back);
                }
                else
                {
                    OpenCameraDevice(CameraPosition.Front);
                }
            }
            else
            {
                GoToScreen(Screen.Trial);
            }
        }

        #endregion camera disposing properly

    }
}