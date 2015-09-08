using GalaSoft.MvvmLight.Threading;
using KAIT.Kiosk.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
//using KAIT.Kiosk.Extensions;
using System.Configuration;

namespace KAIT.Kiosk.Controls
{
    /// <summary>
    /// Interaction logic for PassiveEngage.xaml
    /// </summary>
    public partial class MediaContent : UserControl
    {
        int _transitionTime = 3;
        Timer _timer;

        Storyboard _fadeOut;
        Storyboard _fadeIn;


        MediaContentViewModel _viewModel;
        public MediaContent()
        {
            InitializeComponent();

            var time = ConfigurationManager.AppSettings["ContentTransitionTime"];
    
            int result = 0;
            if (int.TryParse(time, out result))
                _transitionTime = result;

            _viewModel = this.DataContext as MediaContentViewModel;
            _viewModel.Activated += _viewModel_Activated;
            _viewModel.Deactivated += _viewModel_Deactivated;
            _timer = new Timer(_transitionTime *1000);
            _timer.Elapsed += _timer_Elapsed;

            this.Loaded += (s, e) =>
            {
                _fadeIn = (Storyboard)TryFindResource("FadeIn");
                _fadeOut = (Storyboard)TryFindResource("FadeOut");
                _fadeOut.Completed += _fadeOut_Completed;

                Video.MediaOpened += Video_MediaOpened;
                Video.MediaEnded += Video_MediaEnded;
            };
            
        }

        void _fadeOut_Completed(object sender, EventArgs e)
        {
            
            try
            {
                _viewModel = this.DataContext as MediaContentViewModel;
                _viewModel.IsVideoPlaying = false;
               
                _viewModel.MediaSource = "blank.bmp";
                _viewModel.MoveNext();
            }
            catch (Exception ex)
            {
                _viewModel.ErrorMessage = String.Format("Missing Media Content: {0}", ex.Message);
            }
            
            DispatcherHelper.UIDispatcher.Invoke(() =>
            {
                _viewModel = this.DataContext as MediaContentViewModel;
                _viewModel.IsVideoPlaying = true;
              
                _fadeIn.Begin();
               
            });
        }

        void Video_MediaEnded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("MEDIA ENDED");
            if (Video.NaturalDuration.HasTimeSpan)
                DispatcherHelper.UIDispatcher.Invoke(() =>
                {
                 
                    _fadeOut.Begin();
                });
        }

        void Video_MediaOpened(object sender, RoutedEventArgs e)
        {
          
            if (!Video.NaturalDuration.HasTimeSpan)
            {
              
                _timer.Start();
            }
        }

        void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();
           
            DispatcherHelper.UIDispatcher.Invoke(() =>
            {
                _fadeOut.Begin();
            });
        }

        void _viewModel_Deactivated(object sender, EventArgs e)
        {
          

            _timer.Stop();
          
            Video.LoadedBehavior = MediaState.Manual;
            Video.Pause();
        }

        void _viewModel_Activated(object sender, EventArgs e)
        {
 
            Video.Play();
        }

    }
}
