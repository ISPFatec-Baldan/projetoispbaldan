using Google.Cloud.Firestore;
using ProjetoISP.Baldan.Models.Checklist;
using ProjetoISP.Baldan.Models.Inspecoes;
using ProjetoISP.Baldan.Models.Setores;

namespace ProjetoISP.Baldan.Services
{
    /// <summary>Quanto uma pergunta já foi usada e se ela dispara o aviso de "não aplicável" repetido.</summary>
    public record UsoPergunta(int TotalRespostas, bool AvisoNaoAplicavel);

    /// <summary>Uma não conformidade reincidente: mesma pergunta reprovada em inspeções seguidas no mesmo setor.</summary>
    public record Reincidencia(
        string IdSetor, string NomeSetor, string IdItemChecklist, string Pergunta,
        int OcorrenciasSeguidas, DateTime UltimaOcorrencia);

    /// <summary>
    /// Regras de recorrência (única fonte da verdade): aviso de "não aplicável" repetido
    /// e não conformidades reincidentes. O painel e a tela de checklist usam este serviço.
    /// </summary>
    public class RecorrenciaService
    {
        private readonly FirestoreDb _db;

        /// <summary>Quantos checklists seguidos "não aplicável" geram o aviso (padrão 10).</summary>
        public int UltimosChecklists { get; }

        public RecorrenciaService(FirestoreDb db, IConfiguration configuracao)
        {
            _db = db;

            // Opcional, no appsettings.json: "Recorrencia": { "UltimosChecklistsNaoAplicavel": 10 }
            var configurado = configuracao.GetValue<int?>("Recorrencia:UltimosChecklistsNaoAplicavel");
            UltimosChecklists = configurado is > 0 ? configurado.Value : 10;
        }

        // ------------------------------------------------------------------
        // Aviso laranja: pergunta "não aplicável" nos últimos N checklists
        // ------------------------------------------------------------------
        public async Task<Dictionary<string, UsoPergunta>> AnalisarPerguntasAsync(IEnumerable<string> idsItens)
        {
            var ids = idsItens.Distinct().ToList();
            var respostasPorItem = ids.ToDictionary(id => id, _ => new List<ResultadoInspecao>());

            // A consulta "in" aceita poucos valores por vez: busca em blocos de 10 perguntas.
            foreach (var bloco in ids.Chunk(10))
            {
                var snapshot = await _db.Collection(Colecoes.ResultadosInspecao)
                    .WhereIn(nameof(ResultadoInspecao.IdItemChecklist), bloco)
                    .GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    var resultado = doc.ConvertTo<ResultadoInspecao>();
                    if (respostasPorItem.TryGetValue(resultado.IdItemChecklist, out var lista))
                        lista.Add(resultado);
                }
            }

            var uso = new Dictionary<string, UsoPergunta>();
            foreach (var (id, respostas) in respostasPorItem)
            {
                var ultimas = respostas
                    .OrderByDescending(r => r.DataRegistro.ToDateTime())
                    .Take(UltimosChecklists)
                    .ToList();

                bool aviso = ultimas.Count == UltimosChecklists
                             && ultimas.All(r => r.RespostaColetada == RespostasPadrao.NaoAplicavel);

                uso[id] = new UsoPergunta(respostas.Count, aviso);
            }
            return uso;
        }

