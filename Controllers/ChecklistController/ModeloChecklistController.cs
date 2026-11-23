using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProjetoISP.Baldan.Models.Checklist;
using ProjetoISP.Baldan.Models.Inspecoes;
using ProjetoISP.Baldan.Models.Setores;

namespace ProjetoISP.Baldan.Controllers
{
    public class ModeloChecklistController : Controller
    {
        private static readonly string[] StatusDisponiveis = { "Ativo", "Inativo" };

        private readonly FirestoreDb _db;

        public ModeloChecklistController(FirestoreDb db)
        {
            _db = db;
        }

        private CollectionReference Colecao => _db.Collection(Colecoes.ModelosChecklist);

        // GET: ModeloChecklist  (lista de checklists; daqui se chega na tela "Editar Checklist")
        public async Task<IActionResult> Index()
        {
            var snapshot = await Colecao.GetSnapshotAsync();
            var modelos = snapshot.Documents
                .Select(d => d.ConvertTo<ModeloChecklist>())
                .OrderBy(m => m.Nome)
                .ToList();

            var categorias = await _db.Collection(Colecoes.Categorias).GetSnapshotAsync();
            ViewBag.NomesCategorias = categorias.Documents.ToDictionary(d => d.Id, d => d.ConvertTo<Categoria>().Nome);

            // modelo -> nomes dos setores onde é aplicado
            var setores = await _db.Collection(Colecoes.Setores).GetSnapshotAsync();
            var nomesSetores = setores.Documents.ToDictionary(d => d.Id, d => d.ConvertTo<SetorArea>().Nome);

            var vinculos = await _db.Collection(Colecoes.ModelosSetores).GetSnapshotAsync();
            ViewBag.SetoresPorModelo = vinculos.Documents
                .Select(d => d.ConvertTo<ModeloChecklistSetor>())
                .Where(v => nomesSetores.ContainsKey(v.IdSetor))
                .GroupBy(v => v.IdModeloChecklist)
                .ToDictionary(g => g.Key, g => g.Select(v => nomesSetores[v.IdSetor]).OrderBy(n => n).ToList());

            return View(modelos);
        }

        // GET: ModeloChecklist/Create
        public async Task<IActionResult> Create()
        {
            await CarregarListasAsync(null, new HashSet<string>());
            return View();
        }

