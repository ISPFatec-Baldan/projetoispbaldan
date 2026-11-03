using Google.Cloud.Firestore;

namespace ProjetoISP.Baldan.Models
{
    [FirestoreData]
    public class Unidade
    {
        [FirestoreProperty]
        public string Id { get; set; } // No Firestore, IDs costumam ser strings (auto-geradas)

        [FirestoreProperty]
        public string Nome { get; set; }

        [FirestoreProperty]
        public string Cidade { get; set; }

        [FirestoreProperty]
        public string Estado { get; set; }
    }
}