﻿using SteamRemoteClient = Steam_Account_Manager.Infrastructure.Base.SteamRemoteClient;
using Games = Steam_Account_Manager.Infrastructure.Base.Games;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using System.Collections.Generic;

namespace Steam_Account_Manager.ViewModels.RemoteControl.View
{
    public partial class GamesView : UserControl
    {
        private GamesViewModel currentViewModel;
        private static readonly Regex _regex = new Regex("[^0-9.-]+");
        bool Error = false;
        public GamesView()
        {
            InitializeComponent();
            add_idBox.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, PasteBlocked));
            currentViewModel = new GamesViewModel();
            this.DataContext = currentViewModel;
        }

        private void PasteBlocked(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void games_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Error)
            {
                SelectsText.Foreground = Utilities.StringToBrush("#b0b9c6");
                Error = false;
            }

            if (games.SelectedItems.Count == 0)
            {
                SelectsText.Text = "Selected: 0";
            }
            else if (games.SelectedItems.Count == 1)
            {
                SelectsText.Text = "Selected\nAppID: " + currentViewModel.Games[games.SelectedIndex].AppID +
                    "\nPlaytime: " + currentViewModel.Games[games.SelectedIndex].PlayTime_Forever / 60 + "h";
            }
            else
                SelectsText.Text = "Selected: " + games.SelectedItems.Count;
        }

        private void add_idBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = _regex.IsMatch(e.Text);
        }

        private async void Idle_Click(object sender, System.Windows.RoutedEventArgs e)
        {

            if(Idle.IsChecked == true)
            {
                var selectedCount = games.SelectedItems.Count;
                bool ContainCustom = false;
                if (!string.IsNullOrEmpty(Custom_gameTitle.Text))
                {
                    ContainCustom = true;
                    selectedCount++;
                }
                    

                if (selectedCount > 32 || selectedCount == 0)
                {
                    Error = true;
                    SelectsText.Foreground = Utilities.StringToBrush("PaleVioletRed");

                    if (games.SelectedItems.Count > 32)
                        SelectsText.Text = "Maximum\n32 games!";
                    else if (games.SelectedItems.Count == 0)
                        SelectsText.Text = "Select game!";

                    e.Handled = true;
                    Idle.IsChecked = false;
                }
                else if (selectedCount == 1)
                {
                    if(ContainCustom) 
                        await SteamRemoteClient.IdleGame(null, Custom_gameTitle.Text);
                    else
                        await SteamRemoteClient.IdleGame(((Games)games.SelectedItem).AppID, Custom_gameTitle.Text);
                }
                else
                {
                    await SteamRemoteClient.IdleGames(games.SelectedItems.Cast<Games>().Select(game => game.AppID).ToHashSet(),Custom_gameTitle.Text);
                }
            }
            else
            {
                await SteamRemoteClient.StopIdle();
            }

                
        }
    }
}
