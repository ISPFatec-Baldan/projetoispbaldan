using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using ProjetoISP.Baldan.Models.Checklist;

namespace ProjetoISP.Baldan.Controllers
{
    public class CategoriaController : Controller
    {
        private readonly FirestoreDb _db;

        public CategoriaController(FirestoreDb db)
        {
            _db = db;
        }

        private CollectionReference Colecao => _db.Collection(Colecoes.Categorias);

        // GET: Categoria
        public async Task<IActionResult> Index()
        {
            var snapshot = await Colecao.GetSnapshotAsync();
            var categorias = snapshot.Documents
                .Select(d => d.ConvertTo<Categoria>())
                .OrderBy(c => c.Nome)
                .ToList();
            return View(categorias);
        }

        // GET: Categoria/Details/abc123
        public async Task<IActionResult> Details(string id)
        {
            var categoria = await ObterAsync(id);
            if (categoria == null) return NotFound();
            return View(categoria);
        }

        // GET: Categoria/Create
        public IActionResult Create() => View();

        // POST: Categoria/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nome")] Categoria categoria)
        {
            if (!ModelState.IsValid) return View(categoria);

            await Colecao.AddAsync(categoria);
            return RedirectToAction(nameof(Index));
        }

        // GET: Categoria/Edit/abc123
        public async Task<IActionResult> Edit(string id)
        {
            var categoria = await ObterAsync(id);
            if (categoria == null) return NotFound();
            return View(categoria);
        }

        // POST: Categoria/Edit/abc123
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,Nome")] Categoria categoria)
        {
            if (id != categoria.Id) return NotFound();
            if (await ObterAsync(id) == null) return NotFound();
            if (!ModelState.IsValid) return View(categoria);

            await Colecao.Document(id).SetAsync(categoria);
            return RedirectToAction(nameof(Index));
        }

        // GET: Categoria/Delete/abc123
        public async Task<IActionResult> Delete(string id)
        {
            var categoria = await ObterAsync(id);
            if (categoria == null) return NotFound();
            return View(categoria);
        }

        // POST: Categoria/Delete/abc123
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var modelos = await _db.Collection(Colecoes.ModelosChecklist)
                .WhereEqualTo(nameof(ModeloChecklist.IdCategoria), id)
                .Limit(1)
                .GetSnapshotAsync();

            if (modelos.Count > 0)
            {
                TempData["Erro"] = "Não é possível excluir: existem modelos de checklist nesta categoria.";
                return RedirectToAction(nameof(Index));
            }

            await Colecao.Document(id).DeleteAsync();
            return RedirectToAction(nameof(Index));
        }

        // ---------- Auxiliares ----------

        private async Task<Categoria?> ObterAsync(string? id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            var snapshot = await Colecao.Document(id).GetSnapshotAsync();
            return snapshot.Exists ? snapshot.ConvertTo<Categoria>() : null;
        }
    }
}