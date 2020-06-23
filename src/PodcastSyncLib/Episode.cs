using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PodcastSyncLib
{
    public class Episode : INotifyPropertyChanged
    {
        public string EnclosureUri { get; set; }
        public string Title { get; set; }
        public bool NeedsDownload
        {
            get
            {
                return _NeedsDownload;
            }
            set
            {
                _NeedsDownload = value;
                NotifyPropertyChanged();
            }
        }

        public string FilePath { get; set; }
        public string FolderPath { get; set; }
        public Podcast Podcast { get; set; }

        private bool _NeedsDownload;

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
