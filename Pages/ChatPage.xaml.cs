using ManagerOfItProjects.DataBase;
using ManagerOfItProjects.Models;
using ManagerOfItProjects.Services;
using System;
using System.Collections.Generic;
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
        private ChatItem currentChat;

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
            string sql = @"SELECT c.ChatID, c.Name, c.IsGroup, c.CreatedAt,
                           (SELECT TOP 1 m.MessageText FROM Messages m WHERE m.ChatID = c.ChatID ORDER BY m.SentAt DESC) as LastMessage,
                           (SELECT TOP 1 m.SentAt FROM Messages m WHERE m.ChatID = c.ChatID ORDER BY m.SentAt DESC) as LastMessageAt,
                           (SELECT COUNT(1) FROM Messages m WHERE m.ChatID = c.ChatID AND m.SenderID <> @p0 AND m.IsRead = 0) as UnreadCount
                           FROM Chats c
                           INNER JOIN ChatParticipants cp ON cp.ChatID = c.ChatID
                           WHERE cp.UserID = @p0
                           ORDER BY LastMessageAt DESC";
            var chats = db.Database.SqlQuery<ChatItem>(sql, CurrentUser.UserID).ToList();
            ChatsList.ItemsSource = chats.Select(c => new { Entity = c, DisplayName = $"{c.Name}  {(c.UnreadCount > 0 ? "(" + c.UnreadCount + ")" : "")}" }).ToList();
        }

        private void ChatsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dynamic selected = ChatsList.SelectedItem;
            if (selected == null) return;
            currentChat = selected.Entity as ChatItem;
            if (currentChat == null) return;
            LoadMessages(currentChat.ChatID);
            LoadParticipants(currentChat.ChatID);
            db.Database.ExecuteSqlCommand("UPDATE Messages SET IsRead = 1 WHERE ChatID = @p0 AND SenderID <> @p1", currentChat.ChatID, CurrentUser.UserID);
        }

        private void LoadMessages(int chatId)
        {
            string sql = @"SELECT m.MessageID,m.ChatID,m.SenderID,u.Login as SenderLogin,m.MessageText,m.SentAt,m.IsRead
                           FROM Messages m INNER JOIN Users u ON u.UserID = m.SenderID
                           WHERE m.ChatID = @p0 ORDER BY m.SentAt";
            var messages = db.Database.SqlQuery<ChatMessageItem>(sql, chatId).ToList();
            MessagesList.ItemsSource = messages.Select(m => new { DisplayText = $"{m.SenderLogin}: {m.MessageText} ({m.SentAt:HH:mm})" }).ToList();
        }

        private void LoadParticipants(int chatId)
        {
            string sql = @"SELECT u.UserID,u.Login,ur.RoleName
                           FROM ChatParticipants cp
                           INNER JOIN Users u ON u.UserID = cp.UserID
                           INNER JOIN UserRoles ur ON ur.RoleID = u.RoleID
                           WHERE cp.ChatID = @p0";
            var participants = db.Database.SqlQuery<ChatParticipantItem>(sql, chatId).ToList();
            ParticipantsList.ItemsSource = participants.Select(p => new { DisplayText = $"{p.Login} ({p.RoleName})" }).ToList();
        }

        private void Send_Click(object sender, RoutedEventArgs e) => SendMessage();

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

            db.Database.ExecuteSqlCommand(
                "INSERT INTO Messages (ChatID, SenderID, MessageText, SentAt, IsRead) VALUES (@p0,@p1,@p2,@p3,@p4)",
                currentChat.ChatID, CurrentUser.UserID, MessageBox.Text.Trim(), DateTime.Now, false);

            var participantIds = db.Database.SqlQuery<int>("SELECT UserID FROM ChatParticipants WHERE ChatID = @p0 AND UserID <> @p1", currentChat.ChatID, CurrentUser.UserID).ToList();
            foreach (int userId in participantIds)
                NotificationService.CreateNotification(userId, "Новое сообщение", $"Новое сообщение в чате {currentChat.Name}", "NewMessage", currentChat.ChatID);

            MessageBox.Clear();
            LoadMessages(currentChat.ChatID);
        }

        private void NewChat_Click(object sender, RoutedEventArgs e)
        {
            string chatName = $"Новый чат {DateTime.Now:ddMMyyHHmm}";
            db.Database.ExecuteSqlCommand("INSERT INTO Chats (Name, IsGroup, CreatedAt) VALUES (@p0,@p1,@p2)", chatName, true, DateTime.Now);
            int chatId = db.Database.SqlQuery<int>("SELECT TOP 1 ChatID FROM Chats WHERE Name = @p0 ORDER BY ChatID DESC", chatName).FirstOrDefault();
            if (chatId > 0)
                db.Database.ExecuteSqlCommand("INSERT INTO ChatParticipants (ChatID, UserID) VALUES (@p0,@p1)", chatId, CurrentUser.UserID);
            LoadChats();
        }
    }
}
