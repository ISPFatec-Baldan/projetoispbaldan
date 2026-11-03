using Google.Cloud.Firestore;

namespace ProjetoISP.Baldan.Models.Acesso
{
    [FirestoreData]
    public class Usuario
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty]
        public string Nome { get; set; }

        [FirestoreProperty]
        public string Cargo { get; set; }

        [FirestoreProperty]
        public string Email { get; set; }

        [FirestoreProperty]
        public string PerfilAcesso { get; set; }

        [FirestoreProperty]
        public string IdSetor { get; set; } // Referência para o ID do Setor
    }
}