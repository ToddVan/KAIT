/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:KAIT.Kiosk"
                           x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"

  You can also use Blend to do all this with the tool's support.
  See http://www.galasoft.ch/mvvm
*/

using GalaSoft.MvvmLight.Ioc;
using KAIT.Kiosk.Services;
using Microsoft.Kinect;
using Microsoft.Practices.ServiceLocation;
using KAIT.Common.Interfaces;
using KAIT.ObjectDetection;
using KAIT.ObjectDetection.ViewModel;
using KAIT.Common.Sensor;
using KAIT.Biometric.Services;
using KAIT.ContentMetaData;
using KAIT.Kinect.Service;
using KAIT.Common.Services.Messages;


namespace KAIT.Kiosk.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            ////if (ViewModelBase.IsInDesignModeStatic)
            ////{
            ////    // Create design time view services and models
            ////    SimpleIoc.Default.Register<IDataService, DesignDataService>();
            ////}
            ////else
            ////{
            ////    // Create run time view services and models
            ////    SimpleIoc.Default.Register<IDataService, DataService>();
            ////}

            SimpleIoc.Default.Register<MainViewModel>();
            SimpleIoc.Default.Register<ConfigurationViewModel>();
            SimpleIoc.Default.Register<MediaContentViewModel>();

            SimpleIoc.Default.Register<IDemographicsService, BiometricTelemetryService>();
            SimpleIoc.Default.Register<IKioskInteractionService, KioskInteractionService>();
            SimpleIoc.Default.Register<ISensorService<KinectSensor>, KinectSensorService>();
            SimpleIoc.Default.Register<ISpeechService, KinectSpeechService>();
            SimpleIoc.Default.Register<IItemInteractionService, ObjectDetectionService>();
            SimpleIoc.Default.Register<IContentManagement<ZoneFileMetaData>, FileContentManagementService>();
            SimpleIoc.Default.Register<IBodyTrackingService, BodyTrackingService>();
            SimpleIoc.Default.Register<IConfigurationProvider, ConfigurationProvider>();
        }

        public MainViewModel Main
        {
            get { return ServiceLocator.Current.GetInstance<MainViewModel>(); }
        }

        public ConfigurationViewModel Configuration
        {
            get { return ServiceLocator.Current.GetInstance<ConfigurationViewModel>(); }
        }
       

        public MediaContentViewModel MediaContent
        {
            get { return ServiceLocator.Current.GetInstance<MediaContentViewModel>(); }
        }

        public TouchScreenViewModel TouchScreen
        {
            get { return ServiceLocator.Current.GetInstance<TouchScreenViewModel>(); }
        }

        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }
}