using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace HttpListenerEfExample
{
    public class User
    {
        public int Id { get; set; }
        [JsonPropertyName("username")]
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? PasswordHash { get; set; }
        public string? FullName { get; set; }
        public DateTime BirthDate { get; set; }
        public string? Gender { get; set; }
        public string? Position { get; set; }
        public string? Department { get; set; }
        public string? PhoneNumber { get; set; }
        public string? PhotoPath { get; set; }
    }
    public class TaskUser
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsCompleted { get; set; }
        public int AssignedToUserId { get; set; }
        public User AssignedToUser { get; set; }
    }
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<TaskUser> Tasks { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseMySql(
                "server=192.168.0.78;database=project2;user=root2;password=000000;",
                new MySqlServerVersion(new Version(8, 0, 23))
            )
            .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("Users");
            // Настройка первичного ключа для TaskUser
            modelBuilder.Entity<TaskUser>()
                .HasKey(t => t.Id); // Указываем, что Id является первичным ключом

            modelBuilder.Entity<TaskUser>().ToTable("Tasks");
            // Настройка связи между TaskUser и User
            modelBuilder.Entity<TaskUser>()
                .HasOne(t => t.AssignedToUser) // Задача связана с одним пользователем
                .WithMany() // У пользователя может быть много задач
                .HasForeignKey(t => t.AssignedToUserId); // Внешний ключ
        }
    }

    public class UserServer
    {
        private readonly HttpListener _listener;
        private readonly string _url;
        private readonly AppDbContext _dbContext;

        public UserServer(string url)
        {
            _url = url;
            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
            _dbContext = new AppDbContext();
            _dbContext.Database.Migrate();
        }

        public async Task Start()
        {
            _listener.Start();
            Console.WriteLine($"Listening on {_url}");

            while (true)
            {
                var context = await _listener.GetContextAsync();
                _ = ProcessRequestAsync(context);
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            Console.WriteLine($"Received request: {request.HttpMethod} {request.Url.AbsolutePath}");

            try
            {
                var path = request.Url.AbsolutePath.TrimEnd('/');

                if (request.HttpMethod == "POST" && path == "/api/login")
                {
                    await HandleLogin(context);
                }
                else if (request.HttpMethod == "POST" && path == "/api/users")
                {
                    await HandleRegistration(context);
                }
                else if (request.HttpMethod == "GET" && path == "/api/users")
                {
                    await HandleGetUsers(context);
                }
                else if (request.HttpMethod == "GET" && path == "/api/login")
                {
                    await HandleGetLogin(context);
                }
                else if (request.HttpMethod == "GET" && path == "/api/tasks")
                {
                    await HandleGetTasks(context); 
                }
                else if (request.HttpMethod == "POST" && path == "/api/tasks")
                {
                    await HandleCreateTask(context); // Обработка создания задачи
                }
                else
                {
                    response.StatusCode = 404;
                    await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Not Found"));
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes($"Server error: {ex.Message}"));
            }
            finally
            {
                response.Close();
            }
        }

        private async Task HandleLogin(HttpListenerContext context)
        {
            using var reader = new StreamReader(context.Request.InputStream);
            var body = await reader.ReadToEndAsync();
            var loginRequest = System.Text.Json.JsonSerializer.Deserialize<LoginRequest>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (loginRequest == null || string.IsNullOrEmpty(loginRequest.Username) || string.IsNullOrEmpty(loginRequest.Password))
            {
                context.Response.StatusCode = 400;
                await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Invalid login data"));
                return;
            }

            var user = _dbContext.Users.FirstOrDefault(u => u.Username == loginRequest.Username);

            if (user == null || !VerifyPassword(loginRequest.Password, user.PasswordHash))
            {
                context.Response.StatusCode = 401;
                await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Invalid username or password"));
                return;
            }

            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            var responseJson = Newtonsoft.Json.JsonConvert.SerializeObject(user);
            await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(responseJson));
        }

        private async Task HandleRegistration(HttpListenerContext context)
        {
            using var reader = new StreamReader(context.Request.InputStream);
            var body = await reader.ReadToEndAsync();
            var registrationRequest = System.Text.Json.JsonSerializer.Deserialize<User>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (registrationRequest == null || string.IsNullOrEmpty(registrationRequest.Username) || string.IsNullOrEmpty(registrationRequest.PasswordHash))
            {
                context.Response.StatusCode = 400;
                await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Invalid registration data"));
                return;
            }

            registrationRequest.PasswordHash = ComputeSha256Hash(registrationRequest.PasswordHash);

            _dbContext.Users.Add(registrationRequest);
            await _dbContext.SaveChangesAsync();

            context.Response.StatusCode = 201;
            await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("User registered successfully"));
        }

        private async Task HandleCreateTask(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                using var reader = new StreamReader(request.InputStream);
                var body = await reader.ReadToEndAsync();
                
                Console.WriteLine($"Полный JSON: {body}");
                Console.WriteLine($"Длина JSON: {body.Length}");

                // Проверка на пустоту
                if (string.IsNullOrWhiteSpace(body))
                {
                    response.StatusCode = 400;
                    await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Пустое тело запроса"));
                    return;
                }

                TaskUser newTask;
                try 
                {
                    newTask = JsonConvert.DeserializeObject<TaskUser>(body);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка десериализации: {ex.Message}");
                    response.StatusCode = 400;
                    await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes($"Ошибка десериализации: {ex.Message}"));
                    return;
                }

                if (newTask == null)
                {
                    response.StatusCode = 400;
                    await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Не удалось создать объект задачи"));
                    return;
                }

                // Более подробная проверка полей
                var validationErrors = new List<string>();
                if (string.IsNullOrEmpty(newTask.Title)) validationErrors.Add("Отсутствует название задачи");
                if (newTask.AssignedToUserId == 0) validationErrors.Add("Некорректный ID пользователя");

                if (validationErrors.Any())
                {
                    response.StatusCode = 400;
                    await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(string.Join(", ", validationErrors)));
                    return;
                }

                using (var dbContext = new AppDbContext())
                {
                    dbContext.Tasks.Add(newTask);
                    await dbContext.SaveChangesAsync();

                    var createdTask = await dbContext.Tasks
                        .Include(t => t.AssignedToUser)
                        .FirstOrDefaultAsync(t => t.Id == newTask.Id);

                    response.StatusCode = 201;
                    response.ContentType = "application/json";
                    var responseJson = JsonConvert.SerializeObject(createdTask);
                    await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(responseJson));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка: {ex.Message}");
                response.StatusCode = 500;
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes($"Ошибка сервера: {ex.Message}"));
            }
        }
        private async Task HandleGetTasks(HttpListenerContext context)
        {
            var response = context.Response;

            try
            {
                using (var dbContext = new AppDbContext())
                {
                    var tasks = await dbContext.Tasks
                        .Include(t => t.AssignedToUser)
                        .ToListAsync();

                    var responseJson = JsonConvert.SerializeObject(tasks);

                    response.StatusCode = 200;
                    response.ContentType = "application/json";
                    await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(responseJson));
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes($"Ошибка сервера: {ex.Message}"));
            }
        }
        
        private string ComputeSha256Hash(string input)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
        
        private bool VerifyPassword(string enteredPassword, string storedHash)
        {
            return ComputeSha256Hash(enteredPassword) == storedHash;
        }
        private async Task HandleGetUsers(HttpListenerContext context)
        {
            var users = _dbContext.Users.ToList();
            var responseJson = Newtonsoft.Json.JsonConvert.SerializeObject(users);

            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(responseJson));
        }
        private async Task HandleGetLogin(HttpListenerContext context)
        {
            context.Response.StatusCode = 200;
            await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Login page"));
        }
    }
    
    

    class Program
    {
        static async Task Main()
        {
            var server = new UserServer("http://192.168.0.78:8080/");
            await server.Start();
        }
    }
}
