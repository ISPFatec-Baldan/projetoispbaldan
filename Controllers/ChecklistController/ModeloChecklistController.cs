using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProjetoISP.Baldan.Models.Checklist;
using ProjetoISP.Baldan.Models.Inspecoes;

namespace ProjetoISP.Baldan.Controllers
{
    public class ModeloChecklistController : Controller
    {
        private readonly FirestoreDb _db;

        public ModeloChecklistController(FirestoreDb db)
        {
            _db = db;
        }

        private CollectionReference Colecao => _db.Collection(Colecoes.ModelosChecklist);

        // GET: ModeloChecklist
        public async Task<IActionResult> Index()
        {
            var snapshot = await Colecao.GetSnapshotAsync();
            var modelos = snapshot.Documents
                .Select(d => d.ConvertTo<ModeloChecklist>())
                .OrderBy(m => m.Nome)
                .ToList();

            ViewBag.NomesCategorias = await NomesCategoriasAsync();
            return View(modelos);
        }

        // GET: ModeloChecklist/Details/abc123
        public async Task<IActionResult> Details(string id)
        {
            var modelo = await ObterAsync(id);
            if (modelo == null) return NotFound();

            ViewBag.NomeCategoria = await NomeCategoriaAsync(modelo.IdCategoria);
            return View(modelo);
        }

        // GET: ModeloChecklist/Create
        public async Task<IActionResult> Create()
        {
            await CarregarCategoriasAsync();
            return View();
        }

        // POST: ModeloChecklist/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nome,Versao,Status,IdCategoria")] ModeloChecklist modelo)
        {
            await ValidarCategoriaAsync(modelo);

            if (!ModelState.IsValid)
            {
                await CarregarCategoriasAsync(modelo.IdCategoria);
                return View(modelo);
            }

            modelo.DataCriacao = Timestamp.GetCurrentTimestamp();
            await Colecao.AddAsync(modelo);
            return RedirectToAction(nameof(Index));
        }

        // GET: ModeloChecklist/Edit/abc123
        public async Task<IActionResult> Edit(string id)
        {
            var modelo = await ObterAsync(id);
            if (modelo == null) return NotFound();

            await CarregarCategoriasAsync(modelo.IdCategoria);
            return View(modelo);
        }

        // POST: ModeloChecklist/Edit/abc123
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,Nome,Versao,Status,IdCategoria")] ModeloChecklist modelo)
        {
            if (id != modelo.Id) return NotFound();

            var existente = await ObterAsync(id);
            if (existente == null) return NotFound();

            await ValidarCategoriaAsync(modelo);

            if (!ModelState.IsValid)
            {
                await CarregarCategoriasAsync(modelo.IdCategoria);
                return View(modelo);
            }

            // Altera só os campos editáveis, preservando DataCriacao
            existente.Nome = modelo.Nome;
            existente.Versao = modelo.Versao;
            existente.Status = modelo.Status;
            existente.IdCategoria = modelo.IdCategoria;

            await Colecao.Document(id).SetAsync(existente);
            return RedirectToAction(nameof(Index));
        }

        // GET: ModeloChecklist/Delete/abc123
        public async Task<IActionResult> Delete(string id)
        {
            var modelo = await ObterAsync(id);
            if (modelo == null) return NotFound();

            ViewBag.NomeCategoria = await NomeCategoriaAsync(modelo.IdCategoria);
            return View(modelo);
        }

        // POST: ModeloChecklist/Delete/abc123
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var itens = await _db.Collection(Colecoes.ItensChecklist)
                .WhereEqualTo(nameof(ItemChecklist.IdModeloChecklist), id)
                .Limit(1)
                .GetSnapshotAsync();

            var inspecoes = await _db.Collection(Colecoes.Inspecoes)
                .WhereEqualTo(nameof(InspecaoISP.IdModeloChecklist), id)
                .Limit(1)
                .GetSnapshotAsync();

            if (itens.Count > 0 || inspecoes.Count > 0)
            {
                TempData["Erro"] = "Não é possível excluir: existem itens ou inspeções vinculados a este modelo.";
                return RedirectToAction(nameof(Index));
            }

            await Colecao.Document(id).DeleteAsync();
            return RedirectToAction(nameof(Index));
        }

        // ---------- Auxiliares ----------

        private async Task<ModeloChecklist?> ObterAsync(string? id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            var snapshot = await Colecao.Document(id).GetSnapshotAsync();
            return snapshot.Exists ? snapshot.ConvertTo<ModeloChecklist>() : null;
        }

        private async Task<Dictionary<string, string>> NomesCategoriasAsync()
        {
            var snapshot = await _db.Collection(Colecoes.Categorias).GetSnapshotAsync();
            return snapshot.Documents.ToDictionary(d => d.Id, d => d.ConvertTo<Categoria>().Nome);
        }

        private async Task<string> NomeCategoriaAsync(string? idCategoria)
        {
            if (string.IsNullOrEmpty(idCategoria)) return "—";

            var snapshot = await _db.Collection(Colecoes.Categorias).Document(idCategoria).GetSnapshotAsync();
            return snapshot.Exists ? snapshot.ConvertTo<Categoria>().Nome : "—";
        }

        private async Task CarregarCategoriasAsync(string? selecionada = null)
        {
            var snapshot = await _db.Collection(Colecoes.Categorias).GetSnapshotAsync();
            var categorias = snapshot.Documents
                .Select(d => d.ConvertTo<Categoria>())
                .OrderBy(c => c.Nome)
                .ToList();

            ViewBag.Categorias = new SelectList(categorias, nameof(Categoria.Id), nameof(Categoria.Nome), selecionada);
        }

        private async Task ValidarCategoriaAsync(ModeloChecklist modelo)
        {
            if (string.IsNullOrEmpty(modelo.IdCategoria)) return;

            var snapshot = await _db.Collection(Colecoes.Categorias).Document(modelo.IdCategoria).GetSnapshotAsync();
            if (!snapshot.Exists)
                ModelState.AddModelError(nameof(ModeloChecklist.IdCategoria), "Categoria inválida.");
        }
    }
}