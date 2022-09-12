﻿using Steam_Account_Manager.Infrastructure;
using Steam_Account_Manager.ViewModels.RemoteControl.View;
using System;

namespace Steam_Account_Manager.ViewModels.RemoteControl
{
    internal class MainRemoteControlViewModel : ObservableObject
    {
        public RelayCommand LoginViewCommand { get; set; }
        public RelayCommand GamesViewCommand { get; set; }
        public RelayCommand MessagesViewCommand { get; set; }
        public RelayCommand FriendsViewCommand { get; set; }

        public static LoginView LoginV;
        public static GamesView GamesV;
        public static FriendsView FriendsV;
        public static MessagesView MessagesV;

        private static bool _isPanelActive;
        public static event EventHandler IsPanelActiveChanged;
        public static bool IsPanelActive
        {
            get => _isPanelActive;
            set
            {
                _isPanelActive = value;
                IsPanelActiveChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        private static object _remoteControlCurrentView;
        public static event EventHandler RemoteControlCurrentViewChanged;
        public static object RemoteControlCurrentView
        {
            get => _remoteControlCurrentView; 
            set
            {
                _remoteControlCurrentView = value;
                RemoteControlCurrentViewChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        

        public MainRemoteControlViewModel()
        {
            LoginV = new LoginView();

               MessagesV = new MessagesView();
            //  CurrentView = MessagesV;

            //  FriendsV = new FriendsView();
            // CurrentView = FriendsV;
            RemoteControlCurrentView = LoginV;

            LoginViewCommand = new RelayCommand(o =>
            {
                if (RemoteControlCurrentView != LoginV)
                    RemoteControlCurrentView = LoginV;
            });

            GamesViewCommand = new RelayCommand(o =>
            {
                if (RemoteControlCurrentView != GamesV)
                {
                    if (GamesV == null)
                        GamesV = new GamesView();

                    RemoteControlCurrentView = GamesV;
                }
            });

            MessagesViewCommand = new RelayCommand(o =>
            {
                if (RemoteControlCurrentView != MessagesV)
                {
                    if (MessagesV == null)
                        MessagesV = new MessagesView();

                    RemoteControlCurrentView = MessagesV;
                }
                    
            });

            FriendsViewCommand = new RelayCommand(o =>
            {
                if (RemoteControlCurrentView != FriendsV)
                {
                    if (FriendsV == null)
                        FriendsV = new FriendsView();

                    RemoteControlCurrentView = FriendsV;
                }
            });
        }
    }
}
