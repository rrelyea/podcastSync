using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PodcastSyncLib
{
    public class Podcast : INotifyPropertyChanged
    {
        public Podcast()
        {
            // TODO: once system.text.json can deal with r/o collections, make episodes r/o
            Episodes = new List<Episode>();
        }
        public string RssUri { get; set; }
        public string Title { get; set; }

        public List<Episode> Episodes { get; set; }

        public int EpisodesToDownload
        {
            get { return _EpisodesToDownload; }
            set
            {
                _EpisodesToDownload = value;
                NotifyPropertyChanged();
            }
        }

        private int _EpisodesToDownload;

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
