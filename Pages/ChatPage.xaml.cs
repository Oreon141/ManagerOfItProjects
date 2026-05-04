using ManagerOfItProjects.DataBase;
using ManagerOfItProjects.Models;
using ManagerOfItProjects.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace ManagerOfItProjects.Pages
{
    public partial class ChatPage : Page
    {
        private readonly ITProjectsManagerEntities db = ITProjectsManagerEntities.GetContext();
        private readonly DispatcherTimer timer = new DispatcherTimer();
        private Chats currentChat;

        public ChatPage()
        {
            InitializeComponent();
            LoadChats();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            LoadChats();
            if (currentChat != null)
            {
                LoadMessages(currentChat.ChatID);
                LoadParticipants(currentChat.ChatID);
            }
        }

        private void LoadChats()
        {
            ChatsList.ItemsSource = db.ChatParticipants.Where(cp => cp.UserID == CurrentUser.UserID).Select(cp => cp.Chats).ToList().Select(c => new
            {
                Entity = c,
                DisplayName = $"{c.Name}"
            }).ToList();
        }

        private void ChatsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dynamic selected = ChatsList.SelectedItem;
            if (selected == null) return;
            currentChat = selected.Entity as Chats;
            if (currentChat == null) return;
            LoadMessages(currentChat.ChatID);
            LoadParticipants(currentChat.ChatID);
        }

        private void LoadMessages(int chatId)
        {
            MessagesList.ItemsSource = db.Messages.Where(m => m.ChatID == chatId).OrderBy(m => m.SentAt).ToList().Select(m => new
            {
                DisplayText = $"{m.Users?.Login}: {m.MessageText} ({m.SentAt:HH:mm})"
            }).ToList();
        }

        private void LoadParticipants(int chatId)
        {
            ParticipantsList.ItemsSource = db.ChatParticipants.Where(cp => cp.ChatID == chatId).ToList().Select(cp => new
            {
                DisplayText = $"{cp.Users?.Login} ({cp.Users?.UserRoles?.RoleName})"
            }).ToList();
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void MessageBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage();
                e.Handled = true;
            }
        }

        private void SendMessage()
        {
            if (currentChat == null || string.IsNullOrWhiteSpace(MessageBox.Text)) return;

            var message = new Messages { ChatID = currentChat.ChatID, SenderID = CurrentUser.UserID, MessageText = MessageBox.Text.Trim(), SentAt = DateTime.Now, IsRead = false };
            db.Messages.Add(message);
            db.SaveChanges();

            var participantIds = db.ChatParticipants.Where(x => x.ChatID == currentChat.ChatID && x.UserID != CurrentUser.UserID).Select(x => x.UserID).ToList();
            foreach (var userId in participantIds)
            {
                NotificationService.CreateNotification(userId, "Новое сообщение", $"Новое сообщение в чате {currentChat.Name}", "NewMessage", currentChat.ChatID);
            }

            MessageBox.Clear();
            LoadMessages(currentChat.ChatID);
        }

        private void NewChat_Click(object sender, RoutedEventArgs e)
        {
            var chat = new Chats { Name = $"Новый чат {DateTime.Now:ddMMyyHHmm}", IsGroup = true, CreatedAt = DateTime.Now };
            db.Chats.Add(chat);
            db.SaveChanges();
            db.ChatParticipants.Add(new ChatParticipants { ChatID = chat.ChatID, UserID = CurrentUser.UserID });
            db.SaveChanges();
            LoadChats();
        }
    }
}
