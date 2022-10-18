﻿using Steam_Account_Manager.Infrastructure;
using System;

namespace Steam_Account_Manager.ViewModels
{
    internal class SettingsViewModel : ObservableObject
    {
        private bool[] _themeMode  = { false, false, false };
        private bool[] _localeMode = { false, false, false };

        bool _autoCloseMode,
              _noConfirmMode,
              _takeAccountInfoMode,
              _passwordEnabled,
              _autoGetSteamId,
              _apiKeyError,
              _passwordError,
              _rememberPassword,
              _minimizeToTray;

        string _webApiKey,
               _encryptingKey,
               _password;

        public RelayCommand SaveChangesCommand { get; set; }
        public RelayCommand OpenApiKeyUrlCommand { get; set; }
        public RelayCommand GenerateCryptoKeyCommand { get; set; }
        public RelayCommand ResetCryptoKeyCommand { get; set; }
        public RelayCommand ChangeOrAddPasswordCommand { get; set; }

        #region Properties

        public bool MinimizeToTray
        {
            get => _minimizeToTray;
            set
            {
                _minimizeToTray = value;
                OnPropertyChanged(nameof(MinimizeToTray));
            }
        }
        public bool RememberPassword
        {
            get => _rememberPassword;
            set
            {
                _rememberPassword = value;
                OnPropertyChanged(nameof(RememberPassword));
            }

        }
        public bool AutoGetSteamId
        {
            get => _autoGetSteamId;
            set
            {
                _autoGetSteamId = value;
                OnPropertyChanged(nameof(AutoGetSteamId));
            }
        }

        public bool PasswordError
        {
            get => _passwordError;
            set
            {
                _passwordError = value;
                OnPropertyChanged(nameof(PasswordError));
            }
        }
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }
        public bool PasswordEnabled
        {
            get => _passwordEnabled;
            set
            {
                _passwordEnabled = value;
                OnPropertyChanged(nameof(PasswordEnabled));
            }
        }

        public string EncryptingKey
        {
            get => _encryptingKey;
            set
            {
                _encryptingKey = value;
                OnPropertyChanged(nameof(EncryptingKey));
            }
        }
        public bool ApiKeyError
        {
            get => _apiKeyError;
            set
            {
                _apiKeyError = value;
                OnPropertyChanged(nameof(ApiKeyError));
            }
        }
        public string WebApiKey
        {
            get => _webApiKey;
            set
            {
                _webApiKey = value;
                OnPropertyChanged(nameof(WebApiKey));
            }
        }
        public bool TakeAccountInfoMode
        {
            get => _takeAccountInfoMode;
            set
            {
                _takeAccountInfoMode = value;
                OnPropertyChanged(nameof(TakeAccountInfoMode));
            }
        }

        public bool[] LocaleMode
        {
            get => _localeMode;
            set
            {
                _localeMode = value;
                OnPropertyChanged();
            }
        }

        public bool[] ThemeMode
        {
            get => _themeMode;
            set
            {
                _themeMode = value;
                OnPropertyChanged();
            }
        }

        public bool AutoCloseMode
        {
            get => _autoCloseMode;
            set
            {
                _autoCloseMode = value;
                OnPropertyChanged(nameof(AutoCloseMode));
            }
        }

        public bool NoConfirmMode
        {
            get => _noConfirmMode;
            set
            {
                _noConfirmMode = value;
                OnPropertyChanged(nameof(NoConfirmMode));
            }
        } 
        #endregion

        private static bool? OpenAuthWindow()
        {
            var authenticationWindow = new View.AuthenticationWindow(false)
            {
                Owner = App.Current.MainWindow,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
            };
            return authenticationWindow.ShowDialog();
        }

