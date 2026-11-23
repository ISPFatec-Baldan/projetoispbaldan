using Google.Cloud.Firestore;

namespace ProjetoISP.Baldan.Models.Checklist
{
    /// <summary>Vínculo muitos-para-muitos: em quais setores um modelo de checklist é aplicado.</summary>
    [FirestoreData]
    public class ModeloChecklistSetor
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }

        [FirestoreProperty]
        public string IdModeloChecklist { get; set; }

        [FirestoreProperty]
        public string IdSetor { get; set; }
    }
}
