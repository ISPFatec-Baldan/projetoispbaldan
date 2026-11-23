using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using ProjetoISP.Baldan.Models.Checklist;
using ProjetoISP.Baldan.Models.Inspecoes;
using ProjetoISP.Baldan.Services;

namespace ProjetoISP.Baldan.Controllers
{
    /// <summary>
    /// Tela "Perguntas": banco de perguntas do ISP. Cada pergunta tem um tópico e pode estar em vários
    /// checklists. Incluir ou retirar uma pergunta de um checklist passa a valer na próxima semana.
    /// </summary>
    public class PerguntasController : Controller
    {
        private const string TipoRespostaPadrao = "Conformidade";
        private static readonly string[] Ordens = { "recentes", "antigas", "alfabetica" };
        private static readonly string[] Situacoes = { "ativas", "aviso", "semchecklist", "desativadas" };

        private readonly FirestoreDb _db;
        private readonly RecorrenciaService _recorrencia;
        private readonly VigenciaService _vigencia;

        public PerguntasController(FirestoreDb db, RecorrenciaService recorrencia, VigenciaService vigencia)
        {
            _db = db;
            _recorrencia = recorrencia;
            _vigencia = vigencia;
        }

        private CollectionReference Colecao => _db.Collection(Colecoes.ItensChecklist);
        private CollectionReference Vinculos => _db.Collection(Colecoes.ChecklistPerguntas);

        // GET: Perguntas?ordem=recentes&situacao=ativas&idChecklist=abc&idTopico=xyz
        public async Task<IActionResult> Index(string? ordem, string? situacao, string? idChecklist, string? idTopico)
        {
            ordem = ordem != null && Ordens.Contains(ordem) ? ordem : "recentes";
            situacao = situacao != null && Situacoes.Contains(situacao) ? situacao : "ativas";
            var agora = DateTime.UtcNow;

            var itens = (await Colecao.GetSnapshotAsync()).Documents
                .Select(d => d.ConvertTo<ItemChecklist>())
                .ToList();

            var topicos = await CarregarTopicosAsync();
            var nomesTopicos = topicos.ToDictionary(t => t.Id!, t => t.Nome);

            // Nomes de todos os checklists (para mostrar os vínculos) e só os ativos (para escolher)
            var modelosSnap = await _db.Collection(Colecoes.ModelosChecklist).GetSnapshotAsync();
            var modelos = modelosSnap.Documents.Select(d => (Id: d.Id, Dados: d.ConvertTo<ModeloChecklist>())).ToList();
            var nomesChecklists = modelos.ToDictionary(m => m.Id, m => m.Dados.Nome);
            var checklistsAtivos = modelos
                .Where(m => m.Dados.Status == "Ativo")
                .OrderBy(m => m.Dados.Nome)
                .Select(m => new ChecklistOpcao(m.Id, m.Dados.Nome))
                .ToList();

            // Vínculos ainda não encerrados (vigentes, saindo ou entrando), por pergunta
            var vinculosPorItem = (await Vinculos.GetSnapshotAsync()).Documents
                .Select(d => d.ConvertTo<ChecklistPergunta>())
                .Where(v => v.VigenteAte == null || v.VigenteAte.Value.ToDateTime() > agora)
                .Where(v => nomesChecklists.ContainsKey(v.IdModeloChecklist))
                .GroupBy(v => v.IdItemChecklist)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(v => ParaVinculo(v, nomesChecklists[v.IdModeloChecklist], agora))
                          .OrderBy(v => v.NomeChecklist)
                          .ToList());

            var uso = await _recorrencia.AnalisarPerguntasAsync(itens.Select(i => i.Id!));

            var linhas = itens.Select(i =>
            {
                var vinculos = vinculosPorItem.TryGetValue(i.Id!, out var lista) ? lista : new List<VinculoPergunta>();
                var u = uso[i.Id!];
                bool emUso = u.TotalRespostas > 0 || vinculos.Any(v => v.Situacao != "entrando");
                var topico = nomesTopicos.TryGetValue(i.IdTopico ?? "", out var nome) ? nome : "Sem tópico";
                return new PerguntaItem(i, topico, vinculos, u.TotalRespostas, u.AvisoNaoAplicavel, emUso);
            }).ToList();

            IEnumerable<PerguntaItem> filtradas = linhas;

            if (!string.IsNullOrEmpty(idChecklist))
                filtradas = filtradas.Where(l => l.Vinculos.Any(v => v.IdChecklist == idChecklist));
            if (!string.IsNullOrEmpty(idTopico))
                filtradas = filtradas.Where(l => l.Item.IdTopico == idTopico);

