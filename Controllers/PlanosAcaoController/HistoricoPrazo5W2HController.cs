using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using ProjetoISP.Baldan.Models.PlanosAcao;

namespace ProjetoISP.Baldan.Controllers
{
    /// <summary>
    /// Somente leitura. Os registros de histórico são criados automaticamente pelo
    /// PlanoAcao5W2HController quando o prazo de um plano é alterado.
    /// </summary>
    public class HistoricoPrazo5W2HController : Controller
    {
        private readonly FirestoreDb _db;

        public HistoricoPrazo5W2HController(FirestoreDb db)
        {
            _db = db;
        }

        private CollectionReference Colecao => _db.Collection(Colecoes.HistoricosPrazo);

        // GET: HistoricoPrazo5W2H  (opcional: ?idPlanoAcao=abc123 para ver o histórico de um plano)
        public async Task<IActionResult> Index(string? idPlanoAcao)
        {
            Query consulta = Colecao;
            if (!string.IsNullOrEmpty(idPlanoAcao))
                consulta = consulta.WhereEqualTo(nameof(HistoricoPrazo5W2H.IdPlanoAcao), idPlanoAcao);

            var snapshot = await consulta.GetSnapshotAsync();
            var historicos = snapshot.Documents
                .Select(d => d.ConvertTo<HistoricoPrazo5W2H>())
                .OrderByDescending(h => h.DataAlteracao.ToDateTime())
                .ToList();

            ViewBag.IdPlanoAcao = idPlanoAcao;
            ViewBag.DescricoesPlanos = await DescricoesPlanosAsync();
            return View(historicos);
        }

        // GET: HistoricoPrazo5W2H/Details/abc123
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var snapshot = await Colecao.Document(id).GetSnapshotAsync();
            if (!snapshot.Exists) return NotFound();

            var historico = snapshot.ConvertTo<HistoricoPrazo5W2H>();
            ViewBag.DescricoesPlanos = await DescricoesPlanosAsync();
            return View(historico);
        }

        // ---------- Auxiliares ----------

        private async Task<Dictionary<string, string>> DescricoesPlanosAsync()
        {
            var snapshot = await _db.Collection(Colecoes.PlanosAcao).GetSnapshotAsync();
            return snapshot.Documents.ToDictionary(d => d.Id, d => d.ConvertTo<PlanoAcao5W2H>().DescricaoDesvio);
        }
    }
}