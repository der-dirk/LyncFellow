using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;
using Microsoft.Lync.Model;
using Microsoft.Lync.Model.Conversation;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace LyncFellow
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            using (Mutex mutexApplication = new Mutex(false, "LyncFellowApplication"))
            {
                if (!mutexApplication.WaitOne(0, false))
                {
                    MessageBox.Show(Application.ProductName + " is already running!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                ApplicationContext applicationContext = new ApplicationContext();
                Application.Run(applicationContext);
            }
        }

    }

    class ApplicationContext : System.Windows.Forms.ApplicationContext
    {

        System.ComponentModel.IContainer _components;
        NotifyIcon _notifyIcon;
        SettingsForm _settingsForm;
        ViewModel _viewModel;

        Buddies _buddies;
        LyncClient _lyncClient;
        System.Windows.Forms.Timer _housekeepingTimer;
        DateTime _lyncEventsUpdated;

        FileSystemWatcher _watcher;
        private enum TrackerStatus { OK = 0, ERROR = 1, UNKNOWN = 2 }
        TrackerStatus _trackerStatus;

        public ApplicationContext()
        {
            if (Properties.Settings.Default.CallUpgrade)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.CallUpgrade = false;
                Properties.Settings.Default.Save();
            }

            _components = new System.ComponentModel.Container();
            _notifyIcon = new NotifyIcon(_components)
            {
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true
            };
            _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Settings", null, new EventHandler(MenuSettingsItem_Click)));
            _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Exit", null, new EventHandler(MenuExitItem_Click)));

            _buddies = new Buddies();

            _housekeepingTimer = new System.Windows.Forms.Timer();
            _housekeepingTimer.Interval = 5000;
            _housekeepingTimer.Tick += HousekeepingTimer_Tick;
            HousekeepingTimer_Tick(null, null);     // tick anyway enables timer when finished

            _trackerStatus = TrackerStatus.OK;

            if(Properties.Settings.Default.CITrackerPath.Length > 0)
            {
              _trackerStatus = TrackerStatus.UNKNOWN;
              UpdateTrackerSetting();
            }

            _viewModel = new ViewModel(_buddies);
        }

        private void HousekeepingTimer_Tick(object sender, EventArgs e)
        {
            _housekeepingTimer.Enabled = false;

            _buddies.RefreshList();

            if (_lyncClient != null && _lyncClient.State == ClientState.Invalid)
            {
                Trace.WriteLine("LyncFellow: _lyncClient != null && _lyncClient.State == ClientState.Invalid");
                ReleaseLyncClient();
            }
            if (_lyncClient == null)
            {
                try
                {
                    _lyncClient = LyncClient.GetClient();
                }
                catch { }
                if (_lyncClient != null)
                {
                    if (_lyncClient.State != ClientState.Invalid)
                    {
                        _lyncClient.StateChanged += new EventHandler<ClientStateChangedEventArgs>(LyncClient_StateChanged);
                        _lyncClient.ConversationManager.ConversationAdded += new EventHandler<ConversationManagerEventArgs>(ConversationManager_ConversationAdded);
                        HandleSelfContactEventConnection(_lyncClient.State);
                    }
                    else
                    {
                        ReleaseLyncClient();
                    }
                }
            }
            if (_lyncClient != null && DateTime.Now.Subtract(_lyncEventsUpdated).TotalMinutes > 60)
            {
                Trace.WriteLine("LyncFellow: _lyncClient != null && DateTime.Now.Subtract(_lyncEventsUpdated).TotalMinutes > 60");
                HandleSelfContactEventConnection(_lyncClient.State);
            }

            UpdateBuddiesColorBySelfAvailability();

            bool deviceNotFunctioning = _buddies.LastWin32Error == 0x1f;    //ERROR_GEN_FAILURE
            bool lyncValid = IsLyncConnectionValid();
            if (_buddies.Count > 0 && !deviceNotFunctioning && lyncValid)
            {
                _notifyIcon.Text = Application.ProductName;
                _notifyIcon.Icon = Properties.Resources.LyncFellow;
            }
            else
            {
                _notifyIcon.Text = Application.ProductName;
                _notifyIcon.Icon = Properties.Resources.LyncFellowInfo;
                if (_buddies.Count == 0)
                {
                    _notifyIcon.Text += "\r* No USB buddy found.";
                }
                else if (deviceNotFunctioning)
                {
                    _notifyIcon.Text += "\r* Please reconnect USB buddy.";
                }
                if (!lyncValid)
                {
                    _notifyIcon.Text += "\r* No Lync connection.";
                }
            }

            _housekeepingTimer.Enabled = true;
        }

        private void UpdateTrackerSetting()
        {
          if (_watcher == null)
          {
            _watcher = new FileSystemWatcher();
            /* Watch for changes in LastAccess and LastWrite times, and
                the renaming of files or directories. */
            _watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite;

            // Add event handlers.
            _watcher.Changed += new FileSystemEventHandler(OnChanged);
            _watcher.Created += new FileSystemEventHandler(OnChanged);
            _watcher.Deleted += new FileSystemEventHandler(OnChanged);
          }

          // Stop Watching
          _watcher.EnableRaisingEvents = false;

          if (Properties.Settings.Default.CITrackerPath.Length > 0)
          {
            QueryTrackerFile();

            // Begin watching.
            try
            {
              _watcher.Path = Path.GetDirectoryName(Properties.Settings.Default.CITrackerPath);
              _watcher.Filter = Path.GetFileName(Properties.Settings.Default.CITrackerPath);
              _watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
              Console.WriteLine("{0}", ex.ToString());
              _trackerStatus = TrackerStatus.UNKNOWN;
            }
          }
          else
          {
            _trackerStatus = TrackerStatus.OK;
          }
        }

        // Define the event handlers.
        private void OnChanged(object source, FileSystemEventArgs arg)
        {
          QueryTrackerFile();
        }

        private void QueryTrackerFile()
        {
          TrackerStatus newStatus = TrackerStatus.UNKNOWN;
          try
          {
            string path = Properties.Settings.Default.CITrackerPath;
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
              using (var sr = new StreamReader(fs, Encoding.UTF8, true, 1024))
              {
                string lineOfText;
                while ((lineOfText = sr.ReadLine()) != null)
                {
                  newStatus = (lineOfText.IndexOf("Compile OK") != -1) ? TrackerStatus.OK : TrackerStatus.ERROR;
                  Console.WriteLine(lineOfText);
                }
              }
            }
          }
          catch (Exception ex)
          {
            Console.WriteLine("{0}", ex.ToString());
            newStatus = TrackerStatus.UNKNOWN;
          }

          if (_trackerStatus != newStatus)
          {
            _trackerStatus = newStatus;
            _buddies.Heartbeat();
            UpdateBuddiesColorBySelfAvailability();
          }
        }

        private bool IsLyncConnectionValid()
        {
            return _lyncClient != null && _lyncClient.State == ClientState.SignedIn && _lyncClient.Self.Contact != null;
        }

        private void ReleaseLyncClient()
        {
            _lyncClient = null;
            GC.Collect();
        }

        private void HandleSelfContactEventConnection(ClientState State)
        {
            if (State == ClientState.SignedIn || State == ClientState.SigningOut)
            {
                _lyncClient.Self.Contact.ContactInformationChanged -= new EventHandler<ContactInformationChangedEventArgs>(SelfContact_ContactInformationChanged);
                Trace.WriteLine("LyncFellow: _lyncClient.Self.Contact.ContactInformationChanged -= SelfContact_ContactInformationChanged");

                if (State == ClientState.SignedIn)
                {
                    _lyncClient.Self.Contact.ContactInformationChanged += new EventHandler<ContactInformationChangedEventArgs>(SelfContact_ContactInformationChanged);
                    _lyncEventsUpdated = DateTime.Now;
                    Trace.WriteLine("LyncFellow: _lyncClient.Self.Contact.ContactInformationChanged += SelfContact_ContactInformationChanged");
                }
            }
        }

        private void LyncClient_StateChanged(object sender, ClientStateChangedEventArgs e)
        {
            Trace.WriteLine(string.Format("LyncFellow: LyncClient_StateChanged e.OldState={0}, e.NewState={1}, e.StatusCode=0x{2:x}", e.OldState, e.NewState, e.StatusCode));
            HandleSelfContactEventConnection(e.NewState);
            UpdateBuddiesColorBySelfAvailability();
        }

        private void SelfContact_ContactInformationChanged(object sender, ContactInformationChangedEventArgs e)
        {
            foreach (ContactInformationType changedInformationType in e.ChangedContactInformation)
            {
                if (changedInformationType == ContactInformationType.Availability)
                {
                    UpdateBuddiesColorBySelfAvailability();
                }
            }
        }

        private void ConversationManager_ConversationAdded(object sender, ConversationManagerEventArgs e)
        {
            bool IncomingCall = false;
            bool NewConversationIM = false;
            foreach (var Modality in e.Conversation.Modalities)
            {
              if (Modality.Value != null && Modality.Value.State == ModalityState.Notified)
              {
                if (Modality.Key == ModalityTypes.InstantMessage)
                {
                  NewConversationIM = true;
                }
                else if (Modality.Key == ModalityTypes.AudioVideo)
                {
                  IncomingCall = true;
                }
              }
            }

            if (IncomingCall)
            {
              if (Properties.Settings.Default.IncomingCall_DoDance)
                _buddies.Dance(5000);
              if (Properties.Settings.Default.IncomingCall_DoFlapWings)
                _buddies.FlapWings(5000);
              if (Properties.Settings.Default.IncomingCall_DoGlowHeart)
                _buddies.Heartbeat(5000);

              if (Properties.Settings.Default.IncomingCall_DoBlinkSingleColor)
                _buddies.BlinkSingleColor(5000);
              else if (Properties.Settings.Default.IncomingCall_DoBlinkRainbow)
                _buddies.Rainbow(5000);

                //if (e.Conversation.Participants.Count >= 2)
                //{
                //    var Initiator = e.Conversation.Participants[1].Contact;
                //    Trace.WriteLine(string.Format("LyncFellow: Initiator.Uri=\"{0}\"", Initiator.Uri));
                //    // magic heartbeat for incoming conversations from G&K ;-)
                //    if (Initiator.Uri.Contains("glueckkanja")
                //        || Initiator.Uri.Contains("+4969800706")
                //        || Initiator.Uri.Contains("+49711460533")
                //        || Initiator.Uri.Contains("+4940609298")
                //        || Initiator.Uri.Contains("+49151182260"))
                //    {
                //        _buddies.Heartbeat(10000);
                //    }
                //}
            }

            if (NewConversationIM)
            {
              if (Properties.Settings.Default.NewIMConversation_DoDance)
                _buddies.Dance(5000);
              if (Properties.Settings.Default.NewIMConversation_DoFlapWings)
                _buddies.FlapWings(5000);
              if (Properties.Settings.Default.NewIMConversation_DoGlowHeart)
                _buddies.Heartbeat(5000);

              if (Properties.Settings.Default.NewIMConversation_DoBlinkSingleColor)
                _buddies.BlinkSingleColor(5000);
              else if (Properties.Settings.Default.NewIMConversation_DoBlinkRainbow)
                _buddies.Rainbow(5000);
            }
        }

        private void UpdateBuddiesColorBySelfAvailability()
        {
            ContactAvailability Availability = ContactAvailability.None;
            string Activity = "";
            bool lyncValid = IsLyncConnectionValid();
            if (lyncValid)
            {
                Availability = (ContactAvailability)_lyncClient.Self.Contact.GetContactInformation(ContactInformationType.Availability);
                Activity = (string)_lyncClient.Self.Contact.GetContactInformation(ContactInformationType.ActivityId);
            }

            bool RedOnBusy = Properties.Settings.Default.RedOnDndCallBusy == ContactAvailability.Busy;
            bool RedOnCall = RedOnBusy || Properties.Settings.Default.RedOnDndCallBusy == ContactAvailability.None;
            bool InACall = Activity == "on-the-phone" || Activity == "in-a-conference";
            bool Dnd = Availability == ContactAvailability.DoNotDisturb;
            bool Busy = Availability == ContactAvailability.Busy || Availability == ContactAvailability.BusyIdle;
            bool Free = Availability == ContactAvailability.Free || Availability == ContactAvailability.FreeIdle;
            bool Away = Availability == ContactAvailability.Away || Availability == ContactAvailability.TemporarilyAway;

            var colorOld = _buddies.Color;
            var colorNew = iBuddy.Color.Off;
            if (_trackerStatus != TrackerStatus.OK)
            {
              colorNew = (_trackerStatus == TrackerStatus.ERROR) ? iBuddy.Color.Red : iBuddy.Color.Lila;
            }
            else
            {
              if (Dnd || (RedOnCall && InACall) || (RedOnBusy && Busy))
              {
                colorNew = iBuddy.Color.Red;
              }
              else if (Free || Busy)
              {
                colorNew = iBuddy.Color.Green;
              }
              else if (Away)
              {
                colorNew = iBuddy.Color.Yellow;
              }
            }


            _buddies.Color = colorNew;

            Trace.WriteLine(string.Format("LyncFellow: UpdateBuddiesColorBySelfAvailability lyncValid={0}, Availability={1}, Activity=\"{2}\", RedOnBusy={3}, RedOnCall={4}, InACall={5}, Dnd={6}, Busy={7}, Free={8}, Away={9}, colorOld={10}, colorNew={11}",
                lyncValid, Availability, Activity, RedOnBusy, RedOnCall, InACall, Dnd, Busy, Free, Away, colorOld, colorNew));
        }

        private void MenuSettingsItem_Click(object sender, EventArgs e)
        {
            if (_settingsForm == null)
            {
                _settingsForm = new SettingsForm(_viewModel);
                _settingsForm.Closed += settingsForm_Closed;
                _settingsForm.Show();
            }
            else { _settingsForm.Activate(); }
        }

        void settingsForm_Closed(object sender, EventArgs e)
        {
            _settingsForm = null;

            UpdateTrackerSetting();
            UpdateBuddiesColorBySelfAvailability();
        }

        private void MenuExitItem_Click(object sender, EventArgs e)
        {
            ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _components != null) { _components.Dispose(); }
            if (disposing && _buddies != null) { _buddies.Release(); }
        }

        protected override void ExitThreadCore()
        {
            if (_settingsForm != null) { _settingsForm.Close(); }
            _notifyIcon.Visible = false;
            base.ExitThreadCore();
        }

    }

}
