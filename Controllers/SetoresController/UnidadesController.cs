using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using ProjetoISP.Baldan.Models.Setores;

namespace ProjetoISP.Baldan.Controllers
{
    public class UnidadesController : Controller
    {
        private readonly FirestoreDb _db;

        public UnidadesController(FirestoreDb db)
        {
            _db = db;
        }

        private CollectionReference Colecao => _db.Collection(Colecoes.Unidades);

        // GET: Unidades
        public async Task<IActionResult> Index()
        {
            var snapshot = await Colecao.GetSnapshotAsync();
            var unidades = snapshot.Documents
                .Select(d => d.ConvertTo<Unidade>())
                .OrderBy(u => u.Nome)
                .ToList();
            return View(unidades);
        }

        // GET: Unidades/Details/abc123
        public async Task<IActionResult> Details(string id)
        {
            var unidade = await ObterAsync(id);
            if (unidade == null) return NotFound();
            return View(unidade);
        }

        // GET: Unidades/Create
        public IActionResult Create() => View();

        // POST: Unidades/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nome,Cidade,Estado")] Unidade unidade)
        {
            if (!ModelState.IsValid) return View(unidade);

            await Colecao.AddAsync(unidade);
            return RedirectToAction(nameof(Index));
        }

        // GET: Unidades/Edit/abc123
        public async Task<IActionResult> Edit(string id)
        {
            var unidade = await ObterAsync(id);
            if (unidade == null) return NotFound();
            return View(unidade);
        }

        // POST: Unidades/Edit/abc123
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,Nome,Cidade,Estado")] Unidade unidade)
        {
            if (id != unidade.Id) return NotFound();
            if (await ObterAsync(id) == null) return NotFound();
            if (!ModelState.IsValid) return View(unidade);

            await Colecao.Document(id).SetAsync(unidade);
            return RedirectToAction(nameof(Index));
        }

        // GET: Unidades/Delete/abc123
        public async Task<IActionResult> Delete(string id)
        {
            var unidade = await ObterAsync(id);
            if (unidade == null) return NotFound();
            return View(unidade);
        }

        // POST: Unidades/Delete/abc123
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            // O Firestore não tem chave estrangeira: a checagem é feita aqui.
            var setores = await _db.Collection(Colecoes.Setores)
                .WhereEqualTo(nameof(SetorArea.IdUnidade), id)
                .Limit(1)
                .GetSnapshotAsync();

            if (setores.Count > 0)
            {
                TempData["Erro"] = "Não é possível excluir: existem setores vinculados a esta unidade.";
                return RedirectToAction(nameof(Index));
            }

            await Colecao.Document(id).DeleteAsync();
            return RedirectToAction(nameof(Index));
        }

        // ---------- Auxiliares ----------

        private async Task<Unidade?> ObterAsync(string? id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            var snapshot = await Colecao.Document(id).GetSnapshotAsync();
            return snapshot.Exists ? snapshot.ConvertTo<Unidade>() : null;
        }
    }
}