        // POST: ModeloChecklist/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Nome,Versao,Status,IdCategoria")] ModeloChecklist modelo, string[]? idsSetores)
        {
            var setoresValidos = await FiltrarSetoresExistentesAsync(idsSetores);
            await ValidarAsync(modelo);

            if (!ModelState.IsValid)
            {
                await CarregarListasAsync(modelo, setoresValidos);
                return View(modelo);
            }

            modelo.DataCriacao = Timestamp.GetCurrentTimestamp();

            var batch = _db.StartBatch();
            var documento = Colecao.Document();
            batch.Create(documento, modelo);
            foreach (var idSetor in setoresValidos)
            {
                batch.Create(_db.Collection(Colecoes.ModelosSetores).Document(),
                    new ModeloChecklistSetor { IdModeloChecklist = documento.Id, IdSetor = idSetor });
            }
            await batch.CommitAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: ModeloChecklist/Edit/abc123
        public async Task<IActionResult> Edit(string id)
        {
            var modelo = await ObterAsync(id);
            if (modelo == null) return NotFound();

            await CarregarListasAsync(modelo, await IdsSetoresDoModeloAsync(id));
            return View(modelo);
        }

        // POST: ModeloChecklist/Edit/abc123
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id, [Bind("Id,Nome,Versao,Status,IdCategoria")] ModeloChecklist modelo, string[]? idsSetores)
        {
            if (id != modelo.Id) return NotFound();

            var existente = await ObterAsync(id);
            if (existente == null) return NotFound();

            var setoresValidos = await FiltrarSetoresExistentesAsync(idsSetores);
            await ValidarAsync(modelo);

            if (!ModelState.IsValid)
            {
                await CarregarListasAsync(modelo, setoresValidos);
                return View(modelo);
            }

            // Altera só os campos editáveis, preservando DataCriacao
            existente.Nome = modelo.Nome;
            existente.Versao = modelo.Versao;
            existente.Status = modelo.Status;
            existente.IdCategoria = modelo.IdCategoria;

            var batch = _db.StartBatch();
            batch.Set(Colecao.Document(id), existente);
            await SincronizarVinculosAsync(batch, id, setoresValidos);
            await batch.CommitAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: ModeloChecklist/Excluir/abc123
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(string id)
        {
            var itens = await _db.Collection(Colecoes.ChecklistPerguntas)
                .WhereEqualTo(nameof(ChecklistPergunta.IdModeloChecklist), id)
                            .Limit(1)
                .GetSnapshotAsync();

            var inspecoes = await _db.Collection(Colecoes.Inspecoes)
                .WhereEqualTo(nameof(InspecaoISP.IdModeloChecklist), id)
                .Limit(1)
                .GetSnapshotAsync();

            if (itens.Count > 0 || inspecoes.Count > 0)
            {
                TempData["Erro"] = "Não é possível excluir: o checklist tem perguntas ou inspeções vinculadas.";
                return RedirectToAction(nameof(Index));
            }

            // Sem perguntas nem inspeções: remove também os vínculos com setores
            var vinculos = await _db.Collection(Colecoes.ModelosSetores)
                .WhereEqualTo(nameof(ModeloChecklistSetor.IdModeloChecklist), id)
                .GetSnapshotAsync();

            var batch = _db.StartBatch();
            foreach (var doc in vinculos.Documents) batch.Delete(doc.Reference);
            batch.Delete(Colecao.Document(id));
            await batch.CommitAsync();

            return RedirectToAction(nameof(Index));
        }

        // ---------- Auxiliares ----------

        private async Task<ModeloChecklist?> ObterAsync(string? id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            var snapshot = await Colecao.Document(id).GetSnapshotAsync();
            return snapshot.Exists ? snapshot.ConvertTo<ModeloChecklist>() : null;
        }

        private async Task<HashSet<string>> IdsSetoresDoModeloAsync(string idModelo)
        {
            var vinculos = await _db.Collection(Colecoes.ModelosSetores)
                .WhereEqualTo(nameof(ModeloChecklistSetor.IdModeloChecklist), idModelo)
                .GetSnapshotAsync();
            return vinculos.Documents.Select(d => d.ConvertTo<ModeloChecklistSetor>().IdSetor).ToHashSet();
        }

        // Só aceita IDs de setores que existem de verdade
        private async Task<HashSet<string>> FiltrarSetoresExistentesAsync(string[]? idsSetores)
        {
            if (idsSetores == null || idsSetores.Length == 0) return new HashSet<string>();

            var snapshot = await _db.Collection(Colecoes.Setores).GetSnapshotAsync();
            var existentes = snapshot.Documents.Select(d => d.Id).ToHashSet();
            return idsSetores.Where(existentes.Contains).ToHashSet();
        }

        // Deixa os vínculos do modelo exatamente iguais à seleção: apaga os removidos e cria os novos
        private async Task SincronizarVinculosAsync(WriteBatch batch, string idModelo, HashSet<string> selecionados)
        {
            var vinculos = await _db.Collection(Colecoes.ModelosSetores)
                .WhereEqualTo(nameof(ModeloChecklistSetor.IdModeloChecklist), idModelo)
                .GetSnapshotAsync();

            var jaVinculados = new HashSet<string>();
            foreach (var doc in vinculos.Documents)
            {
                var idSetor = doc.ConvertTo<ModeloChecklistSetor>().IdSetor;
                if (!selecionados.Contains(idSetor) || !jaVinculados.Add(idSetor))
                    batch.Delete(doc.Reference);
            }

            foreach (var idSetor in selecionados.Where(s => !jaVinculados.Contains(s)))
            {
                batch.Create(_db.Collection(Colecoes.ModelosSetores).Document(),
                    new ModeloChecklistSetor { IdModeloChecklist = idModelo, IdSetor = idSetor });
            }
        }

        private async Task CarregarListasAsync(ModeloChecklist? modelo, HashSet<string> setoresSelecionados)
        {
            var categoriasSnap = await _db.Collection(Colecoes.Categorias).GetSnapshotAsync();
            var categorias = categoriasSnap.Documents
                .Select(d => d.ConvertTo<Categoria>())
                .OrderBy(c => c.Nome)
                .ToList();
            ViewBag.Categorias = new SelectList(categorias, nameof(Categoria.Id), nameof(Categoria.Nome), modelo?.IdCategoria);

            var setoresSnap = await _db.Collection(Colecoes.Setores).GetSnapshotAsync();
            ViewBag.SetoresDisponiveis = setoresSnap.Documents
                .Select(d => d.ConvertTo<SetorArea>())
                .OrderBy(s => s.Nome)
                .ToList();
            ViewBag.SetoresSelecionados = setoresSelecionados;

            ViewBag.StatusLista = new SelectList(StatusDisponiveis, modelo?.Status);
        }

        private async Task ValidarAsync(ModeloChecklist modelo)
        {
            if (!StatusDisponiveis.Contains(modelo.Status))
                ModelState.AddModelError(nameof(ModeloChecklist.Status), "Status inválido.");

            if (string.IsNullOrEmpty(modelo.IdCategoria)) return;

            var snapshot = await _db.Collection(Colecoes.Categorias).Document(modelo.IdCategoria).GetSnapshotAsync();
            if (!snapshot.Exists)
                ModelState.AddModelError(nameof(ModeloChecklist.IdCategoria), "Categoria inválida.");
        }
    }
}
