using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Forms;
using System.IO;
using System.Windows.Threading;
using MimeDetective;

namespace IKEDO
{
    public partial class DocumentsWindow : Window
    {
        private string? _downloadPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";

        private readonly Setting? _setting;

        private readonly Person? _personalInfo;

        private readonly List<Document>? _inboxDocuments;

        private readonly List<Document>? _sentDocuments;
        public DocumentsWindow(Setting setting, Person? personalInfo, List<Document>? inboxDocuments, List<Document>? sentDocuments)
        {
            _setting = setting;
            _personalInfo = personalInfo;
            _inboxDocuments = inboxDocuments;
            _sentDocuments = sentDocuments;

            InitializeComponent();

            SetPersonalInfo(_personalInfo);

            SetDocumentList(DocumentType.Inbox);

            DeactivateDownloadButton();
        }
        private void Inbox_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SetColumnName("Отправитель", Person);

            SetDocumentList(DocumentType.Inbox);

            UnCheckedMainCheckBox();
        }
        private void Sent_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SetColumnName("Получатели", Person);

            SetDocumentList(DocumentType.Sent);

            UnCheckedMainCheckBox();
        }
        public void SetDocumentList(DocumentType type)
        {
            if (DocumentsList == null)
            {
                return;
            }

            List<object>? documents = new();

            var documentList = type switch
            {
                DocumentType.Sent => _sentDocuments,
                DocumentType.Inbox => _inboxDocuments,
                _ => null
            };

            if (documentList == null)
            {
                return;
            }

            foreach (Document document in documentList)
            {
                if (document == null)
                {
                    return;
                }

                List<PersonInfo>? personInfoList = new();

                if (type == DocumentType.Sent && document.Recipients != null)
                {
                    foreach (Recipients recipient in document.Recipients)
                    {
                        var personInfo = new PersonInfo(
                            GetFullName(recipient),
                            recipient.JobTitle ?? ""
                        );
                        personInfoList.Add(personInfo);
                    }
                }
                else if (type == DocumentType.Inbox && document.Sender != null)
                {
                    var personInfo = new PersonInfo(
                        GetFullName(document.Sender.Employee),
                        document?.Sender?.JobTitle?.Name ?? ""
                    );
                    personInfoList.Add(personInfo);
                }

                var Document = new TableDocument(
                    false,
                    document?.Name ?? "",
                    personInfoList,
                    Convert.ToDateTime(document?.DateSent).ToShortDateString()
                );

                documents.Add(Document);
            }

            DocumentsList.ItemsSource = documents;
        }

        private void SetPersonalInfo(Person? personalInfo)
        {
            if (personalInfo == null || personalInfo.LastName == null)
            {
                return;
            }

            FullName.Text = GetFullName(personalInfo);

            Email.Text = personalInfo.Email ?? "";
        }

        private static string GetFullName(Employee? employee)
        {
            if (employee == null || employee.LastName == null)
            {
                return "";
            }

            string fullName = $"{employee.LastName} {employee.FirstName} {employee.MiddleName}";

            return fullName;
        }

        private void DirectorySelectionButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new()
            {
                InitialDirectory = _downloadPath ?? Environment.CurrentDirectory
            };

            DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                _downloadPath = dialog.SelectedPath;
            }
        }

        private static void SetColumnName(string columnName, GridViewColumn? column)
        {
            if (column == null)
            {
                return;
            }

            column.Header = columnName;
        }

        private void MainCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (MainCheckBox == null || DocumentsList == null)
            {
                return;
            }

            List<TableDocument>? documents = DocumentsList.Items.Cast<TableDocument>().ToList();

            bool isChecked = MainCheckBox.IsChecked ?? false;

            int count = 0;

            foreach (TableDocument document in documents)
            {
                count++;
                document.CheckBox = isChecked;
            }

            DocumentsList.ItemsSource = documents;

            if (isChecked && count > 0)
            {
                ActivateDownloadButton();
            }
            else
            {
                DeactivateDownloadButton();
            }
        }

        private void UnCheckedMainCheckBox()
        {
            if (MainCheckBox == null)
            {
                return;
            }

            DeactivateDownloadButton();

            MainCheckBox.IsChecked = false;
        }

        private void ContentCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (MainCheckBox == null || DocumentsList == null)
            {
                return;
            }

            List<TableDocument>? documents = DocumentsList.Items.Cast<TableDocument>().ToList();

            int count = 0;

            foreach (TableDocument document in documents)
            {
                if (document.CheckBox == true)
                {
                    count++;
                }
            }

            if (count > 0)
            {
                ActivateDownloadButton();
            }
            if (documents.Count > 0 && count == documents.Count)
            {
                MainCheckBox.IsChecked = true;
            }
            else if (documents.Count > 0 && count == 0)
            {
                MainCheckBox.IsChecked = false;
            }
            else if (documents.Count > 0)
            {
                MainCheckBox.IsChecked = null;
            }
        }
        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DownloadDocuments(GetDocumentType());
            }
            catch (Exception exception)
            {
                DownloadProgressBar.Value = 0;

                SwitchUI(download: false);

                UnCheckedAllCheckBox();

                System.Windows.MessageBox.Show(exception.Message);

                return;
            }
        }

        private void ActivateDownloadButton()
        {
            DownloadButton.IsEnabled = true;

            DownloadButton.Foreground = Brushes.Black;
        }

        private void DeactivateDownloadButton()
        {
            DownloadButton.IsEnabled = false;

            DownloadButton.Foreground = Brushes.Gray;
        }

        private void DownloadDocuments(DocumentType type)
        {
            DeactivateDownloadButton();

            SwitchUI(download: true);

            if (DocumentsList == null || _setting == null || _downloadPath == null)
            {
                return;
            }

            List<Document>? documents = type switch
            {
                DocumentType.Sent => _sentDocuments,
                DocumentType.Inbox => _inboxDocuments,
                _ => null
            };

            if (documents == null || documents.Count == 0)
            {
                return;
            }

            List<DownloadInfo> downloadInfo = new();

            List<TableDocument>? tableDocuments = DocumentsList.Items.Cast<TableDocument>().ToList();

            for (int i = 0; i < tableDocuments.Count; i++)
            {
                if (tableDocuments[i].CheckBox == true && documents[i].Name != null && documents[i].Id != null)
                {
                    downloadInfo.Add(new DownloadInfo(documents[i].Name ?? "", documents[i].Id ?? ""));             
                }
            }

            const int STOCK_FOR_THE_PROGRESS_BAR = 1;

            const int PROGRESS_MAX = 100;

            int progressStep = PROGRESS_MAX / (downloadInfo.Count + STOCK_FOR_THE_PROGRESS_BAR);

            DownloadProgressBar.Value += progressStep;

            Task.Run(async () =>
            {
                foreach (DownloadInfo info in downloadInfo)
                {
                    Dispatcher.Invoke(() =>
                    {
                        DownloadProgressBar.Value += progressStep;
                    });

                    if (info.FileName == null || info.DocumentID == null)
                    {
                        continue;
                    }

                    string? rawData = null;

                    rawData = await APIRequests.DownloadRawDocument(_setting,id: info.DocumentID);

                    if (rawData == null)
                    {
                        continue;
                    }

                    byte[] data = Convert.FromBase64String(rawData);

                    Dispatcher.Invoke(() =>
                    {
                        string? extension = GetExtensionFromFileData(data);

                        if (extension == null)
                        {
                            return;
                        }

                        string relativePath = $"{info.FileName}.{extension}";

                        string filePath = Path.Combine(_downloadPath, relativePath);

                        int additiveIndex = 1;

                        while (File.Exists(filePath))
                        {
                            relativePath = $"{info.FileName} ({additiveIndex}).{extension}";
                            filePath = Path.Combine(_downloadPath, relativePath);
                            additiveIndex++;
                        }

                        using FileStream fileStream = new(filePath, FileMode.Create);

                        fileStream.Write(data, 0, data.Length);

                        fileStream.Close();
                    });                
                }
                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;

                    SwitchUI(download: false);

                    UnCheckedAllCheckBox();
                });
            });
        }
        public static string? GetExtensionFromFileData(byte[] data)
        {
            var Inspector = new ContentInspectorBuilder()
            {
                Definitions = MimeDetective.Definitions.Default.All()
            }.Build();

            var Results = Inspector.Inspect(data);

            var ResultsByFileExtension = Results.ByFileExtension();

            if (ResultsByFileExtension == null || ResultsByFileExtension.Length <= 0)
            {
                return null;
            }

            const int PROBABLE_EXTANSION_INDEX = 0;

            return ResultsByFileExtension[PROBABLE_EXTANSION_INDEX].Extension;
        }
        private void SwitchUI(bool download)
        {
            DocumentsList.IsEnabled =
            InboxRadioButton.IsEnabled =
            SentRadioButton.IsEnabled =
            DirectorySelectionButton.IsEnabled = !download;

            DocumentsList.Foreground =
            InboxRadioButton.Foreground =
            SentRadioButton.Foreground =
            DirectorySelectionButton.Foreground = download ? Brushes.Gray : Brushes.Black;

            ProgressBarText.Visibility = download ? Visibility.Visible : Visibility.Hidden;
        }

        private void UnCheckedAllCheckBox()
        {
            if(DocumentsList == null || MainCheckBox == null)
            {
                return;
            }

            List<TableDocument>? documents = DocumentsList.Items.Cast<TableDocument>().ToList();

            foreach (TableDocument document in documents)
            {
                document.CheckBox = false;
            }

            DocumentsList.ItemsSource = documents;

            MainCheckBox.IsChecked = false;
        }

        private DocumentType GetDocumentType()
        {
            if(InboxRadioButton == null || SentRadioButton == null)
            {
                return DocumentType.NoType;
            }

            if (InboxRadioButton.IsChecked ?? false)
            {
                return DocumentType.Inbox;
            }
            else if (SentRadioButton.IsChecked ?? false)
            {
                return DocumentType.Sent;
            }

            return DocumentType.NoType;
        }       
    }
}
