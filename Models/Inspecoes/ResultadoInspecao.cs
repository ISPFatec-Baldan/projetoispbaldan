using Google.Cloud.Firestore;

namespace ProjetoISP.Baldan.Models.Inspecoes
{
    [FirestoreData]
    public class ResultadoInspecao
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }

        // Valores aceitos: veja RespostasPadrao (Conforme, Não conforme, Não aplicável)
        [FirestoreProperty]
        public string RespostaColetada { get; set; }

        [FirestoreProperty]
        public string? Observacoes { get; set; }

        [FirestoreProperty]
        public string? UrlFotoEvidencia { get; set; }

        // A resposta fica presa ao texto e à versão da pergunta NO MOMENTO em que foi dada.
        // Editar a pergunta depois não altera o que já foi respondido.
        [FirestoreProperty]
        public string? DescricaoPerguntaRegistrada { get; set; }

        [FirestoreProperty]
        public int VersaoPergunta { get; set; }

        [FirestoreProperty]
        public Timestamp DataRegistro { get; set; }

        [FirestoreProperty]
        public string IdInspecao { get; set; }

        [FirestoreProperty]
        public string IdItemChecklist { get; set; }
    }
}
