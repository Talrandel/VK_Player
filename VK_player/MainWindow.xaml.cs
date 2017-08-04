using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WMPLib;

namespace VK_player
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BackgroundWorker backgroundWorker;
        private DispatcherTimer timer;
        private List<Audio> TrackList;

        private int CurrentAudioNumber;
        private Audio CurrentAudio;

        private double CurrentAudioPosition;

        private bool IsLooped;
        private bool IsRandom;

        private bool HandlePositionSlider;

        private double VolumeLevel;
        private readonly int VolumeMax = 800;
        private readonly int VolumeСoefficient = 8;

        private BitmapImage ButtonImage_VolumeOn;
        private BitmapImage ButtonImage_VolumeOff;
        private BitmapImage ButtonImage_Play;
        private BitmapImage ButtonImage_Pause;
        private BitmapImage ButtonImage_LoopOn;
        private BitmapImage ButtonImage_LoopOff;
        private Image TempImage;

        private WMPLib.WindowsMediaPlayer Player;

        public MainWindow()
        {
            InitializeComponent();
            TrackList = new List<Audio>();
            Player = new WMPLib.WindowsMediaPlayer();
            Player.PlayStateChange += new WMPLib._WMPOCXEvents_PlayStateChangeEventHandler(Player_PlayStateChange);
            Player.MediaError += new WMPLib._WMPOCXEvents_MediaErrorEventHandler(Player_MediaError);

            VolumeLevel = VolumeMax;
            SliderVolumePosition.Value = VolumeLevel;

            CurrentAudioPosition = 0;

            IsLooped = false;
            IsRandom = false;
            HandlePositionSlider = true;

            ButtonImage_VolumeOn = new BitmapImage(new Uri(@"D:\Programs\VK_player\VK_player\icon_volume_on.png"));
            ButtonImage_VolumeOff = new BitmapImage(new Uri(@"D:\Programs\VK_player\VK_player\icon_volume_off.png"));
            ButtonImage_Play = new BitmapImage(new Uri(@"D:\Programs\VK_player\VK_player\icon_play.png"));
            ButtonImage_Pause = new BitmapImage(new Uri(@"D:\Programs\VK_player\VK_player\icon_pause.png"));
            ButtonImage_LoopOn = new BitmapImage(new Uri(@"D:\Programs\VK_player\VK_player\icon_loop_on.png"));
            ButtonImage_LoopOff = new BitmapImage(new Uri(@"D:\Programs\VK_player\VK_player\icon_loop_off.png"));
            TempImage = new Image();

            int minutes = (int)(Player.controls.currentPosition / 60);
            int seconds = (int)(Player.controls.currentPosition % 59);
            string durationCurrent = minutes.ToString() + ":" + seconds.ToString();
            LabelTime.Content = String.Format("{0} / {1}", durationCurrent, durationCurrent);
            SliderAudioPosition.Maximum = 1;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
        }
        void timer_Tick(object sender, EventArgs e)
        {
            if (Player.currentMedia != null)
            {
                SliderAudioPosition.Value = Player.controls.currentPosition;
                HandlePositionSlider = true;
                CurrentAudioPosition = Player.controls.currentPosition;
                int minutes = (int)(Player.controls.currentPosition / 60);
                int seconds = (int)(Player.controls.currentPosition % 59);
                string durationCurrent = minutes.ToString() + ":" + seconds.ToString();
                LabelTime.Content = String.Format("{0} / {1}", durationCurrent, Player.currentMedia.durationString);
            }
            else
                LabelTime.Content = String.Format("{0} / {1}", 0.ToString(), 0.ToString());
        }

        private void Player_MediaError(object pMediaObject)
        {
            MessageBox.Show("Cannot play media file.");
        }

        private void Player_PlayStateChange(int NewState)
        {
            switch (NewState)
            {
                case 0:    // Undefined
                    break;
                case 1:    // Stopped
                    TempImage.Source = ButtonImage_Play;
                    ButtonPlay.Content = TempImage;
                    timer.Stop();
                    break;
                case 2:    // Paused
                    TempImage.Source = ButtonImage_Play;
                    ButtonPlay.Content = TempImage;
                    timer.Stop();
                    break;
                case 3:    // Playing
                    TempImage.Source = ButtonImage_Pause;
                    ButtonPlay.Content = TempImage;
                    timer.Start();
                    SliderAudioPosition.Maximum = Player.currentMedia.duration;
                    break;
                case 4:    // ScanForward
                    break;
                case 5:    // ScanReverse
                    break;
                case 6:    // Buffering
                    break;
                case 7:    // Waiting
                    break;
                case 8:    // MediaEnded
                    if (IsLooped)
                    {
                        //PlayAudioByNumber();
                        DispatcherTimer timerOne = new DispatcherTimer();

                        timerOne.Interval = TimeSpan.FromMilliseconds(1);
                        timerOne.Tick += delegate{
                            timerOne.Stop();
                            PlayAudioByNumber();
                        };
                        timerOne.Start();
                        return;
                    }
                    else if (IsRandom)
                    {
                        throw new NotImplementedException();
                    }
                    DispatcherTimer timerTwo = new DispatcherTimer();

                    timerTwo.Interval = TimeSpan.FromMilliseconds(1);
                    timerTwo.Tick += delegate
                    {
                        timerTwo.Stop();
                        Next_Click(this, new RoutedEventArgs());
                        //PlayAudioByNumber();
                    };
                    timerTwo.Start();

                    break;
                case 9:    // Transitioning
#warning Сюда приходит логика при попытке воспроизвести тот же файл, зацикливаясь
                    break;
                case 10:   // Ready
                    break;
                case 11:   // Reconnecting
                    break;
                case 12:   // Last
                    break;
                default:
                    MessageBox.Show("Unknown State: " + NewState.ToString());
                    break;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var LoadWindow = new LoadVK();
            LoadWindow.ShowDialog();
            backgroundWorker = ((BackgroundWorker)this.FindResource("backgroundWorker"));
            backgroundWorker.RunWorkerAsync();
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!Settings1.Default.Authorized)
            {
                Thread.Sleep(500);
            }
            WebRequest request = WebRequest.Create("https://api.vk.com/method/audio.get?owner_id=" + Settings1.Default.Id + "&need_user=0&access_token=" + Settings1.Default.Token);
            WebResponse response = request.GetResponse();
            string responseFromServer = string.Empty;
            using (Stream dataStream = response.GetResponseStream())
            {
                using (StreamReader reader = new StreamReader(dataStream))
                {
                    responseFromServer = reader.ReadToEnd();
                }
            }
            response.Close();
            responseFromServer = HttpUtility.HtmlDecode(responseFromServer);

            JToken jtoken = JToken.Parse(responseFromServer);
            TrackList = jtoken["response"].Children().Skip(1).Select(c => c.ToObject<Audio>()).ToList();
            
            for (int i = 0; i < TrackList.Count; i++)
            {
                TrackList[i].url = TrackList[i].url.Split('?')[0];
            }

            Dispatcher.Invoke(delegate
            {
                for (int i = 0; i < TrackList.Count; i++)
                {
                    ListBoxTracks.Items.Add(TrackList[i].artist + " - " + TrackList[i].title);
                }
                ListBoxTracks.SelectedIndex = 0;
                CurrentAudioNumber = ListBoxTracks.SelectedIndex;
            });
        }

        private void ListBoxTracks_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            CurrentAudioNumber = ListBoxTracks.SelectedIndex;
            Play_Click(this, new RoutedEventArgs());
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentAudio != null && Player.currentMedia != null)
            {
                if (TrackList[CurrentAudioNumber].url != Player.currentMedia.sourceURL)
                {
                    PlayAudioByNumber();
                }
                else
                {
                    if (Player.playState == WMPPlayState.wmppsPlaying)
                    {
                        Player.controls.pause();
                    }
                    else if (Player.playState == WMPPlayState.wmppsPaused)
                    {
                        Player.controls.playItem(Player.currentMedia);
                        Player.controls.currentPosition = CurrentAudioPosition;
                    }
                    else if (Player.playState == WMPPlayState.wmppsStopped)
                    {
                        PlayAudioByNumber();
                    }
                    else
                    {
                        PlayAudioByNumber();
                    }
                }
            }
            else
            {
                PlayAudioByNumber();
            }

            SliderAudioPosition.Value = CurrentAudioPosition;
            SliderAudioPosition.Maximum = Player.currentMedia.duration;
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            Player.controls.stop();
            SliderAudioPosition.Maximum = 1;
            SliderAudioPosition.Value = 0;
            LabelTime.Content = String.Format("{0} / {1}", 0.ToString(), 0.ToString());
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentAudioNumber == ListBoxTracks.Items.Count - 1)
                CurrentAudioNumber = 0;
            else
                CurrentAudioNumber++;

            ListBoxTracks.SelectedIndex = CurrentAudioNumber;
            Stop_Click(this, new RoutedEventArgs());
            PlayAudioByNumber();
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentAudioNumber == 0)
                CurrentAudioNumber = ListBoxTracks.Items.Count - 1;
            else
                CurrentAudioNumber--;

            ListBoxTracks.SelectedIndex = CurrentAudioNumber;
            Stop_Click(this, new RoutedEventArgs());
            PlayAudioByNumber();
        }

        private void PlayAudioByNumber()
        {
            Audio selected = TrackList[CurrentAudioNumber];
            try
            {
                CurrentAudio = selected;
                Player.URL = selected.url;
                Player.controls.play();
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show(ex.StackTrace);
            }
        }

        private void Volume_Click(object sender, RoutedEventArgs e)
        {            
            if (SliderVolumePosition.Value > 1)
            {
                VolumeLevel = SliderVolumePosition.Value;
                SliderVolumePosition.Value = 0;
                TempImage.Source = ButtonImage_VolumeOff;
            }
            else if (SliderVolumePosition.Value == 0)
            {
                SliderVolumePosition.Value = VolumeLevel;
                TempImage.Source = ButtonImage_VolumeOn;
            }
            ButtonVolume.Content = TempImage;
            Player.settings.volume = VolumeСoefficient * (int)SliderVolumePosition.Value;
        }

        private void SliderVolumePosition_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Player.settings.volume = VolumeСoefficient * (int)SliderVolumePosition.Value;
        }

        private void SliderAudioPosition_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
#warning Обрабатывать только ручное изменение положения трека
            //if (!HandlePositionSlider)
            //{
            //    Player.controls.currentPosition = e.NewValue;
            //    HandlePositionSlider = true;
            //}
            //else
            //{
            //    Player.controls.currentPosition = e.NewValue;
            //    HandlePositionSlider = false;
            //}
            Player.controls.currentPosition = e.NewValue;
        }

        private void Loop_Click(object sender, RoutedEventArgs e)
        {
            TempImage.Source = (!IsLooped) ? ButtonImage_LoopOn : ButtonImage_LoopOff;
            IsLooped = !IsLooped;
            ButtonLoop.Content = TempImage;
        }
    }
}