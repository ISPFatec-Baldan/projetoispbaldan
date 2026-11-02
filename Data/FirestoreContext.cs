using Google.Cloud.Firestore;

namespace ProjetoISP.Baldan.Data
{
    public class FirestoreContext
    {
        public FirestoreDb Db { get; }

        public FirestoreContext(FirestoreDb db)
        {
            Db = db;
        }

        // Nomes das coleções (equivalem às tabelas)
        public static class Colecoes
        {
            // Acesso
            public const string PerfisAcesso = "perfis_acesso";
            public const string Usuarios = "usuarios";

            // Setores
            public const string SetoresAreas = "setores_areas";

            // Checklist
            public const string CategoriasPerguntas = "categorias_perguntas";
            public const string PerguntasChecklist = "perguntas_checklist";
            public const string SetoresPerguntas = "setores_perguntas";

            // Inspeções
            public const string InspecoesISP = "inspecoes_isp";
            public const string ParticipantesInspecao = "participantes_inspecao";
            public const string RespostasChecklist = "respostas_checklist";

            // Não conformidades e planos de ação
            public const string NaoConformidades = "nao_conformidades";
            public const string PlanosAcao5W2H = "planos_acao_5w2h";
            public const string EvidenciasFotograficas = "evidencias_fotograficas";
            public const string HistoricosPrazos5W2H = "historicos_prazos_5w2h";
        }

        // Equivalentes aos DbSet<T>
        public CollectionReference PerfisAcesso => Db.Collection(Colecoes.PerfisAcesso);
        public CollectionReference Usuarios => Db.Collection(Colecoes.Usuarios);
        public CollectionReference SetoresAreas => Db.Collection(Colecoes.SetoresAreas);
        public CollectionReference CategoriasPerguntas => Db.Collection(Colecoes.CategoriasPerguntas);
        public CollectionReference PerguntasChecklist => Db.Collection(Colecoes.PerguntasChecklist);
        public CollectionReference SetoresPerguntas => Db.Collection(Colecoes.SetoresPerguntas);
        public CollectionReference InspecoesISP => Db.Collection(Colecoes.InspecoesISP);
        public CollectionReference ParticipantesInspecao => Db.Collection(Colecoes.ParticipantesInspecao);
        public CollectionReference RespostasChecklist => Db.Collection(Colecoes.RespostasChecklist);
        public CollectionReference NaoConformidades => Db.Collection(Colecoes.NaoConformidades);
        public CollectionReference PlanosAcao5W2H => Db.Collection(Colecoes.PlanosAcao5W2H);
        public CollectionReference EvidenciasFotograficas => Db.Collection(Colecoes.EvidenciasFotograficas);
        public CollectionReference HistoricosPrazos5W2H => Db.Collection(Colecoes.HistoricosPrazos5W2H);

        // Substitui a chave composta { SetorAreaid, PerguntaChecklistid }:
        // no Firestore, o ID do documento é composto manualmente.
        public static string IdSetorPergunta(string setorAreaId, string perguntaChecklistId)
            => $"{setorAreaId}_{perguntaChecklistId}";

        // ---- Helpers genéricos (CRUD básico) ----

        // Cria um documento com ID gerado automaticamente e devolve esse ID
        public async Task<string> AdicionarAsync<T>(CollectionReference colecao, T entidade) where T : class
        {
            var doc = await colecao.AddAsync(entidade);
            return doc.Id;
        }

        // Cria ou sobrescreve o documento inteiro com o ID informado
        public async Task SalvarAsync<T>(CollectionReference colecao, string id, T entidade) where T : class
        {
            await colecao.Document(id).SetAsync(entidade);
        }

        // Atualiza apenas os campos informados, sem apagar o restante do documento
        public async Task AtualizarCamposAsync(CollectionReference colecao, string id, Dictionary<string, object> campos)
        {
            await colecao.Document(id).UpdateAsync(campos);
        }

        public async Task<T?> ObterAsync<T>(CollectionReference colecao, string id) where T : class
        {
            var snap = await colecao.Document(id).GetSnapshotAsync();
            return snap.Exists ? snap.ConvertTo<T>() : null;
        }

        // Atenção: lê a coleção inteira (cobrança por documento lido).
        // Em coleções grandes, prefira ListarPorCampoAsync ou consultas com Limit.
        public async Task<List<T>> ListarAsync<T>(CollectionReference colecao) where T : class
        {
            var snap = await colecao.GetSnapshotAsync();
            return snap.Documents.Select(d => d.ConvertTo<T>()).ToList();
        }

        public async Task<List<T>> ListarPorCampoAsync<T>(CollectionReference colecao, string campo, object valor) where T : class
        {
            var snap = await colecao.WhereEqualTo(campo, valor).GetSnapshotAsync();
            return snap.Documents.Select(d => d.ConvertTo<T>()).ToList();
        }

        public async Task RemoverAsync(CollectionReference colecao, string id)
        {
            await colecao.Document(id).DeleteAsync();
        }

        // Substitui o SaveChanges quando várias gravações precisam ser atômicas
        public Task<T> ExecutarTransacaoAsync<T>(Func<Transaction, Task<T>> operacao)
            => Db.RunTransactionAsync(operacao);
    }
}