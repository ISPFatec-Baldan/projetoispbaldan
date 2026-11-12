using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProjetoISP.Baldan.Models.Checklist;
using ProjetoISP.Baldan.Models.Inspecoes;

namespace ProjetoISP.Baldan.Controllers
{
    public class ItemChecklistController : Controller
    {
        private readonly FirestoreDb _db;

        public ItemChecklistController(FirestoreDb db)
        {
            _db = db;
        }

        private CollectionReference Colecao => _db.Collection(Colecoes.ItensChecklist);

        // GET: ItemChecklist  (opcional: ?idModeloChecklist=abc123 para filtrar por modelo)
        public async Task<IActionResult> Index(string? idModeloChecklist)
        {
            Query consulta = Colecao;
            if (!string.IsNullOrEmpty(idModeloChecklist))
                consulta = consulta.WhereEqualTo(nameof(ItemChecklist.IdModeloChecklist), idModeloChecklist);

            var snapshot = await consulta.GetSnapshotAsync();
            var itens = snapshot.Documents
                .Select(d => d.ConvertTo<ItemChecklist>())
                .OrderBy(i => i.DataCriacao.ToDateTime())
                .ToList();

            ViewBag.IdModeloChecklist = idModeloChecklist;
            ViewBag.NomesModelos = await NomesModelosAsync();
            return View(itens);
        }

        // GET: ItemChecklist/Details/abc123
        public async Task<IActionResult> Details(string id)
        {
            var item = await ObterAsync(id);
            if (item == null) return NotFound();

            ViewBag.NomeModelo = await NomeModeloAsync(item.IdModeloChecklist);
            return View(item);
        }

        // GET: ItemChecklist/Create  (opcional: ?idModeloChecklist=abc123 já seleciona o modelo)
        public async Task<IActionResult> Create(string? idModeloChecklist)
        {
            await CarregarModelosAsync(idModeloChecklist);
            return View(new ItemChecklist { IdModeloChecklist = idModeloChecklist ?? string.Empty });
        }

        // POST: ItemChecklist/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DescricaoPergunta,TipoResposta,IdModeloChecklist")] ItemChecklist item)
        {
            await ValidarModeloAsync(item);

            if (!ModelState.IsValid)
            {
                await CarregarModelosAsync(item.IdModeloChecklist);
                return View(item);
            }

            item.DataCriacao = Timestamp.GetCurrentTimestamp();
            await Colecao.AddAsync(item);
            return RedirectToAction(nameof(Index), new { idModeloChecklist = item.IdModeloChecklist });
        }

        // GET: ItemChecklist/Edit/abc123
        public async Task<IActionResult> Edit(string id)
        {
            var item = await ObterAsync(id);
            if (item == null) return NotFound();

            ViewBag.NomeModelo = await NomeModeloAsync(item.IdModeloChecklist);
            return View(item);
        }

        // POST: ItemChecklist/Edit/abc123
        // O modelo do item NÃO é editável (resultados de inspeções dependem dele).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,DescricaoPergunta,TipoResposta")] ItemChecklist item)
        {
            if (id != item.Id) return NotFound();

            var existente = await ObterAsync(id);
            if (existente == null) return NotFound();

            // IdModeloChecklist não vem do formulário; evita o erro de "campo obrigatório".
            IgnorarValidacao(nameof(ItemChecklist.IdModeloChecklist));

            if (!ModelState.IsValid)
            {
                item.IdModeloChecklist = existente.IdModeloChecklist;
                ViewBag.NomeModelo = await NomeModeloAsync(existente.IdModeloChecklist);
                return View(item);
            }

            // Altera só os campos editáveis, preservando DataCriacao e IdModeloChecklist
            existente.DescricaoPergunta = item.DescricaoPergunta;
            existente.TipoResposta = item.TipoResposta;

            await Colecao.Document(id).SetAsync(existente);
            return RedirectToAction(nameof(Index), new { idModeloChecklist = existente.IdModeloChecklist });
        }

        // GET: ItemChecklist/Delete/abc123
        public async Task<IActionResult> Delete(string id)
        {
            var item = await ObterAsync(id);
            if (item == null) return NotFound();

            ViewBag.NomeModelo = await NomeModeloAsync(item.IdModeloChecklist);
            return View(item);
        }

        // POST: ItemChecklist/Delete/abc123
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var resultados = await _db.Collection(Colecoes.ResultadosInspecao)
                .WhereEqualTo(nameof(ResultadoInspecao.IdItemChecklist), id)
                .Limit(1)
                .GetSnapshotAsync();

            if (resultados.Count > 0)
            {
                TempData["Erro"] = "Não é possível excluir: este item já tem resultados de inspeção registrados.";
                return RedirectToAction(nameof(Index));
            }

            await Colecao.Document(id).DeleteAsync();
            return RedirectToAction(nameof(Index));
        }

        // ---------- Auxiliares ----------

        private async Task<ItemChecklist?> ObterAsync(string? id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            var snapshot = await Colecao.Document(id).GetSnapshotAsync();
            return snapshot.Exists ? snapshot.ConvertTo<ItemChecklist>() : null;
        }

        private async Task<Dictionary<string, string>> NomesModelosAsync()
        {
            var snapshot = await _db.Collection(Colecoes.ModelosChecklist).GetSnapshotAsync();
            return snapshot.Documents.ToDictionary(d => d.Id, d =>
            {
                var m = d.ConvertTo<ModeloChecklist>();
                return $"{m.Nome} (v{m.Versao})";
            });
        }

        private async Task<string> NomeModeloAsync(string? idModelo)
        {
            if (string.IsNullOrEmpty(idModelo)) return "—";

            var snapshot = await _db.Collection(Colecoes.ModelosChecklist).Document(idModelo).GetSnapshotAsync();
            if (!snapshot.Exists) return "—";

            var m = snapshot.ConvertTo<ModeloChecklist>();
            return $"{m.Nome} (v{m.Versao})";
        }

        private async Task CarregarModelosAsync(string? selecionado = null)
        {
            var snapshot = await _db.Collection(Colecoes.ModelosChecklist).GetSnapshotAsync();
            var modelos = snapshot.Documents
                .Select(d => d.ConvertTo<ModeloChecklist>())
                .OrderBy(m => m.Nome)
                .Select(m => new { m.Id, Rotulo = $"{m.Nome} (v{m.Versao})" })
                .ToList();

            ViewBag.Modelos = new SelectList(modelos, "Id", "Rotulo", selecionado);
        }

        private async Task ValidarModeloAsync(ItemChecklist item)
        {
            if (string.IsNullOrEmpty(item.IdModeloChecklist)) return;

            var snapshot = await _db.Collection(Colecoes.ModelosChecklist).Document(item.IdModeloChecklist).GetSnapshotAsync();
            if (!snapshot.Exists)
                ModelState.AddModelError(nameof(ItemChecklist.IdModeloChecklist), "Modelo de checklist inválido.");
        }

        // Remove do ModelState os erros de campos que não vêm do formulário.
        private void IgnorarValidacao(params string[] campos)
        {
            foreach (var campo in campos)
            {
                var chaves = ModelState.Keys
                    .Where(k => k == campo || k.EndsWith("." + campo))
                    .ToList();

                foreach (var chave in chaves)
                    ModelState.Remove(chave);
            }
        }
    }
}