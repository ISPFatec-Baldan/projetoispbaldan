using Google.Cloud.Firestore;
using ProjetoISP.Baldan.Data;

var builder = WebApplication.CreateBuilder(args);

// Configuração do Firebase (Firestore)
var projectId = builder.Configuration["Firebase:ProjectId"]
    ?? throw new InvalidOperationException("Firebase:ProjectId não configurado no appsettings.json");

var credentialPath = builder.Configuration["Firebase:CredentialPath"]
    ?? throw new InvalidOperationException("Firebase:CredentialPath não configurado no appsettings.json");

var credentialFullPath = Path.Combine(builder.Environment.ContentRootPath, credentialPath);

Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialFullPath);

builder.Services.AddSingleton(_ => FirestoreDb.Create(projectId));
builder.Services.AddSingleton<FirestoreContext>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();