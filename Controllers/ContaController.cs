using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProjetoISP.Baldan.Controllers
{
    /// <summary>Telas de entrada e saída. [AllowAnonymous] = acessível sem estar logado.</summary>
    [AllowAnonymous]
    public class ContaController : Controller
    {
        // GET: /Conta/Login?returnUrl=/SetorArea
        // Nossa tela de login (a do Figma). Quem não está logado é enviado para cá.
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            var destino = UrlSegura(returnUrl);

            if (User.Identity?.IsAuthenticated == true)
                return LocalRedirect(destino);

            ViewBag.ReturnUrl = destino;
            return View();
        }

        // GET: /Conta/Entrar?returnUrl=/SetorArea
        // Botão da tela de login: envia o usuário para a página de login da Microsoft.
        [HttpGet]
        public IActionResult Entrar(string? returnUrl = null)
        {
            var propriedades = new AuthenticationProperties { RedirectUri = UrlSegura(returnUrl) };
            return Challenge(propriedades, OpenIdConnectDefaults.AuthenticationScheme);
        }

        // POST: /Conta/Sair
        // Encerra a sessão aqui e na Microsoft, e volta para a tela de login.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Sair()
        {
            var propriedades = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(Login), "Conta")
            };

            return SignOut(
                propriedades,
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        // Só aceita endereços do próprio site (evita redirecionar o usuário para sites externos).
        private string UrlSegura(string? returnUrl) =>
            !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
    }
}