            filtradas = situacao switch
            {
                "desativadas" => filtradas.Where(l => !l.Item.Ativo),
                "aviso" => filtradas.Where(l => l.Item.Ativo && l.AvisoNaoAplicavel),
                "semchecklist" => filtradas.Where(l => l.Item.Ativo && l.Vinculos.Count == 0),
                _ => filtradas.Where(l => l.Item.Ativo)
            };

            filtradas = ordem switch
            {
                "antigas" => filtradas.OrderBy(l => l.Item.DataCriacao.ToDateTime()),
                "alfabetica" => filtradas.OrderBy(l => l.Item.DescricaoPergunta),
                _ => filtradas.OrderByDescending(l => l.Item.DataCriacao.ToDateTime())
            };

            return View(new PerguntasViewModel
            {
                Perguntas = filtradas.ToList(),
                Topicos = topicos,
                Checklists = checklistsAtivos,
                Ordem = ordem,
                Situacao = situacao,
                IdChecklist = idChecklist,
                IdTopico = idTopico,
                TotalAtivas = itens.Count(i => i.Ativo),
                TotalDesativadas = itens.Count(i => !i.Ativo),
                UltimosChecklists = _recorrencia.UltimosChecklists,
                TextoVigencia = _vigencia.TextoVigencia()
            });
        }

