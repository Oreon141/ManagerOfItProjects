# Инструкция по запуску ManagerOfItProjects

## 1) Требования
- Windows 10/11
- Visual Studio 2019/2022 с workload **.NET desktop development**
- .NET Framework 4.8 Targeting Pack
- SQL Server (например, SQL Server Express) + SSMS

## 2) Настройка БД
1. Создайте БД `ITProjectsManager` на сервере `Desktop-141\\SQLEXPRESS` (или измените строку подключения в `App.config`).
2. Примените ваш SQL-скрипт с таблицами, включая:
   - `Notifications`
   - `Chats`
   - `ChatParticipants`
   - `Messages`
3. Обновите `DataBase.edmx` (Database First):
   - **Update Model from Database**
   - добавьте новые таблицы и связи
   - сохраните edmx и дождитесь генерации `DataBase.tt`/`DataBase.Context.tt`

## 3) Проверка строки подключения
В `App.config` должна быть корректная строка подключения:
- `name="ITProjectsManagerEntities"`
- `data source=Desktop-141\\SQLEXPRESS`
- `initial catalog=ITProjectsManager`

Если сервер/БД другие — замените значения под вашу среду.

## 4) Сборка и запуск
1. Откройте `ManagerOfItProjects.sln` в Visual Studio.
2. Выберите конфигурацию `Debug | Any CPU`.
3. Выполните **Build Solution**.
4. Запустите приложение (**F5**).

## 5) Что проверить после запуска
- В левом меню есть пункты: **Уведомления** и **Чат**.
- В меню у уведомлений отображается счётчик непрочитанных.
- На странице уведомлений работают фильтры и "Отметить всё прочитанным".
- В чате можно:
  - создать новый чат,
  - отправить сообщение кнопкой и Enter,
  - видеть обновление сообщений (таймер 3 сек).

## 6) Если не работает
- Ошибки по таблицам `Notifications/Chats/...` означают, что таблицы не созданы или `DataBase.edmx` не обновлён.
- Проверьте, что в `App.config` строка подключения указывает на доступный SQL Server.
