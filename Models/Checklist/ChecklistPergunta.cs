using Google.Cloud.Firestore;

namespace ProjetoISP.Baldan.Models.Checklist
{
    /// <summary>
    /// Vínculo entre uma pergunta e um checklist, com vigência semanal:
    /// a pergunta vale no checklist de VigenteDesde (inclusive) até VigenteAte (exclusive).
    /// VigenteAte vazio = sem data para sair.
    /// </summary>
    [FirestoreData]
    public class ChecklistPergunta
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }

        [FirestoreProperty]
        public string IdModeloChecklist { get; set; }

        [FirestoreProperty]
        public string IdItemChecklist { get; set; }

        [FirestoreProperty]
        public Timestamp VigenteDesde { get; set; }

        [FirestoreProperty]
        public Timestamp? VigenteAte { get; set; }
    }
}
