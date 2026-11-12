using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProjetoISP.Baldan.Models.Acesso;
using ProjetoISP.Baldan.Models.Checklist;
using ProjetoISP.Baldan.Models.Inspecoes;
using ProjetoISP.Baldan.Models.Setores;

namespace ProjetoISP.Baldan.Controllers
{
    public class InspecoesISPController : Controller
    {
        // Status usados no fluxo da inspeção. Ajuste os textos se a Baldan usar outros.
        private const string StatusEmAndamento = "Em andamento";
        private static readonly string[] StatusDisponiveis = { StatusEmAndamento, "Finalizada", "Cancelada" };

        private readonly FirestoreDb _db;

        public InspecoesISPController(FirestoreDb db)
        {
            _db = db;
        }

        private CollectionReference Colecao => _db.Collection(Colecoes.Inspecoes);

        // GET: InspecoesISP
        public async Task<IActionResult> Index()
        {
            var snapshot = await Colecao
                .OrderByDescending(nameof(InspecaoISP.DataHoraInicio))
                .GetSnapshotAsync();

            var inspecoes = snapshot.Documents.Select(d => d.ConvertTo<InspecaoISP>()).ToList();

            await CarregarNomesAsync();
            return View(inspecoes);
        }

        // GET: InspecoesISP/Details/abc123
        public async Task<IActionResult> Details(string id)
        {
            var inspecao = await ObterAsync(id);
            if (inspecao == null) return NotFound();

            await CarregarNomesAsync();
            return View(inspecao);
        }

        // GET: InspecoesISP/Create
        public async Task<IActionResult> Create()
        {
            await CarregarListasAsync();
            return View();
        }

        // POST: InspecoesISP/Create
        // Início e status são definidos pelo servidor: a inspeção nasce "Em andamento".
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdModeloChecklist,IdInspetorUsuario,IdSetor")] InspecaoISP inspecao)
        {
            inspecao.Status = StatusEmAndamento;
            IgnorarValidacao(nameof(InspecaoISP.Status));

            await ValidarReferenciasAsync(inspecao, validarModelo: true);

            if (!ModelState.IsValid)
            {
                await CarregarListasAsync(inspecao);
                return View(inspecao);
            }

            inspecao.DataHoraInicio = Timestamp.GetCurrentTimestamp();
            inspecao.DataHoraFim = null;

            await Colecao.AddAsync(inspecao);
            return RedirectToAction(nameof(Index));
        }

        // GET: InspecoesISP/Edit/abc123
        public async Task<IActionResult> Edit(string id)
        {
            var inspecao = await ObterAsync(id);
            if (inspecao == null) return NotFound();

            await CarregarListasAsync(inspecao);
            return View(inspecao);
        }

        // POST: InspecoesISP/Edit/abc123
        // O modelo de checklist NÃO é editável (os resultados dependem dele).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,Status,IdInspetorUsuario,IdSetor")] InspecaoISP inspecao)
        {
            if (id != inspecao.Id) return NotFound();

            var existente = await ObterAsync(id);
            if (existente == null) return NotFound();

            IgnorarValidacao(nameof(InspecaoISP.IdModeloChecklist));

            if (!StatusDisponiveis.Contains(inspecao.Status))
                ModelState.AddModelError(nameof(InspecaoISP.Status), "Status inválido.");

            await ValidarReferenciasAsync(inspecao, validarModelo: false);

            if (!ModelState.IsValid)
            {
                inspecao.IdModeloChecklist = existente.IdModeloChecklist;
                inspecao.DataHoraInicio = existente.DataHoraInicio;
                inspecao.DataHoraFim = existente.DataHoraFim;
                await CarregarListasAsync(inspecao);
                return View(inspecao);
            }

            existente.Status = inspecao.Status;
            existente.IdInspetorUsuario = inspecao.IdInspetorUsuario;
            existente.IdSetor = inspecao.IdSetor;

            // Encerrada (finalizada/cancelada) registra o fim; voltar para "Em andamento" limpa o fim.
            if (inspecao.Status == StatusEmAndamento)
                existente.DataHoraFim = null;
            else
                existente.DataHoraFim = existente.DataHoraFim ?? Timestamp.GetCurrentTimestamp();

            await Colecao.Document(id).SetAsync(existente);
            return RedirectToAction(nameof(Index));
        }

        // GET: InspecoesISP/Delete/abc123
        public async Task<IActionResult> Delete(string id)
        {
            var inspecao = await ObterAsync(id);
            if (inspecao == null) return NotFound();

            await CarregarNomesAsync();
            return View(inspecao);
        }

