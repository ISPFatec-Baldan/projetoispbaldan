using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProjetoISP.Baldan.Models.Checklist;
using ProjetoISP.Baldan.Models.Inspecoes;
using ProjetoISP.Baldan.Models.PlanosAcao;
using ProjetoISP.Baldan.Models.Setores;

namespace ProjetoISP.Baldan.Controllers
{
    public class ResultadoInspecaoController : Controller
    {
        private readonly FirestoreDb _db;

        public ResultadoInspecaoController(FirestoreDb db)
        {
            _db = db;
        }

        private CollectionReference Colecao => _db.Collection(Colecoes.ResultadosInspecao);

        // GET: ResultadoInspecao  (opcional: ?idInspecao=abc123 para ver os resultados de uma inspeção)
        public async Task<IActionResult> Index(string? idInspecao)
        {
            Query consulta = Colecao;
            if (!string.IsNullOrEmpty(idInspecao))
                consulta = consulta.WhereEqualTo(nameof(ResultadoInspecao.IdInspecao), idInspecao);

            var snapshot = await consulta.GetSnapshotAsync();
            var resultados = snapshot.Documents.Select(d => d.ConvertTo<ResultadoInspecao>()).ToList();

            ViewBag.IdInspecao = idInspecao;
            ViewBag.NomesItens = await NomesItensAsync();
            ViewBag.RotulosInspecoes = await RotulosInspecoesAsync();
            return View(resultados);
        }

        // GET: ResultadoInspecao/Details/abc123
        public async Task<IActionResult> Details(string id)
        {
            var resultado = await ObterAsync(id);
            if (resultado == null) return NotFound();

            await CarregarRotulosAsync(resultado);
            return View(resultado);
        }

        // GET: ResultadoInspecao/Create  (opcional: ?idInspecao=abc123 já seleciona a inspeção)
        public async Task<IActionResult> Create(string? idInspecao)
        {
            await CarregarListasAsync(idInspecao, null);
            return View(new ResultadoInspecao { IdInspecao = idInspecao ?? string.Empty });
        }

        // POST: ResultadoInspecao/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("RespostaColetada,Observacoes,UrlFotoEvidencia,IdInspecao,IdItemChecklist")] ResultadoInspecao resultado)
        {
            await ValidarAsync(resultado, idAtual: null);

            if (!ModelState.IsValid)
            {
                await CarregarListasAsync(resultado.IdInspecao, resultado.IdItemChecklist);
                return View(resultado);
            }

            await Colecao.AddAsync(resultado);
            return RedirectToAction(nameof(Index), new { idInspecao = resultado.IdInspecao });
        }

        // GET: ResultadoInspecao/Edit/abc123
        public async Task<IActionResult> Edit(string id)
        {
            var resultado = await ObterAsync(id);
            if (resultado == null) return NotFound();

            await CarregarRotulosAsync(resultado);
            return View(resultado);
        }

        // POST: ResultadoInspecao/Edit/abc123
        // Inspeção e item NÃO são editáveis; só a resposta, as observações e a foto.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id, [Bind("Id,RespostaColetada,Observacoes,UrlFotoEvidencia")] ResultadoInspecao resultado)
        {
            if (id != resultado.Id) return NotFound();

            var existente = await ObterAsync(id);
            if (existente == null) return NotFound();

            IgnorarValidacao(nameof(ResultadoInspecao.IdInspecao), nameof(ResultadoInspecao.IdItemChecklist));

            if (!ModelState.IsValid)
            {
                resultado.IdInspecao = existente.IdInspecao;
                resultado.IdItemChecklist = existente.IdItemChecklist;
                await CarregarRotulosAsync(resultado);
                return View(resultado);
            }

            existente.RespostaColetada = resultado.RespostaColetada;
            existente.Observacoes = resultado.Observacoes;
            existente.UrlFotoEvidencia = resultado.UrlFotoEvidencia;

            await Colecao.Document(id).SetAsync(existente);
            return RedirectToAction(nameof(Index), new { idInspecao = existente.IdInspecao });
        }

        // GET: ResultadoInspecao/Delete/abc123
        public async Task<IActionResult> Delete(string id)
        {
            var resultado = await ObterAsync(id);
            if (resultado == null) return NotFound();

            await CarregarRotulosAsync(resultado);
            return View(resultado);
        }

        // POST: ResultadoInspecao/Delete/abc123
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var planos = await _db.Collection(Colecoes.PlanosAcao)
                .WhereEqualTo(nameof(PlanoAcao5W2H.IdResultadoInspecao), id)
                .Limit(1)
                .GetSnapshotAsync();

            if (planos.Count > 0)
            {
                TempData["Erro"] = "Não é possível excluir: existe plano de ação vinculado a este resultado.";
                return RedirectToAction(nameof(Index));
            }

            var resultado = await ObterAsync(id);
            await Colecao.Document(id).DeleteAsync();
            return RedirectToAction(nameof(Index), new { idInspecao = resultado?.IdInspecao });
        }

        // ---------- Auxiliares ----------

        private async Task<ResultadoInspecao?> ObterAsync(string? id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            var snapshot = await Colecao.Document(id).GetSnapshotAsync();
            return snapshot.Exists ? snapshot.ConvertTo<ResultadoInspecao>() : null;
        }

