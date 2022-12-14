using Microsoft.Win32;
using Steam_Account_Manager.Infrastructure;
using Steam_Account_Manager.Infrastructure.Models.AccountModel;
using Steam_Account_Manager.Infrastructure.Parsers;
using Steam_Account_Manager.ViewModels.View;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Steam_Account_Manager.ViewModels
{
    internal sealed class AccountDataViewModel : ObservableObject
    {
        public RelayCommand CancelCommand { get; set; }
        public RelayCommand CopyCommand { get; set; }
        public RelayCommand OpenUrlProfileCommand { get; set; }
        public RelayCommand OpenFromIdLinkCommand { get; set; }
        public AsyncRelayCommand TakeCsgoStatsInfo { get; set; }
        public RelayCommand SaveChangesComamnd { get; set; }
        public RelayCommand OpenOtherLinksCommand { get; set; }
        public RelayCommand ExportAccountCommand { get; set; }
        public RelayCommand AddAuthenticatorCommand { get; set; }
        public RelayCommand YesAuthenticatorCommand { get; set; }
        public AsyncRelayCommand RefreshCommand { get; set; }

        private Account currentAccount;

        private string _avatarFull, _nickname;
        private bool _noticeView = false, _savePermission = false, _isCsgoStatsSave = false;
        private string _steamLevel, _steamURL, _steamId64;
        private ulong _steaId32;
        private string _login, _password;
        private DateTime _createdDate, _lastUpdateTime;
        private string _profileVisiblity;
        private string _steamYearPicture;
        private string _gameCountPicture, _createdDatePicture;
        private bool _containParseInfo;
        private int _id;
        private string _authenticatorPath;

        //bans
        private int _vacCount;
        private uint _daysSinceLastBan;
        private bool _tradeBan, _communityBan;

        //games
        private string _gamesTotal, _gamesPlayed, _hoursOnPlayed;
        private string _playedPercent;

        //csgostats
        private string _currentRank, _bestRank,
            _kills, _deaths, _KD, _playedMatches, _matchesWon, _winrate, _totalShots, _headshots,
            _headshotsPercent, _shotsHit, _accuracy, _roundsPlayed;

        //eror and notify msgs
        private string _csgoParseError, _steamDataValidateError, _notifyMsg;

        //other account info
        private string _note, _emailLogin, _emailPass, _rockstarEmail, _rockstarPass, _uplayEmail, _uplayPass, _originEmail, _originPass;

        #region Properties

        public DateTime LastUpdateTime
        {
            get => _lastUpdateTime;
            set
            {
                _lastUpdateTime = value;
                OnPropertyChanged(nameof(LastUpdateTime));
            }
        }
        public string AuthenticatorPath
        {
            get => _authenticatorPath;
            set
            {
                _authenticatorPath = value;
                OnPropertyChanged(nameof(AuthenticatorPath));
            }
        }
        public bool ContainParseInfo
        {
            get => _containParseInfo;
            set
            {
                _containParseInfo = value;
                OnPropertyChanged(nameof(ContainParseInfo));
            }
        }
        public string Note
        {
            get => _note;
            set
            {
                _note = value;
                OnPropertyChanged(nameof(Note));
            }
        }
        public string EmailLogin
        {
            get => _emailLogin;
            set
            {
                _emailLogin = value;
                OnPropertyChanged(nameof(EmailLogin));
            }
        }
        public string EmailPass
        {
            get => _emailPass;
            set
            {
                _emailPass = value;
                OnPropertyChanged(nameof(EmailPass));
            }
        }
        public string RockstarEmail
        {
            get => _rockstarEmail;
            set
            {
                _rockstarEmail = value;
                OnPropertyChanged(nameof(RockstarEmail));
            }
        }
        public string RockstarPass
        {
            get => _rockstarPass;
            set
            {
                _rockstarPass = value;
                OnPropertyChanged(nameof(RockstarPass));
            }
        }
        public string UplayEmail
        {
            get => _uplayEmail;
            set
            {
                _uplayEmail = value;
                OnPropertyChanged(nameof(UplayEmail));
            }
        }
        public string UplayPass
        {
            get => _uplayPass;
            set
            {
                _uplayPass = value;
                OnPropertyChanged(nameof(UplayPass));
            }
        }

        public string OriginEmail
        {
            get => _originEmail;
            set
            {
                _originEmail = value;
                OnPropertyChanged(nameof(OriginEmail));
            }
        }

        public string OriginPass
        {
            get => _originPass;
            set
            {
                _originPass = value;
                OnPropertyChanged(nameof(OriginPass));
            }
        }

        public string NotifyMsg
        {
            get => _notifyMsg;
            set
            {
                _notifyMsg = value;
                OnPropertyChanged(nameof(NotifyMsg));
            }
        }
        public string SteamDataValidateError
        {
            get => _steamDataValidateError;
            set
            {
                _steamDataValidateError = value;
                OnPropertyChanged(nameof(SteamDataValidateError));
            }
        }
        public string CsgoParseError
        {
            get => _csgoParseError;
            set
            {
                _csgoParseError = value;
                OnPropertyChanged(nameof(CsgoParseError));
            }
        }
        public string PlayedPercent
        {
            get => _playedPercent;
            set
            {
                _playedPercent = value;
            }
        }
        public string GamesTotal
        {
            get => _gamesTotal;
            set
            {
                _gamesTotal = value;
            }
        }
        public string GamesPlayed
        {
            get => _gamesPlayed;
            set
            {
                _gamesPlayed = value;
            }
        }
        public string HoursOnPlayed
        {
            get => _hoursOnPlayed;
            set
            {
                _hoursOnPlayed = value;
            }
        }

        public uint DaysSinceLastBan
        {
            get => _daysSinceLastBan;
            set
            {
                _daysSinceLastBan = value;
            }
        }
        public bool TradeBan
        {
            get => _tradeBan;
            set
            {
                _tradeBan = value;
                OnPropertyChanged(nameof(TradeBan));
            }
        }
        public bool CommunityBan
        {
            get => _communityBan;
            set
            {
                _communityBan = value;
                OnPropertyChanged(nameof(CommunityBan));
            }
        }

        public int VacCount
        {
            get => _vacCount;
            set
            {
                _vacCount = value;
                OnPropertyChanged(nameof(VacCount));
            }
        }

        public string GameCountPicture
        {
            get => _gameCountPicture;
            set
            {
                _gameCountPicture = value;
                OnPropertyChanged(nameof(GameCountPicture));
            }
        }

        public string CreatedDatePicuture
        {
            get => _createdDatePicture;
            set
            {
                _createdDatePicture = value;
                OnPropertyChanged(nameof(CreatedDatePicuture));
            }
        }
        public string ProfileVisiblity
        {
            get => _profileVisiblity;
            set
            {
                _profileVisiblity = value;
                OnPropertyChanged(nameof(ProfileVisiblity));
            }
        }
        public string SteamYearPicture
        {
            get => _steamYearPicture;
            set
            {
                _steamYearPicture = value;
                OnPropertyChanged(nameof(SteamYearPicture));
            }
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set
            {
                _createdDate = value;
                OnPropertyChanged(nameof(CreatedDate));
            }
        }
        public string Login
        {
            get => _login;
            set
            {
                _login = value;
                OnPropertyChanged(nameof(Login));
            }
        }
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                if (!_savePermission) _savePermission = true;
            }
        }
        public ulong SteamID32
        {
            get => _steaId32;
            set
            {
                _steaId32 = value;
                OnPropertyChanged(nameof(SteamID32));
            }
        }
        public string SteamID64
        {
            get => _steamId64;
            set
            {
                _steamId64 = value;
                OnPropertyChanged(nameof(SteamID64));
            }
        }
        public string SteamURL
        {
            get => _steamURL;
            set
            {
                _steamURL = value;
                OnPropertyChanged(nameof(SteamURL));
            }
        }
        public string SteamLevel
        {
            get => _steamLevel;
            set
            {
                _steamLevel = value;
                OnPropertyChanged(nameof(SteamLevel));
            }
        }
        public bool NoticeView
        {
            get => _noticeView;
            set
            {
                _noticeView = value;
                OnPropertyChanged(nameof(NoticeView));
            }
        }
        public string AvatarFull
        {
            get => _avatarFull;
            set
            {
                _avatarFull = value;
                OnPropertyChanged(nameof(AvatarFull));
            }
        }
        public string Nickname
        {
            get => _nickname;
            set
            {
                _nickname = value;
                OnPropertyChanged(nameof(Nickname));
            }
        }
        #endregion

        #region CsGo Statistics
        public string CurrentRank
        {
            get => _currentRank;
            set
            {
                _currentRank = value;
                OnPropertyChanged(nameof(CurrentRank));
            }
        }
        public string BestRank
        {
            get => _bestRank;
            set
            {
                _bestRank = value;
                OnPropertyChanged(nameof(BestRank));
            }
        }
        public string Kills
        {
            get => _kills;
            set
            {
                _kills = value;
                OnPropertyChanged(nameof(Kills));
            }
        }
        public string Deaths
        {
            get => _deaths;
            set
            {
                _deaths = value;
                OnPropertyChanged(nameof(Deaths));
            }
        }
        public string KD
        {
            get => _KD;
            set
            {
                _KD = value;
                OnPropertyChanged(nameof(KD));
            }
        }
        public string PlayedMatches
        {
            get => _playedMatches;
            set
            {
                _playedMatches = value;
                OnPropertyChanged(nameof(PlayedMatches));
            }
        }
        public string MatchesWon
        {
            get => _matchesWon;
            set
            {
                _matchesWon = value;
                OnPropertyChanged(nameof(MatchesWon));
            }
        }
        public string Winrate
        {
            get => _winrate;
            set
            {
                _winrate = value;
                OnPropertyChanged(nameof(Winrate));
            }
        }
        public string TotalShots
        {
            get => _totalShots;
            set
            {
                _totalShots = value;
                OnPropertyChanged(nameof(TotalShots));
            }
        }
        public string Headshots
        {
            get => _headshots;
            set
            {
                _headshots = value;
                OnPropertyChanged(nameof(Headshots));
            }
        }
        public string HeadshotsPercent
        {
            get => _headshotsPercent;
            set
            {
                _headshotsPercent = value;
                OnPropertyChanged(nameof(HeadshotsPercent));
            }
        }
        public string ShotsHit
        {
            get => _shotsHit;
            set
            {
                _shotsHit = value;
                OnPropertyChanged(nameof(ShotsHit));
            }
        }
        public string Accuracy
        {
            get => _accuracy;
            set
            {
                _accuracy = value;
                OnPropertyChanged(nameof(Accuracy));
            }
        }
        public string RoundsPlayed
        {
            get => _roundsPlayed;
            set
            {
                _roundsPlayed = value;
                OnPropertyChanged(nameof(RoundsPlayed));
            }
        }
        #endregion

        private void FillSteamInfo()
        {
            Nickname = currentAccount.Nickname;
            Login = currentAccount.Login;
            Password = currentAccount.Password;
            ContainParseInfo = currentAccount.ContainParseInfo;
            LastUpdateTime = currentAccount.LastUpdateTime;

            if (currentAccount.ContainParseInfo)
            {
                //Player summaries
                AvatarFull = currentAccount.AvatarFull;
                SteamID32 = ulong.Parse(currentAccount.SteamId64) - 76561197960265728;
                SteamID64 = currentAccount.SteamId64;
                SteamURL = currentAccount.ProfileURL;
                CreatedDate = currentAccount.AccCreatedDate;
                ProfileVisiblity = currentAccount.ProfileVisility == true ? "Public" : "Private";
                CreatedDatePicuture = currentAccount.CreatedDateImageUrl;

                //Games info
                SteamLevel = currentAccount.SteamLevel;
                GameCountPicture = currentAccount.CountGamesImageUrl;
                GamesTotal = currentAccount.TotalGames;
                GamesPlayed = currentAccount.GamesPlayed;
                HoursOnPlayed = currentAccount.HoursOnPlayed;
                PlayedPercent = currentAccount.ProfileVisility == true ? " (" + (float.Parse(GamesPlayed.Replace(",", string.Empty)) / float.Parse(GamesTotal.Replace(",", String.Empty)) * 100).ToString("#.#") + "%)" : "-";

                //Bans info
                VacCount = currentAccount.VacBansCount;
                CommunityBan = currentAccount.CommunityBan;
                TradeBan = currentAccount.TradeBan;
                DaysSinceLastBan = currentAccount.DaysSinceLastBan;
            }
            else
            {
                AvatarFull = "/Images/default_steam_profile.png";
                ProfileVisiblity = SteamURL = SteamID64 = "Unknown";
                GameCountPicture = CreatedDatePicuture = "/Images/Steam_years_of_service/year0.png";
                SteamLevel = "-";
                SteamID32 = 0;
            }

            //Other info
            Note = currentAccount.Note;
            EmailLogin = currentAccount.EmailLogin;
            EmailPass = currentAccount.EmailPass;
            RockstarEmail = currentAccount.RockstarEmail;
            RockstarPass = currentAccount.RockstarPass;
            UplayEmail = currentAccount.UplayEmail;
            UplayPass = currentAccount.UplayPass;
            OriginPass = currentAccount.OriginPass;
            OriginEmail = currentAccount.OriginEmail;


        }
        private void FillCsgoInfo()
        {
            //csgo info
            CurrentRank = currentAccount.CsgoStats.CurrentRank;
            BestRank = currentAccount.CsgoStats.BestRank;
            Kills = currentAccount.CsgoStats.Kills;
            Deaths = currentAccount.CsgoStats.Deaths;
            KD = currentAccount.CsgoStats.KD;
            PlayedMatches = currentAccount.CsgoStats.PlayedMatches;
            MatchesWon = currentAccount.CsgoStats.MatchesWon;
            Winrate = currentAccount.CsgoStats.Winrate;
            TotalShots = currentAccount.CsgoStats.TotalShots;
            Headshots = currentAccount.CsgoStats.Headshots;
            HeadshotsPercent = currentAccount.CsgoStats.HeadshotPercent;
            ShotsHit = currentAccount.CsgoStats.ShotsHit;
            Accuracy = currentAccount.CsgoStats.Accuracy;
            RoundsPlayed = currentAccount.CsgoStats.RoundsPlayed;
        }

        private async void BorderNoticeView(string message)
        {
            NotifyMsg = message;
            NoticeView = true;
            await Task.Delay(2000).ConfigureAwait(false);
            NoticeView = false;
        }

        private async Task CsgoStatsParse()
        {
            await Task.Factory.StartNew(() =>
            {
                try
                {
                    var csgo_parser = new CsgoParser(_steamId64);
                    CsgoParseError = (string)Application.Current.FindResource("adat_cs_inf_takeStats");
                    csgo_parser.GlobalStatsParse().GetAwaiter().GetResult();
                    CsgoParseError = (string)Application.Current.FindResource("adat_cs_inf_takeRank");
                    csgo_parser.RankParse();

                    currentAccount.CsgoStats = csgo_parser.GetCsgoStats;
                    FillCsgoInfo();

                    _isCsgoStatsSave = true;
                    if (!_savePermission) _savePermission = true;
                    CsgoParseError = (string)Application.Current.FindResource("adat_cs_inf_updSucces");
                    Thread.Sleep(2000);
                    CsgoParseError = "";
                }
                catch
                {
                    CsgoParseError = (string)Application.Current.FindResource("adat_cs_inf_serverError");
                    Thread.Sleep(2000);
                    CsgoParseError = "";
                }
            });
        }

        private async Task RefreshAccount(int id)
        {
            await Task.Factory.StartNew(() =>
            {
                try
                {
                    currentAccount = new Account(
                        currentAccount.Login,
                        currentAccount.Password,
                        currentAccount.SteamId64,
                        currentAccount.Note,
                        currentAccount.EmailLogin,
                        currentAccount.EmailPass,
                        currentAccount.RockstarEmail,
                        currentAccount.RockstarPass,
                        currentAccount.UplayEmail,
                        currentAccount.UplayPass,
                        currentAccount.OriginEmail,
                        currentAccount.OriginPass,
                        currentAccount.CsgoStats,
                        currentAccount.AuthenticatorPath,
                        currentAccount.Nickname
                        );

                    Config.Accounts[id] = currentAccount;
                    Config.SaveAccounts();
                    Task.Run(() => BorderNoticeView((string)Application.Current.FindResource("adat_cs_inf_updated")));
                    FillSteamInfo();
                }
                catch
                {
                    Task.Run(() => BorderNoticeView((string)Application.Current.FindResource("adat_cs_inf_noInternet")));
                }

            });
        }

        private void OpenAddAuthenticatorWindow()
        {
            ShowDialogWindow(new AddAuthenticatorWindow(Login, Password, _id));
        }

        public void OpenShowAuthenticatorWindow()
        {
            ShowDialogWindow(new ShowAuthenticatorWindow(_id));
        }


        public AccountDataViewModel(int id)
        {
            currentAccount = Config.Accounts[id];
            FillSteamInfo();
            if (currentAccount.ContainParseInfo) FillCsgoInfo();
            _id = id;

            if (currentAccount.AuthenticatorPath != null) AuthenticatorPath = currentAccount.AuthenticatorPath;

            CancelCommand = new RelayCommand(o =>
            {
                MainWindowViewModel.AccountsViewCommand.Execute(true);
                AccountsViewModel.UpdateAccountTabView(id);
            });

            CopyCommand = new RelayCommand(o =>
            {
                var box = o as TextBox;
                box.SelectAll();
                box.Copy();
                Task.Run(() => BorderNoticeView((string)Application.Current.FindResource("adat_notif_copiedClipoard"))).ConfigureAwait(false);
            });

            OpenUrlProfileCommand = new RelayCommand(o =>
            {
                using (Process.Start(new ProcessStartInfo(SteamURL) { UseShellExecute = true })) { };
            });

            TakeCsgoStatsInfo = new AsyncRelayCommand(async (o) =>
            {
                if (currentAccount.ProfileVisility)
                    await CsgoStatsParse();
            });

            SaveChangesComamnd = new RelayCommand(o =>
            {
                if (!_isCsgoStatsSave && (!_savePermission || (Password == currentAccount.Password &&
                Login == currentAccount.Login && Note == currentAccount.Note && EmailLogin == currentAccount.EmailLogin &&
                RockstarEmail == currentAccount.RockstarEmail && RockstarPass == currentAccount.RockstarPass &&
                UplayEmail == currentAccount.UplayEmail && UplayPass == currentAccount.UplayPass &&
                 OriginEmail == currentAccount.OriginEmail && OriginPass == currentAccount.OriginPass))) { }

                else if (Login == "")
                {
                    SteamDataValidateError = (string)Application.Current.FindResource("adv_error_login_empty");
                }

                else if (Login.Contains(" "))
                {
                    SteamDataValidateError = (string)Application.Current.FindResource("adv_error_login_contain_spaces");
                }

                else if (Login.Length < 3)
                {
                    SteamDataValidateError = (string)Application.Current.FindResource("adv_error_login_shortage");
                }

                else if (Login.Length > 32)
                {
                    SteamDataValidateError = (string)Application.Current.FindResource("adv_error_login_overflow");
                }

                else if (Password == "")
                {
                    SteamDataValidateError = (string)Application.Current.FindResource("adv_error_pass_empty");
                }

                else if (Password.Length < 8)
                {
                    SteamDataValidateError = (string)Application.Current.FindResource("adv_error_pass_shortage");
                }

                else if (Password.Length > 50)
                {
                    SteamDataValidateError = (string)Application.Current.FindResource("adv_error_pass_overflow");
                }
                else
                {
                    _savePermission = false;
                    if (_isCsgoStatsSave)
                    {
                        Config.Accounts[id].CsgoStats = currentAccount.CsgoStats;
                        _isCsgoStatsSave = false;
                    }
                    Config.Accounts[id].Password = Password;
                    Config.Accounts[id].Login = Login;
                    Config.Accounts[id].Note = Note;
                    Config.Accounts[id].EmailLogin = EmailLogin;
                    Config.Accounts[id].EmailPass = EmailPass;
                    Config.Accounts[id].RockstarEmail = RockstarEmail;
                    Config.Accounts[id].RockstarPass = RockstarPass;
                    Config.Accounts[id].UplayEmail = UplayEmail;
                    Config.Accounts[id].UplayPass = UplayPass;
                    Config.Accounts[id].OriginEmail = OriginEmail;
                    Config.Accounts[id].OriginPass = OriginPass;
                    Config.Accounts[id].AccCreatedDate = DateTime.Now;
                    Config.SaveAccounts();
                    Task.Run(() => BorderNoticeView((string)Application.Current.FindResource("adat_notif_changesSaved")));

                }

            });

            RefreshCommand = new AsyncRelayCommand(async (o) => await RefreshAccount(id));

            OpenOtherLinksCommand = new RelayCommand(o =>
            {
                using (Process.Start(new ProcessStartInfo(SteamURL + (string)o) { UseShellExecute = true })) { };
            });

            OpenFromIdLinkCommand = new RelayCommand(o =>
            {
                using (Process.Start(new ProcessStartInfo((string)o + currentAccount.SteamId64) { UseShellExecute = true })) { };
            });

            ExportAccountCommand = new RelayCommand(o =>
            {
                var fileDialog = new SaveFileDialog
                {
                    Filter = "Steam Account (.sa)|*.sa"
                };
                if (fileDialog.ShowDialog() == true)
                {
                    Config.Serialize(Config.Accounts[id], fileDialog.FileName, Config.Properties.UserCryptoKey);
                    Task.Run(() => BorderNoticeView((string)Application.Current.FindResource("adat_notif_accountExported")));
                }
            });

            AddAuthenticatorCommand = new RelayCommand(o =>
            {
                AuthenticatorPath = Config.Accounts[_id].AuthenticatorPath;
                if (AuthenticatorPath == null || !System.IO.File.Exists(AuthenticatorPath))
                {
                    var border = o as Border;
                    border.Visibility = Visibility.Visible;
                }
                else
                {
                    OpenShowAuthenticatorWindow();
                }
            });

            YesAuthenticatorCommand = new RelayCommand(o =>
            {
                OpenAddAuthenticatorWindow();
            });

        }
    }
}