        // POST: InspecoesISP/Delete/abc123
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var resultados = await _db.Collection(Colecoes.ResultadosInspecao)
                .WhereEqualTo(nameof(ResultadoInspecao.IdInspecao), id)
                .Limit(1)
                .GetSnapshotAsync();

            if (resultados.Count > 0)
            {
                TempData["Erro"] = "Não é possível excluir: esta inspeção já tem resultados registrados.";
                return RedirectToAction(nameof(Index));
            }

            await Colecao.Document(id).DeleteAsync();
            return RedirectToAction(nameof(Index));
        }

        // ---------- Auxiliares ----------

        private async Task<InspecaoISP?> ObterAsync(string? id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            var snapshot = await Colecao.Document(id).GetSnapshotAsync();
            return snapshot.Exists ? snapshot.ConvertTo<InspecaoISP>() : null;
        }

        // Dicionários id -> nome, para mostrar nomes em vez de IDs nas telas de leitura.
        private async Task CarregarNomesAsync()
        {
            var modelos = await _db.Collection(Colecoes.ModelosChecklist).GetSnapshotAsync();
            ViewBag.NomesModelos = modelos.Documents.ToDictionary(d => d.Id, d =>
            {
                var m = d.ConvertTo<ModeloChecklist>();
                return $"{m.Nome} (v{m.Versao})";
            });

            var setores = await _db.Collection(Colecoes.Setores).GetSnapshotAsync();
            ViewBag.NomesSetores = setores.Documents.ToDictionary(d => d.Id, d => d.ConvertTo<SetorArea>().Nome);

            var usuarios = await _db.Collection(Colecoes.Usuarios).GetSnapshotAsync();
            ViewBag.NomesUsuarios = usuarios.Documents.ToDictionary(d => d.Id, d => d.ConvertTo<Usuario>().Nome);
        }

        // Listas dos dropdowns (e nome do modelo para exibição na edição).
        private async Task CarregarListasAsync(InspecaoISP? inspecao = null)
        {
            var modelosSnap = await _db.Collection(Colecoes.ModelosChecklist).GetSnapshotAsync();
            var modelos = modelosSnap.Documents
                .Select(d => d.ConvertTo<ModeloChecklist>())
                .OrderBy(m => m.Nome)
                .Select(m => new { m.Id, Rotulo = $"{m.Nome} (v{m.Versao})" })
                .ToList();
            ViewBag.Modelos = new SelectList(modelos, "Id", "Rotulo", inspecao?.IdModeloChecklist);
            ViewBag.NomeModelo = modelos.FirstOrDefault(m => m.Id == inspecao?.IdModeloChecklist)?.Rotulo ?? "—";

            var setoresSnap = await _db.Collection(Colecoes.Setores).GetSnapshotAsync();
            var setores = setoresSnap.Documents
                .Select(d => d.ConvertTo<SetorArea>())
                .OrderBy(s => s.Nome)
                .ToList();
            ViewBag.Setores = new SelectList(setores, nameof(SetorArea.Id), nameof(SetorArea.Nome), inspecao?.IdSetor);

            var usuariosSnap = await _db.Collection(Colecoes.Usuarios).GetSnapshotAsync();
            var usuarios = usuariosSnap.Documents
                .Select(d => d.ConvertTo<Usuario>())
                .OrderBy(u => u.Nome)
                .ToList();
            ViewBag.Inspetores = new SelectList(usuarios, nameof(Usuario.Id), nameof(Usuario.Nome), inspecao?.IdInspetorUsuario);

            ViewBag.StatusLista = new SelectList(StatusDisponiveis, inspecao?.Status);
        }

        private async Task ValidarReferenciasAsync(InspecaoISP inspecao, bool validarModelo)
        {
            if (validarModelo && await ReferenciaInvalidaAsync(Colecoes.ModelosChecklist, inspecao.IdModeloChecklist))
                ModelState.AddModelError(nameof(InspecaoISP.IdModeloChecklist), "Modelo de checklist inválido.");

            if (await ReferenciaInvalidaAsync(Colecoes.Usuarios, inspecao.IdInspetorUsuario))
                ModelState.AddModelError(nameof(InspecaoISP.IdInspetorUsuario), "Inspetor inválido.");

            if (await ReferenciaInvalidaAsync(Colecoes.Setores, inspecao.IdSetor))
                ModelState.AddModelError(nameof(InspecaoISP.IdSetor), "Setor inválido.");
        }

        // true só quando o ID foi informado e NÃO existe na coleção.
        // (ID vazio já é pego pela validação de campo obrigatório.)
        private async Task<bool> ReferenciaInvalidaAsync(string colecao, string? id)
        {
            if (string.IsNullOrEmpty(id)) return false;

            var snapshot = await _db.Collection(colecao).Document(id).GetSnapshotAsync();
            return !snapshot.Exists;
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