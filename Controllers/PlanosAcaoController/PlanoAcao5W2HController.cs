using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProjetoISP.Baldan.Models.Acesso;
using ProjetoISP.Baldan.Models.Checklist;
using ProjetoISP.Baldan.Models.Inspecoes;
using ProjetoISP.Baldan.Models.PlanosAcao;

namespace ProjetoISP.Baldan.Controllers
{
    public class PlanoAcao5W2HController : Controller
    {
        // Status do plano. Ajuste os textos se a Baldan usar outros.
        private const string StatusAberto = "Aberto";
        private static readonly string[] StatusDisponiveis = { StatusAberto, "Em andamento", "Concluído", "Cancelado" };

        private readonly FirestoreDb _db;

        public PlanoAcao5W2HController(FirestoreDb db)
        {
            _db = db;
        }

        private CollectionReference Colecao => _db.Collection(Colecoes.PlanosAcao);

        // GET: PlanoAcao5W2H
        public async Task<IActionResult> Index()
        {
            var snapshot = await Colecao.GetSnapshotAsync();
            var planos = snapshot.Documents
                .Select(d => d.ConvertTo<PlanoAcao5W2H>())
                .OrderBy(p => p.Prazo.ToDateTime())
                .ToList();

            ViewBag.NomesResponsaveis = await NomesUsuariosAsync();
            return View(planos);
        }

        // GET: PlanoAcao5W2H/Details/abc123
        public async Task<IActionResult> Details(string id)
        {
            var plano = await ObterAsync(id);
            if (plano == null) return NotFound();

            await CarregarRotulosAsync(plano);

            // Histórico de alterações do prazo (ordenado na memória para não exigir índice composto)
            var historicoSnap = await _db.Collection(Colecoes.HistoricosPrazo)
                .WhereEqualTo(nameof(HistoricoPrazo5W2H.IdPlanoAcao), id)
                .GetSnapshotAsync();

            ViewBag.Historico = historicoSnap.Documents
                .Select(d => d.ConvertTo<HistoricoPrazo5W2H>())
                .OrderByDescending(h => h.DataAlteracao.ToDateTime())
                .ToList();

            return View(plano);
        }

        // GET: PlanoAcao5W2H/Create  (opcional: ?idResultadoInspecao=abc123)
        public async Task<IActionResult> Create(string? idResultadoInspecao)
        {
            await CarregarListasAsync(idResultadoInspecao, null);
            return View(new PlanoAcao5W2H { IdResultadoInspecao = idResultadoInspecao ?? string.Empty });
        }

        // POST: PlanoAcao5W2H/Create
        // O prazo chega como DateTime (campo date do formulário: name="prazo"),
        // porque o MVC não converte texto em Timestamp do Firestore.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("DescricaoDesvio,AcaoCorretiva,IdResultadoInspecao,IdResponsavelUsuario")] PlanoAcao5W2H plano,
            DateTime? prazo)
        {
            plano.Status = StatusAberto;
            IgnorarValidacao(nameof(PlanoAcao5W2H.Status));

            await ValidarReferenciasAsync(plano, validarResultado: true);

            // DER: um resultado de inspeção gera no máximo UM plano de ação
            if (!string.IsNullOrEmpty(plano.IdResultadoInspecao))
            {
                var planosDoResultado = await Colecao
                    .WhereEqualTo(nameof(PlanoAcao5W2H.IdResultadoInspecao), plano.IdResultadoInspecao)
                    .Limit(1)
                    .GetSnapshotAsync();

                if (planosDoResultado.Count > 0)
                    ModelState.AddModelError(nameof(PlanoAcao5W2H.IdResultadoInspecao),
                        "Este resultado já possui um plano de ação.");
            }

            if (prazo == null)
                ModelState.AddModelError("prazo", "Informe o prazo.");
            else if (prazo.Value.Date < DateTime.Today)
                ModelState.AddModelError("prazo", "O prazo não pode ser anterior a hoje.");

            if (!ModelState.IsValid)
            {
                await CarregarListasAsync(plano.IdResultadoInspecao, plano.IdResponsavelUsuario);
                ViewBag.PrazoAtual = prazo?.ToString("yyyy-MM-dd");
                return View(plano);
            }

            plano.Prazo = ParaTimestamp(prazo!.Value);
            await Colecao.AddAsync(plano);
            return RedirectToAction(nameof(Index));
        }

