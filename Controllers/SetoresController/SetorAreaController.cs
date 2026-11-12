using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProjetoISP.Baldan.Models.Acesso;
using ProjetoISP.Baldan.Models.Inspecoes;
using ProjetoISP.Baldan.Models.Setores;

namespace ProjetoISP.Baldan.Controllers
{
    public class SetorAreaController : Controller
    {
        private readonly FirestoreDb _db;

        public SetorAreaController(FirestoreDb db)
        {
            _db = db;
        }

        private CollectionReference Colecao => _db.Collection(Colecoes.Setores);

        // GET: SetorArea
        public async Task<IActionResult> Index()
        {
            var snapshot = await Colecao.GetSnapshotAsync();
            var setores = snapshot.Documents
                .Select(d => d.ConvertTo<SetorArea>())
                .OrderBy(s => s.Nome)
                .ToList();

            ViewBag.NomesUnidades = await NomesUnidadesAsync();
            return View(setores);
        }

        // GET: SetorArea/Details/abc123
        public async Task<IActionResult> Details(string id)
        {
            var setor = await ObterAsync(id);
            if (setor == null) return NotFound();

            ViewBag.NomeUnidade = await NomeUnidadeAsync(setor.IdUnidade);
            return View(setor);
        }

        // GET: SetorArea/Create
        public async Task<IActionResult> Create()
        {
            await CarregarUnidadesAsync();
            return View();
        }

        // POST: SetorArea/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nome,UrlFotoEquipe,IdUnidade")] SetorArea setor)
        {
            await ValidarUnidadeAsync(setor);

            if (!ModelState.IsValid)
            {
                await CarregarUnidadesAsync(setor.IdUnidade);
                return View(setor);
            }

            setor.DataCriacao = Timestamp.GetCurrentTimestamp();
            await Colecao.AddAsync(setor);
            return RedirectToAction(nameof(Index));
        }

        // GET: SetorArea/Edit/abc123
        public async Task<IActionResult> Edit(string id)
        {
            var setor = await ObterAsync(id);
            if (setor == null) return NotFound();

            await CarregarUnidadesAsync(setor.IdUnidade);
            return View(setor);
        }

        // POST: SetorArea/Edit/abc123
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,Nome,UrlFotoEquipe,IdUnidade")] SetorArea setor)
        {
            if (id != setor.Id) return NotFound();

            var existente = await ObterAsync(id);
            if (existente == null) return NotFound();

            await ValidarUnidadeAsync(setor);

            if (!ModelState.IsValid)
            {
                await CarregarUnidadesAsync(setor.IdUnidade);
                return View(setor);
            }

            // Altera só os campos editáveis, preservando DataCriacao
            existente.Nome = setor.Nome;
            existente.UrlFotoEquipe = setor.UrlFotoEquipe;
            existente.IdUnidade = setor.IdUnidade;

            await Colecao.Document(id).SetAsync(existente);
            return RedirectToAction(nameof(Index));
        }

        // GET: SetorArea/Delete/abc123
        public async Task<IActionResult> Delete(string id)
        {
            var setor = await ObterAsync(id);
            if (setor == null) return NotFound();

            ViewBag.NomeUnidade = await NomeUnidadeAsync(setor.IdUnidade);
            return View(setor);
        }

        // POST: SetorArea/Delete/abc123
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var usuarios = await _db.Collection(Colecoes.Usuarios)
                .WhereEqualTo(nameof(Usuario.IdSetor), id)
                .Limit(1)
                .GetSnapshotAsync();

            var inspecoes = await _db.Collection(Colecoes.Inspecoes)
                .WhereEqualTo(nameof(InspecaoISP.IdSetor), id)
                .Limit(1)
                .GetSnapshotAsync();

            if (usuarios.Count > 0 || inspecoes.Count > 0)
            {
                TempData["Erro"] = "Não é possível excluir: existem usuários ou inspeções vinculados a este setor.";
                return RedirectToAction(nameof(Index));
            }

            await Colecao.Document(id).DeleteAsync();
            return RedirectToAction(nameof(Index));
        }

        // ---------- Auxiliares ----------

        private async Task<SetorArea?> ObterAsync(string? id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            var snapshot = await Colecao.Document(id).GetSnapshotAsync();
            return snapshot.Exists ? snapshot.ConvertTo<SetorArea>() : null;
        }

        private async Task<Dictionary<string, string>> NomesUnidadesAsync()
        {
            var snapshot = await _db.Collection(Colecoes.Unidades).GetSnapshotAsync();
            return snapshot.Documents.ToDictionary(d => d.Id, d => d.ConvertTo<Unidade>().Nome);
        }

        private async Task<string> NomeUnidadeAsync(string? idUnidade)
        {
            if (string.IsNullOrEmpty(idUnidade)) return "—";

            var snapshot = await _db.Collection(Colecoes.Unidades).Document(idUnidade).GetSnapshotAsync();
            return snapshot.Exists ? snapshot.ConvertTo<Unidade>().Nome : "—";
        }

        private async Task CarregarUnidadesAsync(string? selecionada = null)
        {
            var snapshot = await _db.Collection(Colecoes.Unidades).GetSnapshotAsync();
            var unidades = snapshot.Documents
                .Select(d => d.ConvertTo<Unidade>())
                .OrderBy(u => u.Nome)
                .ToList();

            ViewBag.Unidades = new SelectList(unidades, nameof(Unidade.Id), nameof(Unidade.Nome), selecionada);
        }

        private async Task ValidarUnidadeAsync(SetorArea setor)
        {
            if (string.IsNullOrEmpty(setor.IdUnidade)) return;

            var snapshot = await _db.Collection(Colecoes.Unidades).Document(setor.IdUnidade).GetSnapshotAsync();
            if (!snapshot.Exists)
                ModelState.AddModelError(nameof(SetorArea.IdUnidade), "Unidade inválida.");
        }
    }
}