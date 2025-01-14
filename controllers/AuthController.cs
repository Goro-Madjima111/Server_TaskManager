using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using MyAppDB.Models;
using MyAppDB.Data;
using Microsoft.EntityFrameworkCore;

public class AuthController
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

public async Task Register(HttpListenerRequest request, HttpListenerResponse response)
    {
        using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
        {
            string requestBody = await reader.ReadToEndAsync();
            Console.WriteLine("Received request body: " + requestBody);

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                response.StatusCode = 400;
                var errorResponse = JsonSerializer.Serialize(new { message = "Request body cannot be empty." });
                byte[] errorBuffer = Encoding.UTF8.GetBytes(errorResponse);
                response.ContentType = "application/json";
                response.ContentLength64 = errorBuffer.Length;
                response.OutputStream.Write(errorBuffer, 0, errorBuffer.Length);
                response.Close();
                return;
            }

            var registrationRequest = JsonSerializer.Deserialize<UserRegistrationRequest>(requestBody);

            if (registrationRequest == null || string.IsNullOrWhiteSpace(registrationRequest.Username) ||
                string.IsNullOrWhiteSpace(registrationRequest.Email) || string.IsNullOrWhiteSpace(registrationRequest.Password) ||
                registrationRequest.Password != registrationRequest.ConfirmPassword)
            {
                response.StatusCode = 400;
                var errorResponse = JsonSerializer.Serialize(new { message = "Invalid registration data." });
                byte[] errorBuffer = Encoding.UTF8.GetBytes(errorResponse);
                response.ContentType = "application/json";
                response.ContentLength64 = errorBuffer.Length;
                response.OutputStream.Write(errorBuffer, 0, errorBuffer.Length);
                response.Close();
                return;
            }

            var user = new Users
            {
                Username = registrationRequest.Username,
                Email = registrationRequest.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registrationRequest.Password),
                CreatedAt = DateTime.UtcNow,
                Fullname = null, 
                Birthdate = null, 
                Gender = null, 
                Position = null, 
                Department = null, 
                Phonenumber = null, 
                Photopath = null 
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Возвращаем JSON с userId и сообщением об успехе
            var successResponse = JsonSerializer.Serialize(new 
            { 
                userId = user.Id,
                message = "Registration successful"
            });

            response.StatusCode = 200;
            response.ContentType = "application/json";
            byte[] successBuffer = Encoding.UTF8.GetBytes(successResponse);
            response.ContentLength64 = successBuffer.Length;
            response.OutputStream.Write(successBuffer, 0, successBuffer.Length);
            response.Close();
        }
    }
    public async Task Login(HttpListenerRequest request, HttpListenerResponse response)
    {
        using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
        {
            string requestBody = await reader.ReadToEndAsync();

            // Логируем тело запроса для отладки
            Console.WriteLine("Received login request body: " + requestBody);

            // Проверяем, пустое ли тело запроса
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                response.StatusCode = 400; // Bad Request
                byte[] errorBuffer = Encoding.UTF8.GetBytes("Request body cannot be empty.");
                response.ContentLength64 = errorBuffer.Length;
                response.OutputStream.Write(errorBuffer, 0, errorBuffer.Length);
                response.Close();
                return;
            }

            // Десериализуем запрос
            var loginRequest = JsonSerializer.Deserialize<UserLoginRequest>(requestBody);

            // Проверяем валидность данных
            if (loginRequest == null || string.IsNullOrWhiteSpace(loginRequest.Username) ||
                string.IsNullOrWhiteSpace(loginRequest.Password))
            {
                response.StatusCode = 400; // Bad Request
                byte[] errorBuffer = Encoding.UTF8.GetBytes("Invalid login data.");
                response.ContentLength64 = errorBuffer.Length;
                response.OutputStream.Write(errorBuffer, 0, errorBuffer.Length);
                response.Close();
                return;
            }

            // Ищем пользователя в базе данных
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == loginRequest.Username);

            // Проверяем, существует ли пользователь
            if (user == null)
            {
                response.StatusCode = 401; // Unauthorized
                byte[] errorBuffer = Encoding.UTF8.GetBytes("Invalid username or password.");
                response.ContentLength64 = errorBuffer.Length;
                response.OutputStream.Write(errorBuffer, 0, errorBuffer.Length);
                response.Close();
                return;
            }

            // Проверяем пароль
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                response.StatusCode = 401; // Unauthorized
                byte[] errorBuffer = Encoding.UTF8.GetBytes("Invalid username or password.");
                response.ContentLength64 = errorBuffer.Length;
                response.OutputStream.Write(errorBuffer, 0, errorBuffer.Length);
                response.Close();
                return;
            }

            // Если всё успешно, возвращаем ответ 200 OK
            response.StatusCode = 200; // OK
            byte[] successBuffer = Encoding.UTF8.GetBytes("Login successful");
            response.ContentLength64 = successBuffer.Length;
            response.OutputStream.Write(successBuffer, 0, successBuffer.Length);
            response.Close();
        }
    }
}
