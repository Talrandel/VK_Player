using System;
using System.Windows;
using System.Windows.Controls;

namespace VK_player
{
    /// <summary>
    /// Логика взаимодействия для LoadVK.xaml
    /// </summary>
    public partial class LoadVK : Window
    {
        public LoadVK()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WebBrowser.Navigate("https://oauth.vk.com/authorize?client_id=5429926&display=popup&redirect_uri=https://oauth.vk.com/blank.html&scope=audio&response_type=token&v=5.50");
        }

        private void WebBrowser_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            LabelAuthInfo.Content = "Loading";
        }

        private void WebBrowser_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            LabelAuthInfo.Content = "Loaded";
            try
            {
                string url = WebBrowser.Source.OriginalString;
                string temp = url.Split('#')[1];
                if (temp[0] == 'a')
                {
                    var tokenTemp = temp.Split('=')[1];
                    var tokenFinal = tokenTemp.Split('&')[0];
                    Settings1.Default.Token = tokenFinal;
                    Settings1.Default.Id = temp.Split('=')[3];
                    Settings1.Default.Authorized = true;
                    //MessageBox.Show(Settings1.Default.Token + Environment.NewLine + Settings1.Default.Id);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}