using Google.Cloud.Firestore;

namespace ProjetoISP.Baldan.Models.Checklist
{
    [FirestoreData]
    public class Categoria
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }

        [FirestoreProperty]
        public string Nome { get; set; }
    }
}