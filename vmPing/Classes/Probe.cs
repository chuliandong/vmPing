﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading;
using vmPing.Views;

namespace vmPing.Classes
{
    public enum ProbeStatus
    {
        Up,
        Down,
        Error,
        Indeterminate,
        Inactive
    }

    public partial class Probe : INotifyPropertyChanged
    {
        public static ObservableCollection<StatusChangeLog> StatusChangeLog = new ObservableCollection<StatusChangeLog>();
        public static StatusHistoryWindow StatusWindow;

        private static int activeCount;
        public static int ActiveCount
        {
            get => activeCount;
            set
            {
                activeCount = value;
                OnActiveCountChanged(EventArgs.Empty);
            }
        }
        public static event EventHandler ActiveCountChanged;
        protected static void OnActiveCountChanged(EventArgs e)
        {
            ActiveCountChanged?.Invoke(null, e);
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public IsolatedPingWindow IsolatedWindow { get; set; }
        public int DownCount { get; set; }
        public BackgroundWorker Thread { get; set; }
        public AutoResetEvent ThreadResetEvent { get; set; }
        public PingStatistics Statistics { get; set; }
        public PingReply Reply { get; set; }
        public Ping Sender { get; set; }
        public ObservableCollection<string> History { get; } = new ObservableCollection<string>();

        private string hostname;
        public string Hostname
        {
            get => hostname;
            set
            {
                if (value != hostname)
                {
                    hostname = value;
                    NotifyPropertyChanged("Hostname");
                }
            }
        }


        private string alias;
        public string Alias
        {
            get => alias;
            set
            {
                if (value != alias)
                {
                    alias = value;
                    NotifyPropertyChanged("Alias");
                }
            }
        }

        private ProbeStatus status = ProbeStatus.Inactive;
        public ProbeStatus Status
        {
            get => status;
            set
            {
                if (value != status)
                {
                    status = value;
                    NotifyPropertyChanged("Status");
                }
            }
        }

        private bool isActive = false;
        public bool IsActive
        {
            get => isActive;
            set
            {
                if (value == true)
                    ++ActiveCount;
                else
                    --ActiveCount;
                NotifyPropertyChanged("NumberOfActivePings");

                if (value != isActive)
                {
                    isActive = value;
                    NotifyPropertyChanged("IsActive");
                }
            }
        }

        private string statisticsText;
        public string StatisticsText
        {
            get => statisticsText;
            set
            {
                if (value != statisticsText)
                {
                    statisticsText = value;
                    NotifyPropertyChanged("StatisticsText");
                }
            }
        }


        public void AddHistory(string historyItem)
        {
            const int MaxSize = 3600;

            History.Add(historyItem);
            if (History.Count > MaxSize)
                History.RemoveAt(0);
        }

        public void WriteFinalStatisticsToHistory()
        {
            if (Statistics.Sent == 0) return;

            var roundTripTimes = new List<int>();
            var rttRegex = new Regex(@"  \[(?<rtt><?\d+) ?ms]$");

            foreach (var historyItem in History)
            {
                Match regexMatch = rttRegex.Match(historyItem);
                if (!regexMatch.Success) continue;
                if (regexMatch.Groups["rtt"].Value == "<1")
                    roundTripTimes.Add(0);
                else
                    roundTripTimes.Add(int.Parse(regexMatch.Groups["rtt"].Value));
            }

            // Display statics and round trip times.
            AddHistory("");
            AddHistory($"Sent {Statistics.Sent}, Received {Statistics.Received}, Lost {Statistics.Sent - Statistics.Received} ({(100 * (Statistics.Sent - Statistics.Received)) / Statistics.Sent}% loss)");
            if (roundTripTimes.Count > 0)
            {
                AddHistory($"Minimum ({roundTripTimes.Min()}ms), Maximum ({roundTripTimes.Max()}ms), Average ({roundTripTimes.Average().ToString("0.##")}ms)");
            }
            AddHistory(" ");
        }

        private void NotifyPropertyChanged(String info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }
}
