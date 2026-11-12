using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

namespace TestAppMVC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Credencial do Firebase (service account).
            // Precisa ser definida ANTES de criar o FirestoreDb.
            Environment.SetEnvironmentVariable(
                "GOOGLE_APPLICATION_CREDENTIALS",
                Path.Combine(builder.Environment.ContentRootPath, "firebase-key.json"));

            // Autenticacao com Microsoft Identity Web
            // (exige os pacotes NuGet Microsoft.Identity.Web e Microsoft.Identity.Web.UI)
            builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

            // Controllers/Views e telas de UI do Microsoft Identity
            builder.Services.AddControllersWithViews()
                .AddMicrosoftIdentityUI();

            // Conexao com o Firestore (uma unica instancia para toda a aplicacao).
            // Os controllers recebem o FirestoreDb pelo construtor.
            var projectId = builder.Configuration["Firebase:ProjectId"]
                ?? throw new InvalidOperationException(
                    "Firebase:ProjectId nao configurado no appsettings.json.");
            builder.Services.AddSingleton(_ => FirestoreDb.Create(projectId));

            var app = builder.Build();

            // Pipeline de requisicoes HTTP
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}