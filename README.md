# HTTP Listener с использованием Entity Framework

Этот проект представляет собой простой HTTP-сервер, написанный на C# с использованием `HttpListener` и Entity Framework для работы с базой данных. Сервер поддерживает регистрацию пользователей, аутентификацию и управление задачами.

## Возможности

- **Регистрация пользователей**: Регистрация новых пользователей с указанием таких данных, как имя пользователя, email, пароль, полное имя, дата рождения, пол, должность, отдел, номер телефона и путь к фотографии.
- **Аутентификация пользователей**: Аутентификация пользователей по имени пользователя и паролю.
- **Управление задачами**: Создание и получение задач, назначенных пользователям.

## Требования

- .NET SDK (версия 5.0 или выше)
- MySQL Server (версия 8.0.23 или выше)
- Entity Framework Core

## Настройка

1. **Клонирование репозитория**:
   ```bash
   git clone https://github.com/your-repo/HttpListenerEfExample.git
   cd HttpListenerEfExample
   ```

2. **Настройка базы данных**:
   - Обновите строку подключения в классе `AppDbContext`, указав данные вашего MySQL-сервера:
     ```csharp
     protected override void OnConfiguring(DbContextOptionsBuilder options)
         => options.UseMySql(
             "server=ваш_сервер;database=ваша_база_данных;user=ваш_пользователь;password=ваш_пароль;",
             new MySqlServerVersion(new Version(8, 0, 23))
         .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
     ```

3. **Применение миграций базы данных**:
   - Убедитесь, что база данных создана, и примените миграции:
     ```bash
     dotnet ef database update
     ```

4. **Запуск сервера**:
   - Запустите HTTP-сервер:
     ```bash
     dotnet run
     ```

## API Endpoints

- **POST /api/login**: Аутентификация пользователя.
  - Тело запроса:
    ```json
    {
      "username": "ваше_имя_пользователя",
      "password": "ваш_пароль"
    }
    ```
  - Ответ:
    ```json
    {
      "id": 1,
      "username": "ваше_имя_пользователя",
      "email": "ваш_email@example.com",
      "fullName": "Ваше Полное Имя",
      "birthDate": "1990-01-01T00:00:00",
      "gender": "Мужской",
      "position": "Разработчик",
      "department": "IT",
      "phoneNumber": "1234567890",
      "photoPath": "путь/к/фотографии.jpg"
    }
    ```

- **POST /api/users**: Регистрация нового пользователя.
  - Тело запроса:
    ```json
    {
      "username": "новый_пользователь",
      "email": "новый_пользователь@example.com",
      "passwordHash": "хэшированный_пароль",
      "fullName": "Новый Пользователь",
      "birthDate": "1990-01-01T00:00:00",
      "gender": "Женский",
      "position": "Дизайнер",
      "department": "Креатив",
      "phoneNumber": "0987654321",
      "photoPath": "путь/к/новой_фотографии.jpg"
    }
    ```
  - Ответ:
    ```
    Пользователь успешно зарегистрирован
    ```

- **GET /api/users**: Получение списка всех пользователей.
  - Ответ:
    ```json
    [
      {
        "id": 1,
        "username": "пользователь1",
        "email": "пользователь1@example.com",
        "fullName": "Пользователь Один",
        "birthDate": "1990-01-01T00:00:00",
        "gender": "Мужской",
        "position": "Разработчик",
        "department": "IT",
        "phoneNumber": "1234567890",
        "photoPath": "путь/к/фотографии1.jpg"
      },
      {
        "id": 2,
        "username": "пользователь2",
        "email": "пользователь2@example.com",
        "fullName": "Пользователь Два",
        "birthDate": "1991-02-02T00:00:00",
        "gender": "Женский",
        "position": "Дизайнер",
        "department": "Креатив",
        "phoneNumber": "0987654321",
        "photoPath": "путь/к/фотографии2.jpg"
      }
    ]
    ```

- **POST /api/tasks**: Создание новой задачи.
  - Тело запроса:
    ```json
    {
      "title": "Новая задача",
      "description": "Описание задачи",
      "dueDate": "2023-12-31T00:00:00",
      "isCompleted": false,
      "assignedToUserId": 1
    }
    ```
  - Ответ:
    ```json
    {
      "id": 1,
      "title": "Новая задача",
      "description": "Описание задачи",
      "dueDate": "2023-12-31T00:00:00",
      "isCompleted": false,
      "assignedToUserId": 1,
      "assignedToUser": {
        "id": 1,
        "username": "пользователь1",
        "email": "пользователь1@example.com",
        "fullName": "Пользователь Один",
        "birthDate": "1990-01-01T00:00:00",
        "gender": "Мужской",
        "position": "Разработчик",
        "department": "IT",
        "phoneNumber": "1234567890",
        "photoPath": "путь/к/фотографии1.jpg"
      }
    }
    ```

- **GET /api/tasks**: Получение списка всех задач.
  - Ответ:
    ```json
    [
      {
        "id": 1,
        "title": "Новая задача",
        "description": "Описание задачи",
        "dueDate": "2023-12-31T00:00:00",
        "isCompleted": false,
        "assignedToUserId": 1,
        "assignedToUser": {
          "id": 1,
          "username": "пользователь1",
          "email": "пользователь1@example.com",
          "fullName": "Пользователь Один",
          "birthDate": "1990-01-01T00:00:00",
          "gender": "Мужской",
          "position": "Разработчик",
          "department": "IT",
          "phoneNumber": "1234567890",
          "photoPath": "путь/к/фотографии1.jpg"
        }
      }
    ]
    ```