        // GET: PlanoAcao5W2H/Edit/abc123
        public async Task<IActionResult> Edit(string id)
        {
            var plano = await ObterAsync(id);
            if (plano == null) return NotFound();

            await CarregarListasEdicaoAsync(plano);
            ViewBag.PrazoAtual = plano.Prazo.ToDateTime().ToString("yyyy-MM-dd");
            return View(plano);
        }

        // POST: PlanoAcao5W2H/Edit/abc123
        // Se o prazo mudar, o motivo é obrigatório e um registro é gravado em HistoricoPrazo5W2H
        // (junto com a alteração do plano, em uma única operação).
        // O resultado de inspeção do plano NÃO é editável.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id,
            [Bind("Id,DescricaoDesvio,AcaoCorretiva,Status,IdResponsavelUsuario")] PlanoAcao5W2H plano,
            DateTime? prazo,
            string? motivoAlteracao)
        {
            if (id != plano.Id) return NotFound();

            var existente = await ObterAsync(id);
            if (existente == null) return NotFound();

            IgnorarValidacao(nameof(PlanoAcao5W2H.IdResultadoInspecao));

            if (!StatusDisponiveis.Contains(plano.Status))
                ModelState.AddModelError(nameof(PlanoAcao5W2H.Status), "Status inválido.");

            await ValidarReferenciasAsync(plano, validarResultado: false);

            Timestamp novoPrazo = existente.Prazo;
            bool prazoAlterado = false;

            if (prazo == null)
            {
                ModelState.AddModelError("prazo", "Informe o prazo.");
            }
            else
            {
                novoPrazo = ParaTimestamp(prazo.Value);
                prazoAlterado = !existente.Prazo.Equals(novoPrazo);

                if (prazoAlterado && string.IsNullOrWhiteSpace(motivoAlteracao))
                    ModelState.AddModelError("motivoAlteracao", "Informe o motivo da alteração do prazo.");
            }

            if (!ModelState.IsValid)
            {
                plano.IdResultadoInspecao = existente.IdResultadoInspecao;
                plano.Prazo = existente.Prazo;
                await CarregarListasEdicaoAsync(plano);
                ViewBag.PrazoAtual = prazo?.ToString("yyyy-MM-dd") ?? existente.Prazo.ToDateTime().ToString("yyyy-MM-dd");
                return View(plano);
            }

            existente.DescricaoDesvio = plano.DescricaoDesvio;
            existente.AcaoCorretiva = plano.AcaoCorretiva;
            existente.Status = plano.Status;
            existente.IdResponsavelUsuario = plano.IdResponsavelUsuario;

            var prazoAnterior = existente.Prazo;
            existente.Prazo = novoPrazo;

            var batch = _db.StartBatch();
            batch.Set(Colecao.Document(id), existente);

            if (prazoAlterado)
            {
                var historico = new HistoricoPrazo5W2H
                {
                    PrazoAnterior = prazoAnterior,
                    NovoPrazo = novoPrazo,
                    MotivoAlteracao = motivoAlteracao!.Trim(),
                    DataAlteracao = Timestamp.GetCurrentTimestamp(),
                    IdPlanoAcao = id
                };
                batch.Create(_db.Collection(Colecoes.HistoricosPrazo).Document(), historico);
            }

            await batch.CommitAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: PlanoAcao5W2H/Delete/abc123
        public async Task<IActionResult> Delete(string id)
        {
            var plano = await ObterAsync(id);
            if (plano == null) return NotFound();

            await CarregarRotulosAsync(plano);
            return View(plano);
        }

        // POST: PlanoAcao5W2H/Delete/abc123
        // Exclui o plano e o histórico de prazos dele, em uma única operação.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var historicos = await _db.Collection(Colecoes.HistoricosPrazo)
                .WhereEqualTo(nameof(HistoricoPrazo5W2H.IdPlanoAcao), id)
                .GetSnapshotAsync();

            var batch = _db.StartBatch();
            foreach (var doc in historicos.Documents)
                batch.Delete(doc.Reference);
            batch.Delete(Colecao.Document(id));

            await batch.CommitAsync();
            return RedirectToAction(nameof(Index));
        }

        // ---------- Auxiliares ----------

        private async Task<PlanoAcao5W2H?> ObterAsync(string? id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            var snapshot = await Colecao.Document(id).GetSnapshotAsync();
            return snapshot.Exists ? snapshot.ConvertTo<PlanoAcao5W2H>() : null;
        }

        // Data escolhida no formulário -> Timestamp (meia-noite UTC, para a data não "andar" com o fuso)
        private static Timestamp ParaTimestamp(DateTime data) =>
            Timestamp.FromDateTime(DateTime.SpecifyKind(data.Date, DateTimeKind.Utc));

        private async Task<Dictionary<string, string>> NomesUsuariosAsync()
        {
            var snapshot = await _db.Collection(Colecoes.Usuarios).GetSnapshotAsync();
            return snapshot.Documents.ToDictionary(d => d.Id, d => d.ConvertTo<Usuario>().Nome);
        }

        private async Task<Dictionary<string, string>> NomesItensAsync()
        {
            var snapshot = await _db.Collection(Colecoes.ItensChecklist).GetSnapshotAsync();
            return snapshot.Documents.ToDictionary(d => d.Id, d => d.ConvertTo<ItemChecklist>().DescricaoPergunta);
        }

        // id do resultado -> "Pergunta — Resposta"
        private async Task<Dictionary<string, string>> RotulosResultadosAsync()
        {
            var itens = await NomesItensAsync();
            var snapshot = await _db.Collection(Colecoes.ResultadosInspecao).GetSnapshotAsync();

            return snapshot.Documents.ToDictionary(d => d.Id, d =>
            {
                var r = d.ConvertTo<ResultadoInspecao>();
                var pergunta = itens.TryGetValue(r.IdItemChecklist ?? string.Empty, out var texto) ? texto : "?";
                return $"{pergunta} — {r.RespostaColetada}";
            });
        }

        // Textos para as telas de leitura (Details/Delete)
        private async Task CarregarRotulosAsync(PlanoAcao5W2H plano)
        {
            var resultados = await RotulosResultadosAsync();
            ViewBag.RotuloResultado = resultados.TryGetValue(plano.IdResultadoInspecao ?? string.Empty, out var r) ? r : "—";

            var usuarios = await NomesUsuariosAsync();
            ViewBag.NomeResponsavel = usuarios.TryGetValue(plano.IdResponsavelUsuario ?? string.Empty, out var u) ? u : "—";
        }

        // Dropdowns do Create
        private async Task CarregarListasAsync(string? idResultado, string? idResponsavel)
        {
            var resultados = await RotulosResultadosAsync();

            // DER: um resultado gera no máximo UM plano -> só lista resultados que ainda não têm plano
            var planosSnap = await Colecao.GetSnapshotAsync();
            var resultadosComPlano = planosSnap.Documents
                .Select(d => d.ConvertTo<PlanoAcao5W2H>().IdResultadoInspecao)
                .ToHashSet();

            ViewBag.Resultados = new SelectList(
                resultados
                    .Where(r => !resultadosComPlano.Contains(r.Key))
                    .Select(r => new { Id = r.Key, Rotulo = r.Value })
                    .ToList(),
                "Id", "Rotulo", idResultado);

            await CarregarResponsaveisAsync(idResponsavel);
            ViewBag.StatusLista = new SelectList(StatusDisponiveis);
        }

        // Dropdowns do Edit (resultado aparece como texto, não editável)
        private async Task CarregarListasEdicaoAsync(PlanoAcao5W2H plano)
        {
            await CarregarRotulosAsync(plano);
            await CarregarResponsaveisAsync(plano.IdResponsavelUsuario);
            ViewBag.StatusLista = new SelectList(StatusDisponiveis, plano.Status);
        }

        private async Task CarregarResponsaveisAsync(string? selecionado)
        {
            var snapshot = await _db.Collection(Colecoes.Usuarios).GetSnapshotAsync();
            var usuarios = snapshot.Documents
                .Select(d => d.ConvertTo<Usuario>())
                .OrderBy(u => u.Nome)
                .ToList();

            ViewBag.Responsaveis = new SelectList(usuarios, nameof(Usuario.Id), nameof(Usuario.Nome), selecionado);
        }

        private async Task ValidarReferenciasAsync(PlanoAcao5W2H plano, bool validarResultado)
        {
            if (validarResultado && await ReferenciaInvalidaAsync(Colecoes.ResultadosInspecao, plano.IdResultadoInspecao))
                ModelState.AddModelError(nameof(PlanoAcao5W2H.IdResultadoInspecao), "Resultado de inspeção inválido.");

            if (await ReferenciaInvalidaAsync(Colecoes.Usuarios, plano.IdResponsavelUsuario))
                ModelState.AddModelError(nameof(PlanoAcao5W2H.IdResponsavelUsuario), "Responsável inválido.");
        }

        // true só quando o ID foi informado e NÃO existe na coleção.
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