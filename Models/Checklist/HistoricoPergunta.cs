using Google.Cloud.Firestore;

namespace ProjetoISP.Baldan.Models.Checklist
{
    /// <summary>Registro de cada alteração do texto de uma pergunta (auditoria).</summary>
    [FirestoreData]
    public class HistoricoPergunta
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }

        [FirestoreProperty]
        public string TextoAnterior { get; set; }

        [FirestoreProperty]
        public string NovoTexto { get; set; }

        [FirestoreProperty]
        public int VersaoAnterior { get; set; }

        [FirestoreProperty]
        public string? MotivoAlteracao { get; set; }

        [FirestoreProperty]
        public string? AlteradoPor { get; set; }

        [FirestoreProperty]
        public Timestamp DataAlteracao { get; set; }

        [FirestoreProperty]
        public string IdItemChecklist { get; set; }
    }
}