        private async Task<Dictionary<string, string>> NomesItensAsync()
        {
            var snapshot = await _db.Collection(Colecoes.ItensChecklist).GetSnapshotAsync();
            return snapshot.Documents.ToDictionary(d => d.Id, d => d.ConvertTo<ItemChecklist>().DescricaoPergunta);
        }

        // id da inspeção -> "Setor - dd/MM/yyyy HH:mm"
        private async Task<Dictionary<string, string>> RotulosInspecoesAsync()
        {
            var setoresSnap = await _db.Collection(Colecoes.Setores).GetSnapshotAsync();
            var setores = setoresSnap.Documents.ToDictionary(d => d.Id, d => d.ConvertTo<SetorArea>().Nome);

            var inspecoesSnap = await _db.Collection(Colecoes.Inspecoes)
                .OrderByDescending(nameof(InspecaoISP.DataHoraInicio))
                .GetSnapshotAsync();

            var rotulos = new Dictionary<string, string>();
            foreach (var doc in inspecoesSnap.Documents)
            {
                var inspecao = doc.ConvertTo<InspecaoISP>();
                var setor = setores.TryGetValue(inspecao.IdSetor ?? string.Empty, out var nome) ? nome : "?";
                var inicio = inspecao.DataHoraInicio.ToDateTime().ToLocalTime();
                rotulos[doc.Id] = $"{setor} - {inicio:dd/MM/yyyy HH:mm}";
            }
            return rotulos;
        }

        // Dropdowns do Create. Se a inspeção já foi escolhida, só aparecem os itens do checklist dela.
        private async Task CarregarListasAsync(string? idInspecao, string? idItem)
        {
            var rotulos = await RotulosInspecoesAsync();
            ViewBag.Inspecoes = new SelectList(
                rotulos.Select(r => new { Id = r.Key, Rotulo = r.Value }).ToList(), "Id", "Rotulo", idInspecao);

            string? idModelo = null;
            if (!string.IsNullOrEmpty(idInspecao))
            {
                var inspecaoSnap = await _db.Collection(Colecoes.Inspecoes).Document(idInspecao).GetSnapshotAsync();
                if (inspecaoSnap.Exists)
                    idModelo = inspecaoSnap.ConvertTo<InspecaoISP>().IdModeloChecklist;
            }

            Query consultaItens = _db.Collection(Colecoes.ItensChecklist);
            if (!string.IsNullOrEmpty(idModelo))
                consultaItens = consultaItens.WhereEqualTo(nameof(ItemChecklist.IdModeloChecklist), idModelo);

            var itensSnap = await consultaItens.GetSnapshotAsync();
            var itens = itensSnap.Documents
                .Select(d => d.ConvertTo<ItemChecklist>())
                .OrderBy(i => i.DataCriacao.ToDateTime())
                .ToList();

            ViewBag.Itens = new SelectList(itens, nameof(ItemChecklist.Id), nameof(ItemChecklist.DescricaoPergunta), idItem);
        }

        // Rótulos para as telas que mostram inspeção e item como texto (Details/Edit/Delete).
        private async Task CarregarRotulosAsync(ResultadoInspecao resultado)
        {
            var rotulos = await RotulosInspecoesAsync();
            ViewBag.RotuloInspecao = rotulos.TryGetValue(resultado.IdInspecao ?? string.Empty, out var r) ? r : "—";

            var itens = await NomesItensAsync();
            ViewBag.NomeItem = itens.TryGetValue(resultado.IdItemChecklist ?? string.Empty, out var i) ? i : "—";
        }

        private async Task ValidarAsync(ResultadoInspecao resultado, string? idAtual)
        {
            InspecaoISP? inspecao = null;
            ItemChecklist? item = null;

            if (!string.IsNullOrEmpty(resultado.IdInspecao))
            {
                var snap = await _db.Collection(Colecoes.Inspecoes).Document(resultado.IdInspecao).GetSnapshotAsync();
                if (snap.Exists) inspecao = snap.ConvertTo<InspecaoISP>();
                else ModelState.AddModelError(nameof(ResultadoInspecao.IdInspecao), "Inspeção inválida.");
            }

            if (!string.IsNullOrEmpty(resultado.IdItemChecklist))
            {
                var snap = await _db.Collection(Colecoes.ItensChecklist).Document(resultado.IdItemChecklist).GetSnapshotAsync();
                if (snap.Exists) item = snap.ConvertTo<ItemChecklist>();
                else ModelState.AddModelError(nameof(ResultadoInspecao.IdItemChecklist), "Item de checklist inválido.");
            }

            if (inspecao == null || item == null) return;

            // O item precisa pertencer ao checklist usado na inspeção
            if (item.IdModeloChecklist != inspecao.IdModeloChecklist)
            {
                ModelState.AddModelError(nameof(ResultadoInspecao.IdItemChecklist),
                    "Este item não pertence ao checklist da inspeção.");
                return;
            }

            // Um item só pode ser respondido uma vez por inspeção
            var existentes = await Colecao
                .WhereEqualTo(nameof(ResultadoInspecao.IdInspecao), resultado.IdInspecao)
                .GetSnapshotAsync();

            bool jaRespondido = existentes.Documents.Any(d =>
                d.Id != idAtual &&
                d.ConvertTo<ResultadoInspecao>().IdItemChecklist == resultado.IdItemChecklist);

            if (jaRespondido)
                ModelState.AddModelError(nameof(ResultadoInspecao.IdItemChecklist),
                    "Este item já foi respondido nesta inspeção.");
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