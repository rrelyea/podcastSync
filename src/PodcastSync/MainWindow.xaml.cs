using PodcastSyncLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Linq;

namespace PodcastSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            Settings.Load();
            DataContext = Settings.Instance;
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            this.KeyDown += MainWindow_KeyDown;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                SyncPodcasts();
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Settings.Save();
        }

        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            Settings.Dirty = true;
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Settings.Dirty = true;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (Settings.Instance.WindowTop.HasValue)
            {
                this.Top = Settings.Instance.WindowTop.Value;
                this.Left = Settings.Instance.WindowLeft.Value;
                this.Height = Settings.Instance.WindowHeight.Value;
                this.Width = Settings.Instance.WindowWidth.Value;
            }
            podcasts.ItemsSource = Settings.Instance.Podcasts;
            this.SizeChanged += MainWindow_SizeChanged;
            this.LocationChanged += MainWindow_LocationChanged;
            LookForNewEpisodes();
            DispatcherTimer timer = new DispatcherTimer() { IsEnabled = true, Interval = new TimeSpan(hours: 1, minutes: 0, seconds: 0) };
            timer.Start();
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            LookForNewEpisodes();
        }

        private void LookForNewEpisodes()
        {
            Parallel.ForEach(Settings.Instance.Podcasts, async (podcast) =>
            {
                await DownloadNewEpisodes(podcast, updateDownloadCount: true, shouldDownload: true);
            });
        }

        private async void Add(object sender, RoutedEventArgs e)
        {
            var podcast = new Podcast() { RssUri = rss.Text };
            var rssDoc = await GetRssDoc(podcast);
            var titleElement = rssDoc.Descendants(XName.Get("title", "")).FirstOrDefault();
            podcast.Title = titleElement?.Value;
            if (podcast.Title != null)
            {
                Settings.Instance.Podcasts.Add(podcast);
                Settings.Save(forceSave: true);
                rss.Text = "";
            }
        }

        private void EditSettings(object sender, RoutedEventArgs e)
        {
            var psi = new ProcessStartInfo()
            {
                FileName = Environment.ExpandEnvironmentVariables("%windir%\\explorer.exe"),
                Arguments = Settings.SettingsFile,
                Verb = "edit"
            };
            Process.Start(psi);
        }

        private void Sync(object sender, RoutedEventArgs e)
        {
            SyncPodcasts();
        }

        private void SyncPodcasts()
        {
            if (Directory.Exists(Settings.Instance.PodcastPath))
            {
                Parallel.ForEach(Settings.Instance.Podcasts, async (podcast) =>
                {
                    await DownloadNewEpisodes(podcast, updateDownloadCount:false, shouldDownload:true);
                });
            }
        }

        private async Task DownloadNewEpisodes(Podcast podcast, bool updateDownloadCount, bool shouldDownload)
        {
            XDocument rssDoc = await GetRssDoc(podcast);
            var enclosures = rssDoc.Descendants(XName.Get("enclosure", ""));
            
            var url = enclosures.Skip(0).FirstOrDefault().Attribute("url").Value;
            var uri = new Uri(url);

            var folderPath = Path.Combine(Settings.Instance.PodcastPath, podcast.Title);
            var newFilePath = Path.Combine(folderPath, uri.Segments[uri.Segments.Length - 1]);
            var episode = new Episode() { EnclosureUri = url, Title = "TODO", FilePath = newFilePath, FolderPath = folderPath };
            episode.NeedsDownload = !File.Exists(newFilePath);
            episode.Podcast = podcast;
            if (episode.NeedsDownload && updateDownloadCount)
            {
                podcast.EpisodesToDownload = 1;
            }

            if (shouldDownload && episode.NeedsDownload && Directory.Exists(Settings.Instance.PodcastPath))
            {
                using (var client = new HttpClient())
                {
                    var mp3Download = await client.GetAsync(episode.EnclosureUri);
                    await CopyEpisodeToPodcastDir(mp3Download, episode);
                    episode.NeedsDownload = false;
                    episode.Podcast.EpisodesToDownload--;
                }
            }
        }

        private static async Task CopyEpisodeToPodcastDir(HttpResponseMessage mp3Download, Episode episode)
        {
            string newFilePath = episode.FilePath;
            string folderPath = episode.FolderPath;
            if (Directory.Exists(folderPath) && Directory.GetFiles(folderPath).Length > 0)
            {
                Directory.Delete(folderPath, true);
            }

            Directory.CreateDirectory(folderPath);
            using (var fileStream = new FileStream(newFilePath, FileMode.Create))
            {
                var stream = await mp3Download.Content.ReadAsStreamAsync();
                await stream.CopyToAsync(fileStream);
            }
        }

        private async Task<XDocument> GetRssDoc(Podcast podcast)
        {
            using (var client = new HttpClient())
            {
                var rssDownload = await client.GetAsync(podcast.RssUri);
                if (rssDownload.IsSuccessStatusCode)
                {
                    XDocument rssDoc = await XDocument.LoadAsync(await rssDownload.Content.ReadAsStreamAsync(), LoadOptions.None, CancellationToken.None);
                    return rssDoc;
                }
            }

            return null;
        }
    }
}