        public SettingsViewModel()
        {
            NoConfirmMode       = Config.Properties.NoConfirmMode;
            AutoCloseMode       = Config.Properties.AutoClose;
            TakeAccountInfoMode = Config.Properties.TakeAccountInfo;
            WebApiKey           = Config.Properties.WebApiKey;
            AutoGetSteamId      = Config.Properties.AutoGetSteamId;
            RememberPassword    = Config.Properties.RememberPassword;
            MinimizeToTray      = Config.Properties.MinimizeToTray;

            LocaleMode[(byte)Config.Properties.Language] = true;
            ThemeMode[(byte)Config.Properties.Theme]     = true;

            if (!String.IsNullOrEmpty(Config.Properties.Password))
                _passwordEnabled = true;
            
            EncryptingKey = Config.Properties.UserCryptoKey == Config.GetDefaultCryptoKey ? "By default" : Config.Properties.UserCryptoKey;


            SaveChangesCommand = new RelayCommand(o =>
            {

                if(!String.IsNullOrEmpty(WebApiKey) && WebApiKey.Length < 32)
                {
                    ApiKeyError = true;
                }
                else if(!String.IsNullOrEmpty(Password) && ( Password.Length < 5 || Password.Length > 30))
                {
                    PasswordError = true;
                }
                else
                {
                    byte index;
                    if ((byte)Config.Properties.Theme != (index = (byte)Array.FindIndex(ThemeMode, theme => theme)))
                    {
                        Config.Properties.Theme = (Infrastructure.Models.Themes)index;
                    }

                    if((byte)Config.Properties.Language != (index = (byte)Array.FindIndex(LocaleMode, locale => locale)))
                    {
                        Config.Properties.Language = (Infrastructure.Models.Languages)index;
                    }


                    Config.Properties.NoConfirmMode    = NoConfirmMode;
                    Config.Properties.AutoClose        = AutoCloseMode;
                    Config.Properties.TakeAccountInfo  = TakeAccountInfoMode;
                    Config.Properties.WebApiKey        = WebApiKey;
                    Config.Properties.AutoGetSteamId   = AutoGetSteamId;
                    Config.Properties.RememberPassword = RememberPassword;
                    Config.Properties.MinimizeToTray   = MinimizeToTray;

                    if (!String.IsNullOrEmpty(Password))
                        Config.Properties.Password = Utilities.Sha256(Password + Config.GetDefaultCryptoKey);
                    else if(_passwordEnabled == false)
                        Config.Properties.Password = null;

                    if(EncryptingKey != "By default")
                    {
                        Config.Properties.UserCryptoKey = EncryptingKey;
                        if (System.IO.File.Exists("database.dat"))
                            Config.SaveAccounts();
                    }
                    else
                    {
                        Config.Properties.UserCryptoKey = Config.GetDefaultCryptoKey;
                        if (System.IO.File.Exists("database.dat"))
                            Config.SaveAccounts();
                        EncryptingKey = "By default";
                    }

                    Config.SaveProperties();

                    ApiKeyError = PasswordError = false;
                    Themes.Animations.ShakingAnimation(o as System.Windows.FrameworkElement, true);
                }

            });
            
            OpenApiKeyUrlCommand = new RelayCommand(o =>
            {
                using (System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://steamcommunity.com/dev/apikey")
                {
                    UseShellExecute = true
                })) {; }
            });

            GenerateCryptoKeyCommand = new RelayCommand(o =>
            {
                EncryptingKey = Utilities.GenerateCryptoKey();
            });

            ResetCryptoKeyCommand = new RelayCommand(o =>
            {
                EncryptingKey = "By default";
            });

            ChangeOrAddPasswordCommand = new RelayCommand(o =>
            {
                System.Windows.Controls.StackPanel stackPanel = (System.Windows.Controls.StackPanel)o;

                if (Config.Properties.Password == null)
                {
                    if (PasswordEnabled)
                    {
                        PasswordEnabled = false;
                        stackPanel.Visibility = System.Windows.Visibility.Hidden;
                    }
                    else
                    {
                        PasswordEnabled = true;
                        stackPanel.Visibility = System.Windows.Visibility.Visible;
                    }
                }
                else
                {
                    PasswordEnabled = true;
                    if (OpenAuthWindow() == true)
                    {
                        PasswordEnabled = false;
                        stackPanel.Visibility = System.Windows.Visibility.Visible;
                    }
                }
            });
        }
    }
}
