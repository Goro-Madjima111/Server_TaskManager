using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using MyAppDB.Data;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        
        IConfiguration configuration = builder.Build();
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseMySql(
            configuration.GetConnectionString("DefaultConnection"), 
            ServerVersion.AutoDetect(configuration.GetConnectionString("DefaultConnection"))
        );
        
        var dbContext = new AppDbContext(optionsBuilder.Options);
        var authController = new AuthController(dbContext);
        
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://192.168.0.5:8080/");
        listener.Start();
        Console.WriteLine("Listening...");
        
        while (true)
        {
            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            
            if (request.Url != null)
            {
                if (request.Url.AbsolutePath == "/register")
                {
                    await authController.Register(request, response);
                }
                else if (request.Url.AbsolutePath == "/login")
                {
                    await authController.Login(request, response);
                }
                else
                {
                    response.StatusCode = 404;
                    response.Close();
                }
            }
            else
            {
                response.StatusCode = 400;
                response.Close();
            }
        }
    }
}