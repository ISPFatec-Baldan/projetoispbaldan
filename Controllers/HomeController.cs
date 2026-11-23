using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProjetoISP.Baldan.Models;
using ProjetoISP.Baldan.Services;

namespace ProjetoISP.Baldan.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly PainelService _painel;
        private readonly RecorrenciaService _recorrencia;

        public HomeController(
            ILogger<HomeController> logger, PainelService painel, RecorrenciaService recorrencia)
        {
            _logger = logger;
            _painel = painel;
            _recorrencia = recorrencia;
        }

        // Tela inicial (painel)
        public async Task<IActionResult> Index()
        {
            var vm = DashboardViewModel.Exemplo();   // base: só "planos pendentes" e gráfico ainda de exemplo
            await _painel.PreencherAsync(vm);        // o restante vem do Firestore
            return View(vm);
        }

        // GET: /Home/Recorrencias?de=2026-11-01&ate=2026-11-30&idSetor=abc123
        // Alimenta o quadro "Recorrências registradas" (filtros por período e por setor).
        [HttpGet]
        public async Task<IActionResult> Recorrencias(DateTime? de, DateTime? ate, string? idSetor)
        {
            // As datas chegam como dia inteiro (hora local); "ate" vale até o fim do dia.
            DateTime? deUtc = de.HasValue
                ? DateTime.SpecifyKind(de.Value.Date, DateTimeKind.Local).ToUniversalTime()
                : null;
            DateTime? ateUtc = ate.HasValue
                ? DateTime.SpecifyKind(ate.Value.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime()
                : null;

            if (deUtc.HasValue && ateUtc.HasValue && deUtc >= ateUtc)
                return BadRequest("A data inicial não pode ser maior que a data final.");

            var lista = await _recorrencia.ListarReincidenciasAsync(deUtc, ateUtc, idSetor);

            return Json(new
            {
                total = lista.Count,
                itens = lista.Select(r => new
                {
                    setor = r.NomeSetor,
                    pergunta = r.Pergunta,
                    seguidas = r.OcorrenciasSeguidas,
                    ultima = r.UltimaOcorrencia.ToLocalTime().ToString("dd/MM/yyyy")
                })
            });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