        // POST: Perguntas/Cadastrar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cadastrar(string? descricao, string? idTopico, string[]? idsChecklists)
        {
            descricao = descricao?.Trim();
            if (string.IsNullOrEmpty(descricao))
                return Voltar(erro: "Escreva a pergunta antes de cadastrar.");

            if (string.IsNullOrEmpty(idTopico) || !await TopicoExisteAsync(idTopico))
                return Voltar(erro: "Escolha o tópico da pergunta (ou crie um novo com o botão +).");

            var checklists = await FiltrarChecklistsAtivosAsync(idsChecklists);
            var vigencia = Timestamp.FromDateTime(_vigencia.InicioDeVigenciaUtc());

            var item = new ItemChecklist
            {
                DescricaoPergunta = descricao,
                TipoResposta = TipoRespostaPadrao,
                DataCriacao = Timestamp.GetCurrentTimestamp(),
                IdTopico = idTopico,
                Ativo = true,
                Versao = 1
            };

            var batch = _db.StartBatch();
            var documento = Colecao.Document();
            batch.Create(documento, item);
            foreach (var idChecklist in checklists)
            {
                batch.Create(Vinculos.Document(), new ChecklistPergunta
                {
                    IdModeloChecklist = idChecklist,
                    IdItemChecklist = documento.Id,
                    VigenteDesde = vigencia,
                    VigenteAte = null
                });
            }
            await batch.CommitAsync();

            TempData["Sucesso"] = checklists.Count == 0
                ? "Pergunta cadastrada no banco. Vincule-a a um checklist quando quiser."
                : $"Pergunta cadastrada e vinculada a {checklists.Count} checklist(s). Passa a valer {_vigencia.TextoVigencia()}.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Perguntas/Alterar
        // Mudar o TEXTO sobe a versão da pergunta e grava o histórico (as respostas já dadas mantêm o texto
        // original). Mudar os CHECKLISTS vale na próxima semana.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Alterar(
            string id, string? novoTexto, string? motivo, string? idTopico, string[]? idsChecklists)
        {
            var item = await ObterAsync(id);
            if (item == null) return NotFound();

            novoTexto = novoTexto?.Trim();
            if (string.IsNullOrEmpty(novoTexto))
                return Voltar(erro: "O texto da pergunta não pode ficar vazio.");

            if (string.IsNullOrEmpty(idTopico) || !await TopicoExisteAsync(idTopico))
                return Voltar(erro: "Escolha o tópico da pergunta.");

            var checklists = await FiltrarChecklistsAtivosAsync(idsChecklists);
            var agora = DateTime.UtcNow;
            var vigencia = _vigencia.InicioDeVigenciaUtc();

            // Vínculos com checklists INATIVOS não aparecem no formulário: continuam como estão.
            var ativos = await IdsChecklistsAtivosAsync();
            var vinculosAtuais = await Vinculos
                .WhereEqualTo(nameof(ChecklistPergunta.IdItemChecklist), id)
                .GetSnapshotAsync();
            foreach (var doc in vinculosAtuais.Documents)
            {
                var atual = doc.ConvertTo<ChecklistPergunta>();
                bool aberto = atual.VigenteAte == null || atual.VigenteAte.Value.ToDateTime() > agora;
                if (aberto && !ativos.Contains(atual.IdModeloChecklist))
                    checklists.Add(atual.IdModeloChecklist);
            }

            var batch = _db.StartBatch();

            if (novoTexto != item.DescricaoPergunta)
            {
                batch.Create(_db.Collection(Colecoes.HistoricosPergunta).Document(), new HistoricoPergunta
                {
                    TextoAnterior = item.DescricaoPergunta,
                    NovoTexto = novoTexto,
                    VersaoAnterior = item.Versao,
                    MotivoAlteracao = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim(),
                    AlteradoPor = User.Identity?.Name,
                    DataAlteracao = Timestamp.GetCurrentTimestamp(),
                    IdItemChecklist = id
                });

                item.DescricaoPergunta = novoTexto;
                item.Versao += 1;
            }

            item.IdTopico = idTopico;
            batch.Set(Colecao.Document(id), item);

            await SincronizarVinculosAsync(batch, id, checklists, agora, vigencia);
            await batch.CommitAsync();

            TempData["Sucesso"] = $"Pergunta atualizada. Mudanças nos checklists passam a valer {_vigencia.TextoVigencia()}.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Perguntas/Remover
        // Pergunta que já foi respondida (ou já está em algum checklist) é DESATIVADA: sai dos checklists na
        // próxima semana e continua no histórico. Pergunta que nunca valeu em nenhum checklist é excluída de vez.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remover(string id)
        {
            var item = await ObterAsync(id);
            if (item == null) return NotFound();

            var agora = DateTime.UtcNow;
            var vinculos = (await Vinculos
                    .WhereEqualTo(nameof(ChecklistPergunta.IdItemChecklist), id)
                    .GetSnapshotAsync()).Documents;

            bool jaComecou = vinculos
                .Select(d => d.ConvertTo<ChecklistPergunta>())
                .Any(v => v.VigenteDesde.ToDateTime() <= agora);

            var respostas = await _db.Collection(Colecoes.ResultadosInspecao)
                .WhereEqualTo(nameof(ResultadoInspecao.IdItemChecklist), id)
                .Limit(1)
                .GetSnapshotAsync();

            var batch = _db.StartBatch();

            if (respostas.Count > 0 || jaComecou)
            {
                item.Ativo = false;
                batch.Set(Colecao.Document(id), item);
                await SincronizarVinculosAsync(batch, id, new HashSet<string>(), agora, _vigencia.InicioDeVigenciaUtc());
                await batch.CommitAsync();

                TempData["Sucesso"] = $"Pergunta desativada. Ela sai dos checklists {_vigencia.TextoVigencia()} e continua no histórico.";
            }
            else
            {
                foreach (var doc in vinculos) batch.Delete(doc.Reference);

                var historicos = await _db.Collection(Colecoes.HistoricosPergunta)
                    .WhereEqualTo(nameof(HistoricoPergunta.IdItemChecklist), id)
                    .GetSnapshotAsync();
                foreach (var doc in historicos.Documents) batch.Delete(doc.Reference);

                batch.Delete(Colecao.Document(id));
                await batch.CommitAsync();

                TempData["Sucesso"] = "Pergunta excluída.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Perguntas/Reativar  (também cancela as saídas que estavam agendadas)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reativar(string id)
        {
            var item = await ObterAsync(id);
            if (item == null) return NotFound();

            var agora = DateTime.UtcNow;
            item.Ativo = true;

            var batch = _db.StartBatch();
            batch.Set(Colecao.Document(id), item);

            var vinculos = await Vinculos
                .WhereEqualTo(nameof(ChecklistPergunta.IdItemChecklist), id)
                .GetSnapshotAsync();
            foreach (var doc in vinculos.Documents)
            {
                var vinculo = doc.ConvertTo<ChecklistPergunta>();
                if (vinculo.VigenteAte != null && vinculo.VigenteAte.Value.ToDateTime() > agora)
                {
                    vinculo.VigenteAte = null;
                    batch.Set(doc.Reference, vinculo);
                }
            }
            await batch.CommitAsync();

            TempData["Sucesso"] = "Pergunta reativada.";
            return RedirectToAction(nameof(Index), new { situacao = "desativadas" });
        }

        // POST: Perguntas/CriarTopico   (botão "+" da janela; devolve o tópico em JSON)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CriarTopico(string? nome)
        {
            nome = nome?.Trim();
            if (string.IsNullOrEmpty(nome)) return BadRequest("Digite o nome do tópico.");
            if (nome.Length > 60) return BadRequest("O nome do tópico pode ter no máximo 60 caracteres.");

            var existentes = await _db.Collection(Colecoes.Topicos).GetSnapshotAsync();
            var igual = existentes.Documents.FirstOrDefault(d =>
                string.Equals(d.ConvertTo<Topico>().Nome, nome, StringComparison.OrdinalIgnoreCase));

            if (igual != null)
                return Json(new { id = igual.Id, nome = igual.ConvertTo<Topico>().Nome, existente = true });

            var documento = await _db.Collection(Colecoes.Topicos).AddAsync(new Topico { Nome = nome });
            return Json(new { id = documento.Id, nome, existente = false });
        }

        // ---------- Auxiliares ----------

        private IActionResult Voltar(string erro)
        {
            TempData["Erro"] = erro;
            return RedirectToAction(nameof(Index));
        }

        private async Task<ItemChecklist?> ObterAsync(string? id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            var snapshot = await Colecao.Document(id).GetSnapshotAsync();
            return snapshot.Exists ? snapshot.ConvertTo<ItemChecklist>() : null;
        }

        private static VinculoPergunta ParaVinculo(ChecklistPergunta v, string nome, DateTime agora)
        {
            var desde = v.VigenteDesde.ToDateTime();
            DateTime? ate = v.VigenteAte?.ToDateTime();
            var situacao = desde > agora ? "entrando" : ate != null ? "saindo" : "vigente";
            return new VinculoPergunta(v.IdModeloChecklist, nome, situacao, desde.ToLocalTime(), ate?.ToLocalTime());
        }

        private async Task<List<Topico>> CarregarTopicosAsync()
        {
            var snapshot = await _db.Collection(Colecoes.Topicos).GetSnapshotAsync();
            return snapshot.Documents
                .Select(d => d.ConvertTo<Topico>())
                .OrderBy(t => t.Nome)
                .ToList();
        }

        private async Task<bool> TopicoExisteAsync(string idTopico)
        {
            var snapshot = await _db.Collection(Colecoes.Topicos).Document(idTopico).GetSnapshotAsync();
            return snapshot.Exists;
        }

        private async Task<HashSet<string>> IdsChecklistsAtivosAsync()
        {
            var snapshot = await _db.Collection(Colecoes.ModelosChecklist).GetSnapshotAsync();
            return snapshot.Documents
                .Where(d => d.ConvertTo<ModeloChecklist>().Status == "Ativo")
                .Select(d => d.Id)
                .ToHashSet();
        }

        // Só aceita checklists que existem e estão ativos
        private async Task<HashSet<string>> FiltrarChecklistsAtivosAsync(string[]? ids)
        {
            if (ids == null || ids.Length == 0) return new HashSet<string>();

            var ativos = await IdsChecklistsAtivosAsync();
            return ids.Where(ativos.Contains).ToHashSet();
        }

        // Deixa os vínculos da pergunta iguais à seleção, respeitando a vigência semanal:
        //  - marcado e novo           -> cria o vínculo (vale a partir da vigência);
        //  - marcado e já existente   -> mantém (e cancela uma saída que estava agendada);
        //  - desmarcado e já valendo  -> agenda a saída (vigente até a vigência);
        //  - desmarcado e ainda nem começou -> apaga.
        private async Task SincronizarVinculosAsync(
            WriteBatch batch, string idItem, HashSet<string> desejados, DateTime agoraUtc, DateTime vigenciaUtc)
        {
            var snapshot = await Vinculos
                .WhereEqualTo(nameof(ChecklistPergunta.IdItemChecklist), idItem)
                .GetSnapshotAsync();

            var abertos = new Dictionary<string, (DocumentReference Ref, ChecklistPergunta Dados)>();
            foreach (var doc in snapshot.Documents)
            {
                var vinculo = doc.ConvertTo<ChecklistPergunta>();
                bool encerrado = vinculo.VigenteAte != null && vinculo.VigenteAte.Value.ToDateTime() <= agoraUtc;
                if (!encerrado) abertos[vinculo.IdModeloChecklist] = (doc.Reference, vinculo);
            }

            foreach (var (idChecklist, aberto) in abertos)
            {
                if (desejados.Contains(idChecklist))
                {
                    if (aberto.Dados.VigenteAte != null)
                    {
                        aberto.Dados.VigenteAte = null;
                        batch.Set(aberto.Ref, aberto.Dados);
                    }
                }
                else if (aberto.Dados.VigenteDesde.ToDateTime() > agoraUtc)
                {
                    batch.Delete(aberto.Ref);
                }
                else if (aberto.Dados.VigenteAte == null)
                {
                    aberto.Dados.VigenteAte = Timestamp.FromDateTime(vigenciaUtc);
                    batch.Set(aberto.Ref, aberto.Dados);
                }
            }

            foreach (var idChecklist in desejados.Where(c => !abertos.ContainsKey(c)))
            {
                batch.Create(Vinculos.Document(), new ChecklistPergunta
                {
                    IdModeloChecklist = idChecklist,
                    IdItemChecklist = idItem,
                    VigenteDesde = Timestamp.FromDateTime(vigenciaUtc),
                    VigenteAte = null
                });
            }
        }
    }
}
