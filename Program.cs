using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
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

            // Login com a conta Microsoft da empresa (Microsoft Entra ID)
            builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

            // Quem não está logado vai para a NOSSA tela de login (/Conta/Login),
            // e não direto para a página da Microsoft.
            builder.Services.Configure<AuthenticationOptions>(options =>
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme);
            builder.Services.Configure<CookieAuthenticationOptions>(
                CookieAuthenticationDefaults.AuthenticationScheme,
                options => options.LoginPath = "/Conta/Login");

            // Todas as páginas exigem login, exceto as marcadas com [AllowAnonymous].
            builder.Services.AddAuthorization(options =>
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build());

            // Controllers/Views e telas de UI do Microsoft Identity
            builder.Services.AddControllersWithViews()
                .AddMicrosoftIdentityUI();

            // Conexao com o Firestore (uma unica instancia para toda a aplicacao).
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
