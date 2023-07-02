using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Formats.Asn1.AsnWriter;

namespace IKEDO
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {           
            InitializeComponent();
        }

        private void LoadDocumentWindow()
        {

            string token = Token.Text;

            Platform platform = ComboBox.Text == "Demo" ? Platform.Demo : Platform.Development;

            Setting setting = new(platform,token);

            DisableUI();

            ProgressBar.Value = 0;

            ProgressBar.Visibility = Visibility.Visible;

            IncreaseScaleProgress();

            Task.Run(async()=> {

                var personalInfo = await APIRequests.PersonalInfo(setting);

                Dispatcher.Invoke(() => IncreaseScaleProgress());

                var inboxDocuments = await APIRequests.GetDocuments(setting, DocumentType.Inbox);

                Dispatcher.Invoke(() => IncreaseScaleProgress());

                var sentDocuments = await APIRequests.GetDocuments(setting, DocumentType.Sent);

                Dispatcher.Invoke(() =>
                {
                    if(personalInfo == null || (inboxDocuments == null && sentDocuments == null))
                    {
                        EnableUI();

                        ProgressBar.Visibility = Visibility.Hidden;

                        return;
                    }

                    IncreaseScaleProgress();

                    var documentsWindow = new DocumentsWindow(setting, personalInfo, inboxDocuments, sentDocuments);

                    documentsWindow.Show();

                    Application.Current.MainWindow.Close();
                });
            });                     
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Token.Text == "")
            {
                return;
            }

            LoadDocumentWindow();
        }
        private void IncreaseScaleProgress()
        {
            const int PROGRESS_STEP = 25;

            ProgressBar.Value += PROGRESS_STEP;
        }

        private void DisableUI()
        {
            StartButton.IsEnabled = false;

            ComboBox.IsEnabled = false;       
            
            Token.IsEnabled = false;
        }

        private void EnableUI()
        {
            StartButton.IsEnabled = true;

            ComboBox.IsEnabled = true;

            Token.IsEnabled = true;
        }

    }
    
}