        // ------------------------------------------------------------------
        // Não conformidades reincidentes
        //
        // REGRA ADOTADA: uma pergunta é reincidente em um setor quando foi respondida
        // "Não conforme" em duas ou mais inspeções SEGUIDAS daquele setor (mesmo checklist).
        // A data considerada é a da última inspeção da sequência.
        // Só inspeções FINALIZADAS entram na conta (em andamento ou canceladas não contam).
        // Para mudar a regra, altere só este método.
        // ------------------------------------------------------------------
        public async Task<List<Reincidencia>> ListarReincidenciasAsync(DateTime? deUtc, DateTime? ateUtc, string? idSetor)
        {
            Query consultaInspecoes = _db.Collection(Colecoes.Inspecoes);
            if (!string.IsNullOrEmpty(idSetor))
                consultaInspecoes = consultaInspecoes.WhereEqualTo(nameof(InspecaoISP.IdSetor), idSetor);

            var inspecoesSnap = await consultaInspecoes.GetSnapshotAsync();
            var inspecoes = inspecoesSnap.Documents
                .Select(d => (Id: d.Id, Dados: d.ConvertTo<InspecaoISP>()))
                .Where(i => i.Dados.Status == StatusInspecao.Finalizada)
                .ToList();

            if (inspecoes.Count == 0) return new List<Reincidencia>();

            var idsInspecoes = inspecoes.Select(i => i.Id).ToHashSet();

            // Só as respostas "Não conforme" interessam: a inspeção anterior é achada pela ordem das datas.
            var naoConformesSnap = await _db.Collection(Colecoes.ResultadosInspecao)
                .WhereEqualTo(nameof(ResultadoInspecao.RespostaColetada), RespostasPadrao.NaoConforme)
                .GetSnapshotAsync();

            var naoConformes = new Dictionary<string, Dictionary<string, ResultadoInspecao>>();
            foreach (var doc in naoConformesSnap.Documents)
            {
                var resultado = doc.ConvertTo<ResultadoInspecao>();
                if (!idsInspecoes.Contains(resultado.IdInspecao)) continue;

                if (!naoConformes.TryGetValue(resultado.IdInspecao, out var porItem))
                    naoConformes[resultado.IdInspecao] = porItem = new Dictionary<string, ResultadoInspecao>();

                porItem[resultado.IdItemChecklist] = resultado;
            }

            var achados = DetectarSequencias(inspecoes, naoConformes);

            // Filtro de período (pela data da última ocorrência)
            var filtrados = achados
                .Where(a => (deUtc == null || a.Value.Data >= deUtc) && (ateUtc == null || a.Value.Data < ateUtc))
                .ToList();
            if (filtrados.Count == 0) return new List<Reincidencia>();

            var setoresSnap = await _db.Collection(Colecoes.Setores).GetSnapshotAsync();
            var nomesSetores = setoresSnap.Documents.ToDictionary(d => d.Id, d => d.ConvertTo<SetorArea>().Nome);

            // Texto da pergunta: o gravado na resposta; se faltar, o texto atual da pergunta
            Dictionary<string, string>? textosAtuais = null;
            if (filtrados.Any(a => string.IsNullOrEmpty(a.Value.Pergunta)))
            {
                var itensSnap = await _db.Collection(Colecoes.ItensChecklist).GetSnapshotAsync();
                textosAtuais = itensSnap.Documents.ToDictionary(d => d.Id, d => d.ConvertTo<ItemChecklist>().DescricaoPergunta);
            }

            return filtrados
                .Select(a =>
                {
                    var pergunta = !string.IsNullOrEmpty(a.Value.Pergunta)
                        ? a.Value.Pergunta
                        : (textosAtuais != null && textosAtuais.TryGetValue(a.Key.Item, out var texto) ? texto : "(pergunta removida)");

                    return new Reincidencia(
                        a.Key.Setor,
                        nomesSetores.TryGetValue(a.Key.Setor, out var nome) ? nome : "(setor removido)",
                        a.Key.Item,
                        pergunta,
                        a.Value.Seguidas,
                        a.Value.Data);
                })
                .OrderByDescending(r => r.OcorrenciasSeguidas)
                .ThenByDescending(r => r.UltimaOcorrencia)
                .ToList();
        }

        /// <summary>
        /// Núcleo da regra de reincidência. Para cada setor + checklist, percorre as inspeções em ordem de data
        /// e conta quantas vezes SEGUIDAS cada pergunta foi "Não conforme". Guarda, por setor + pergunta,
        /// a sequência mais recente com 2 ou mais ocorrências.
        /// </summary>
        public static Dictionary<(string Setor, string Item), (int Seguidas, DateTime Data, string Pergunta)> DetectarSequencias(
            IEnumerable<(string Id, InspecaoISP Dados)> inspecoes,
            IReadOnlyDictionary<string, Dictionary<string, ResultadoInspecao>> naoConformes)
        {
            var achados = new Dictionary<(string Setor, string Item), (int Seguidas, DateTime Data, string Pergunta)>();

            foreach (var grupo in inspecoes.GroupBy(i => (i.Dados.IdSetor, i.Dados.IdModeloChecklist)))
            {
                var corrida = new Dictionary<string, int>();

                foreach (var inspecao in grupo.OrderBy(i => i.Dados.DataHoraInicio.ToDateTime()))
                {
                    var proxima = new Dictionary<string, int>();

                    if (naoConformes.TryGetValue(inspecao.Id, out var atuais))
                    {
                        foreach (var (idItem, resultado) in atuais)
                        {
                            int seguidas = corrida.TryGetValue(idItem, out var anterior) ? anterior + 1 : 1;
                            proxima[idItem] = seguidas;

                            if (seguidas >= 2)
                            {
                                achados[(grupo.Key.IdSetor, idItem)] = (
                                    seguidas,
                                    inspecao.Dados.DataHoraInicio.ToDateTime(),
                                    resultado.DescricaoPerguntaRegistrada ?? string.Empty);
                            }
                        }
                    }

                    corrida = proxima;
                }
            }

            return achados;
        }
    }
}
