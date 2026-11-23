using Google.Cloud.Firestore;
using ProjetoISP.Baldan.Models;
using ProjetoISP.Baldan.Models.Checklist;
using ProjetoISP.Baldan.Models.Inspecoes;
using ProjetoISP.Baldan.Models.Setores;

namespace ProjetoISP.Baldan.Services
{
    /// <summary>Calcula, a partir do Firestore, os números do painel (tela inicial).</summary>
    public class PainelService
    {
        private readonly FirestoreDb _db;
        private readonly RecorrenciaService _recorrencia;

        public PainelService(FirestoreDb db, RecorrenciaService recorrencia)
        {
            _db = db;
            _recorrencia = recorrencia;
        }

        /// <summary>
        /// Preenche com dados reais: checklists por setor, lista de setores do filtro, taxa de conformidade,
        /// inspeções do mês, não conformidades reincidentes e os avisos.
        /// (Planos pendentes de análise e o gráfico de status continuam de exemplo.)
        /// </summary>
        public async Task PreencherAsync(DashboardViewModel vm)
        {
            var agora = DateTime.Now;
            var inicioSemanaLocal = agora.Date.AddDays(-(((int)agora.DayOfWeek + 6) % 7));   // segunda-feira
            var inicioMesLocal = new DateTime(agora.Year, agora.Month, 1);

            var inicioSemana = ParaUtc(inicioSemanaLocal);
            var inicioSemanaAnterior = ParaUtc(inicioSemanaLocal.AddDays(-7));
            var inicioMes = ParaUtc(inicioMesLocal);
            var inicioMesAnterior = ParaUtc(inicioMesLocal.AddMonths(-1));

            // ---------- setores e vínculos com checklists ----------
            var setoresSnap = await _db.Collection(Colecoes.Setores).GetSnapshotAsync();
            var setores = setoresSnap.Documents.ToDictionary(d => d.Id, d => d.ConvertTo<SetorArea>().Nome);
            vm.SetoresFiltro = setores
                .OrderBy(s => s.Value)
                .Select(s => new SetorOpcao(s.Key, s.Value))
                .ToList();

            // Só entram na grade os setores com ao menos um checklist ATIVO vinculado
            var modelosSnap = await _db.Collection(Colecoes.ModelosChecklist).GetSnapshotAsync();
            var modelosAtivos = modelosSnap.Documents
                .Where(d => d.ConvertTo<ModeloChecklist>().Status == "Ativo")
                .Select(d => d.Id)
                .ToHashSet();

            var vinculosSnap = await _db.Collection(Colecoes.ModelosSetores).GetSnapshotAsync();
            var setoresEsperados = vinculosSnap.Documents
                .Select(d => d.ConvertTo<ModeloChecklistSetor>())
                .Where(v => modelosAtivos.Contains(v.IdModeloChecklist) && setores.ContainsKey(v.IdSetor))
                .Select(v => v.IdSetor)
                .ToHashSet();

            // ---------- inspeções (desde o início do mês anterior) ----------
            var inspecoesSnap = await _db.Collection(Colecoes.Inspecoes)
                .WhereGreaterThanOrEqualTo(nameof(InspecaoISP.DataHoraInicio), Timestamp.FromDateTime(inicioMesAnterior))
                .GetSnapshotAsync();

            var inspecoes = inspecoesSnap.Documents
                .Select(d => d.ConvertTo<InspecaoISP>())
                .Where(i => i.Status != StatusInspecao.Cancelada)
                .ToList();

            DateTime Inicio(InspecaoISP i) => i.DataHoraInicio.ToDateTime();

            // REGRA: setor "Em dia" = teve ao menos uma inspeção FINALIZADA na semana atual
            var setoresEmDia = inspecoes
                .Where(i => Inicio(i) >= inicioSemana && i.Status == StatusInspecao.Finalizada)
                .Select(i => i.IdSetor)
                .ToHashSet();

            vm.SetoresSemana = setoresEsperados
                .OrderBy(id => setores[id])
                .Select(id => new SetorStatusItem(setores[id], setoresEmDia.Contains(id)))
                .ToList();
            vm.ChecklistsRealizadosSemana = vm.SetoresSemana.Count(s => s.Ok);

            int inspecoesMes = inspecoes.Count(i => Inicio(i) >= inicioMes);
            int inspecoesMesAnterior = inspecoes.Count(i => Inicio(i) >= inicioMesAnterior && Inicio(i) < inicioMes);
            vm.TotalInspecoesMes = inspecoesMes;
            vm.TendenciaInspecoes = new Tendencia(inspecoesMes - inspecoesMesAnterior, "", true, "mês anterior");

            // ---------- taxa de conformidade ----------
            // REGRA: Conforme ÷ (Conforme + Não conforme). "Não aplicável" fica fora do cálculo.
            var respostasSnap = await _db.Collection(Colecoes.ResultadosInspecao)
                .WhereGreaterThanOrEqualTo(nameof(ResultadoInspecao.DataRegistro), Timestamp.FromDateTime(inicioSemanaAnterior))
                .GetSnapshotAsync();
            var respostas = respostasSnap.Documents.Select(d => d.ConvertTo<ResultadoInspecao>()).ToList();

            decimal? Taxa(IEnumerable<ResultadoInspecao> lista)
            {
                var itens = lista.ToList();
                int conformes = itens.Count(r => r.RespostaColetada == RespostasPadrao.Conforme);
                int naoConformes = itens.Count(r => r.RespostaColetada == RespostasPadrao.NaoConforme);
                int total = conformes + naoConformes;
                return total == 0 ? null : Math.Round(conformes * 100m / total, 1);
            }

            var taxaSemana = Taxa(respostas.Where(r => r.DataRegistro.ToDateTime() >= inicioSemana));
            var taxaAnterior = Taxa(respostas.Where(r =>
            {
                var data = r.DataRegistro.ToDateTime();
                return data >= inicioSemanaAnterior && data < inicioSemana;
            }));

            vm.TaxaConformidadeSemana = taxaSemana;
            vm.TendenciaConformidade = taxaSemana is { } atual && taxaAnterior is { } anterior
                ? new Tendencia(Math.Round(atual - anterior, 1), "pts", true, "semana anterior")
                : null;

            // ---------- reincidências do mês ----------
            var reincidencias = await _recorrencia.ListarReincidenciasAsync(inicioMes, null, null);
            vm.NaoConformidadesReincidentes = reincidencias.Count;
            vm.TendenciaReincidentes = null;

            // ---------- perguntas "não aplicáveis" repetidas ----------
            var itensSnap = await _db.Collection(Colecoes.ItensChecklist).GetSnapshotAsync();
            var idsAtivos = itensSnap.Documents
                .Where(d => d.ConvertTo<ItemChecklist>().Ativo)
                .Select(d => d.Id)
                .ToList();
            var uso = await _recorrencia.AnalisarPerguntasAsync(idsAtivos);
            int perguntasNaoAplicaveis = uso.Values.Count(u => u.AvisoNaoAplicavel);

            // ---------- avisos (Atenção necessária) ----------
            var alertas = new List<AlertaItem>();

            if (reincidencias.Count > 0)
            {
                alertas.Add(new AlertaItem(
                    $"{reincidencias.Count} {Plural(reincidencias.Count, "não conformidade reincidente", "não conformidades reincidentes")} no mês",
                    "A mesma pergunta reprovou em inspeções seguidas no mesmo setor.",
                    "perigo", "ResultadoInspecao"));
            }

            var pendentes = vm.SetoresSemana.Where(s => !s.Ok).Select(s => s.Nome).ToList();
            if (pendentes.Count > 0)
            {
                var detalhe = pendentes.Count <= 4
                    ? string.Join(", ", pendentes)
                    : $"{string.Join(", ", pendentes.Take(4))} e mais {pendentes.Count - 4}";

                alertas.Add(new AlertaItem(
                    $"{pendentes.Count} {Plural(pendentes.Count, "setor sem checklist finalizado", "setores sem checklist finalizado")} nesta semana",
                    detalhe, "alerta", "InspecoesISP"));
            }

            if (perguntasNaoAplicaveis > 0)
            {
                alertas.Add(new AlertaItem(
                    $"{perguntasNaoAplicaveis} {Plural(perguntasNaoAplicaveis, "pergunta", "perguntas")} não aplicável(is) nos últimos {_recorrencia.UltimosChecklists} checklists",
                    "Considere revisar ou desativar.", "alerta", "ModeloChecklist"));
            }

            vm.Alertas = alertas;
        }

        private static DateTime ParaUtc(DateTime local) =>
            DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime();

        private static string Plural(int quantidade, string singular, string plural) =>
            quantidade == 1 ? singular : plural;
    }
}
