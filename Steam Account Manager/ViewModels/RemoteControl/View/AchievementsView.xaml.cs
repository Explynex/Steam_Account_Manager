using Steam_Account_Manager.Infrastructure.Models;
using System.Windows;
using System.Windows.Controls;

namespace Steam_Account_Manager.ViewModels.RemoteControl.View
{
    public partial class AchievementsView : Window
    {
        ulong appId;
        public AchievementsView(ulong appID)
        {
            InitializeComponent();
            this.appId = appID;
            this.Header.Text = $"Achievements AppID: {appID}";
            this.DataContext = new AchievementsViewModel(appID);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (this.DataContext != null && ((AchievementsViewModel)this.DataContext).Achievements != null)
            {
                switch (filter.SelectedIndex)
                {
                    case 0:
                        ((AchievementsViewModel)this.DataContext).AchievementShowFilter.Filter = o => ((StatData)o) != null;
                        break;
                    case 1:
                        ((AchievementsViewModel)this.DataContext).AchievementShowFilter.Filter = o => !((StatData)o).IsSet;
                        break;
                    case 2:
                        ((AchievementsViewModel)this.DataContext).AchievementShowFilter.Filter = o => ((StatData)o).IsSet;
                        break;
                }

            }
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            achievements.SelectAll();
        }

        private void UnselectAll_Click(object sender, RoutedEventArgs e)
        {
            achievements.UnselectAll();
        }

        private void SelectLocked_Click(object sender, RoutedEventArgs e)
        {
            if (achievements.SelectedItems.Count != 0)
                achievements.UnselectAll();

            for (int i = 0; i < ((AchievementsViewModel)this.DataContext).Achievements.Count; i++)
            {
                if (!((AchievementsViewModel)this.DataContext).Achievements[i].IsSet)
                {
                    ((ListBoxItem)achievements.ItemContainerGenerator.ContainerFromIndex(i)).IsSelected = true;
                }
            }

        }

        private void SelectUnlocked_Click(object sender, RoutedEventArgs e)
        {
            if (achievements.SelectedItems.Count != 0)
                achievements.UnselectAll();


            for (int i = 0; i < ((AchievementsViewModel)this.DataContext).Achievements.Count; i++)
            {
                if (((AchievementsViewModel)this.DataContext).Achievements[i].IsSet)
                {
                    ((ListBoxItem)achievements.ItemContainerGenerator.ContainerFromIndex(i)).IsSelected = true;
                }
            }
        }

    }
}
