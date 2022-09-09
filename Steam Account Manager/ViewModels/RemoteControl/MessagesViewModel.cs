﻿using Steam_Account_Manager.Infrastructure;
using Steam_Account_Manager.Infrastructure.Base;
using Steam_Account_Manager.Infrastructure.Validators;
using System;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace Steam_Account_Manager.ViewModels.RemoteControl
{
    internal class Message
    {
        public string Msg { get; set; }
        public string Time { get; set; }
        public string Username { get; set; }
        public Brush MsgBrush { get; set; }
        public Brush TextBrush { get; set; }
    }
    internal class MessagesViewModel : ObservableObject
    {
        public RelayCommand SelectChatCommand { get; set; }
        public RelayCommand LeaveFromChatCommand { get; set; }
        public RelayCommand SendMessageCommand { get; set; }
        public RelayCommand AddAdminIdCommand { get; set; }
        private string TempID, TempAdminID;

        #region Properties
        private string _message;
        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged(nameof(Message));
            }
        }

        private string _errorMsg;
        public string ErrorMsg
        {
            get => _errorMsg;
            set
            {
                _errorMsg = value;
                OnPropertyChanged(nameof(ErrorMsg));
            }
        }

        private string _interlocutorId;
        public string InterlocutorId
        {
            get => _interlocutorId;
            set
            {
                _interlocutorId = value;
                OnPropertyChanged(nameof(InterlocutorId));

            }
        }

        private uint _chatId;
        public uint ChatId
        {
            get => _chatId;
            set
            {
                _chatId = value;
                OnPropertyChanged(nameof(ChatId));
            }
        }

        private bool _isChatSelected;
        public bool IsChatSelected
        {
            get => _isChatSelected;
            set
            {
                _isChatSelected = value;
                OnPropertyChanged(nameof(IsChatSelected));
            }
        }
        #endregion

        #region Handlers
        private static ObservableCollection<Message> _messages;
        public static event EventHandler MessagesChanged;
        public static ObservableCollection<Message> Messages
        {
            get => _messages;
            set
            {
                _messages = value;
                MessagesChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static event EventHandler SaveChatLogChanged;
        public static bool SaveChatLog
        {
            get => SteamRemoteClient.CurrentUser.Messenger.SaveChatLog;
            set
            {
                SteamRemoteClient.CurrentUser.Messenger.SaveChatLog = value;
                SaveChatLogChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static event EventHandler EnableAdminCommandsChanged;
        public static bool EnableAdminCommands
        {
            get => SteamRemoteClient.CurrentUser.Messenger.EnableAdminCommands;
            set
            {
                SteamRemoteClient.CurrentUser.Messenger.EnableAdminCommands = value;
                EnableAdminCommandsChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static event EventHandler EnablePublicCommandsChanged;
        public static bool EnablePublicCommands
        {
            get => SteamRemoteClient.CurrentUser.Messenger.EnablePublicCommands;
            set
            {
                SteamRemoteClient.CurrentUser.Messenger.EnablePublicCommands = value;
                EnablePublicCommandsChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        private static string _adminId;
        public static event EventHandler AdminIdChanged;
        public static string AdminId
        {
            get => _adminId;
            set
            {
                _adminId = value;
                AdminIdChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        private static ObservableCollection<Command> _msgCommands;
        public static event EventHandler MsgCommandsChanged;
        public static ObservableCollection<Command> MsgCommands
        {
            get => _msgCommands;
            set
            {
                _msgCommands = value;
                MsgCommandsChanged?.Invoke(null, EventArgs.Empty);
            }
        } 
        #endregion


        private static void InitDefaultCommands()
        {
            MsgCommands.Insert(0, new Command
            {
                Keyword = "/help",
                CommandExecution = "List of available commands",
                IsPublic = true,
                MessageAfterExecute = "-"
            });

            MsgCommands.Insert(1, new Command
            {
                Keyword = "/shutdown",
                CommandExecution = "Turns off the app",
                IsPublic = false,
                MessageAfterExecute = "App has been closed."
            });

            MsgCommands.Insert(2, new Command
            {
                Keyword = "/pcsleep",
                CommandExecution = "Sends the PC to sleep",
                IsPublic = false,
                MessageAfterExecute = "Sleeping mode..."
            });

            MsgCommands.Insert(3, new Command
            {
                Keyword = "/pcshutdown",
                CommandExecution = "Turns off the computer",
                IsPublic = false,
                MessageAfterExecute = "Shutting down..."
            });

            MsgCommands.Insert(4, new Command
            {
                Keyword = "/msg {id}",
                CommandExecution = "Sends a message to a friend",
                IsPublic = false,
                MessageAfterExecute = "-"
            });

            MsgCommands.Insert(5, new Command
            {
                Keyword = "/idle [GamesIds]",
                CommandExecution = "Launches games from the library",
                IsPublic = false,
                MessageAfterExecute = "Idling..."
            });

            MsgCommands.Insert(6, new Command
            {
                Keyword = "/customgame {title}",
                CommandExecution = "Sets a custom title as a game",
                IsPublic = false,
                MessageAfterExecute = "Title setted"
            });

            MsgCommands.Insert(7, new Command
            {
                Keyword = "/stopgame",
                CommandExecution = "Stops game activity",
                IsPublic = false,
                MessageAfterExecute = "Game activity stopped."
            });
        }
        public MessagesViewModel()
        {
            Messages = new ObservableCollection<Message>();
            MsgCommands = new ObservableCollection<Command>();
            InitDefaultCommands();

            SelectChatCommand = new RelayCommand(o =>
            {
                if (!string.IsNullOrEmpty(ErrorMsg))
                    ErrorMsg = "";
                if (TempID != InterlocutorId)
                {
                    TempID = InterlocutorId;
                    SteamValidator steamValidator = new SteamValidator(InterlocutorId);
                    if (steamValidator.GetSteamLinkType() != SteamValidator.SteamLinkTypes.ErrorType)
                    {
                        ChatId = steamValidator.SteamId32;
                        SteamRemoteClient.InterlocutorID = SteamRemoteClient.FindFriendFromSteamID(ChatId);
                        IsChatSelected = true;
                        InterlocutorId = "";
                        if (Messages.Count != 0)
                            Messages.Clear();
                    }
                    else
                    {
                        ErrorMsg = "Invalid ID";
                    }
                }
              
            });

            LeaveFromChatCommand = new RelayCommand(o =>
            {
                IsChatSelected = false;
                TempID = "";
                if (Messages.Count != 0)
                    Messages.Clear();
            });

            SendMessageCommand = new RelayCommand(o =>
            {
                if (!String.IsNullOrEmpty(Message))
                {
                    SteamRemoteClient.SendInterlocutorMessage(Message);
                    Message = "";
                }
            });

            AddAdminIdCommand = new RelayCommand(o =>
            {
                if (!string.IsNullOrEmpty(ErrorMsg))
                    ErrorMsg = "";
                if(TempAdminID != AdminId)
                {
                    TempAdminID = AdminId;
                    SteamValidator steamValidator = new SteamValidator(TempAdminID);
                    if (steamValidator.GetSteamLinkType() != SteamValidator.SteamLinkTypes.ErrorType)
                    {
                        SteamRemoteClient.CurrentUser.Messenger.AdminID = steamValidator.SteamId32;
                    }
                    else
                    {
                        ErrorMsg = "Invalid ID";
                    }
                }
            });
        }
    }
}
