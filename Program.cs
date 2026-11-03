using Google.Cloud.Firestore;

var builder = WebApplication.CreateBuilder(args);

// Adicionar os serviços do MVC ao contentor de dependências
builder.Services.AddControllersWithViews();

// Configurar o caminho para o ficheiro JSON de credenciais do Firebase
string pathToCred = Path.Combine(builder.Environment.ContentRootPath, "firebase-auth.json");
Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", pathToCred);

// Registar o FirestoreDb como Singleton para ser injetado nos Controllers
builder.Services.AddSingleton<FirestoreDb>(provider => {
    string projectId = "projetoispbaldan";
    return FirestoreDb.Create(projectId);
});

var app = builder.Build();

// Configuração do pipeline de requisições HTTP
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