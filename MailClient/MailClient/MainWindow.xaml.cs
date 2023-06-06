using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace MailClient
{
    public partial class MainWindow : Window
    {
        private ImapClient imapClient;
        private List<IMailFolder> folders; // Объявление списка папок
        private SmtpClient smtpClient;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Подключение к серверу IMAP
            imapClient = new ImapClient();
            imapClient.Connect("imap.example.com", 993, SecureSocketOptions.SslOnConnect);

            // Аутентификация
            imapClient.Authenticate("username", "password");

            // Получение списка папок
            folders = new List<IMailFolder>();
            folders.Add(imapClient.GetFolder("INBOX")); // Пример получения папки INBOX
            // Другие операции с получением папок...

            // Отображение списка папок в ListBox
            FoldersListBox.ItemsSource = folders;
        }



        private void FoldersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedFolder = (string)FoldersListBox.SelectedItem;

            // Загрузка писем из выбранной папки
            var folder = imapClient.GetFolder(selectedFolder);
            folder.Open(FolderAccess.ReadOnly);

            var messages = folder.Fetch(0, -1, MessageSummaryItems.Full);

            MessagesGrid.ItemsSource = messages.Select(m => new
            {
                Sender = m.Envelope.From.Mailboxes.FirstOrDefault()?.Address,
                Subject = m.Envelope.Subject
            });

            MessagesPanel.Visibility = Visibility.Visible;
        }

        private void MessagesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selectedMessage = (dynamic)MessagesGrid.SelectedItem;

            // Получение информации о выбранном сообщении
            var folder = imapClient.GetFolder(FoldersListBox.SelectedItem.ToString());
            var message = folder.GetMessage(selectedMessage.Index);

            // Отображение информации о сообщении
            SenderTextBlock.Text = message.From.ToString();
            RecipientTextBlock.Text = message.To.ToString();
            SubjectTextBlock.Text = message.Subject;
            ContentTextBlock.Text = message.TextBody;

            MessageViewPanel.Visibility = Visibility.Visible;
        }

        private void ReplyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedMessage = (dynamic)MessagesGrid.SelectedItem;
            string recipient = selectedMessage.Sender;

            // Открытие окна написания письма с автоматически заполненным получателем
            RecipientTextBox.Text = recipient;
            SubjectTextBox.Text = $"Re: {selectedMessage.Subject}";
            ComposePanel.Visibility = Visibility.Visible;
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string recipient = RecipientTextBox.Text;
            string subject = SubjectTextBox.Text;
            string content = ContentTextBox.Text;

            // Отправка письма
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Your Name", "your-email@example.com"));
            message.To.Add(new MailboxAddress("", recipient));
            message.Subject = subject;
            message.Body = new TextPart("plain")
            {
                Text = content
            };

            smtpClient = new SmtpClient();
            smtpClient.Connect("smtp.example.com", 587, SecureSocketOptions.StartTls);
            smtpClient.Authenticate("your-email@example.com", "your-password");
            smtpClient.Send(message);
            smtpClient.Disconnect(true);

            MessageBox.Show("Письмо отправлено!");
        }
    }
}
