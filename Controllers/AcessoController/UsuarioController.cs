using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProjetoISP.Baldan.Models.Acesso;
using ProjetoISP.Baldan.Models.Inspecoes;
using ProjetoISP.Baldan.Models.PlanosAcao;
using ProjetoISP.Baldan.Models.Setores;

namespace ProjetoISP.Baldan.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly FirestoreDb _db;

        public UsuarioController(FirestoreDb db)
        {
            _db = db;
        }

        private CollectionReference Colecao => _db.Collection(Colecoes.Usuarios);

        // GET: Usuario
        public async Task<IActionResult> Index()
        {
            var snapshot = await Colecao.GetSnapshotAsync();
            var usuarios = snapshot.Documents
                .Select(d => d.ConvertTo<Usuario>())
                .OrderBy(u => u.Nome)
                .ToList();

            ViewBag.NomesSetores = await NomesSetoresAsync();
            return View(usuarios);
        }

        // GET: Usuario/Details/abc123
        public async Task<IActionResult> Details(string id)
        {
            var usuario = await ObterAsync(id);
            if (usuario == null) return NotFound();

            ViewBag.NomeSetor = await NomeSetorAsync(usuario.IdSetor);
            return View(usuario);
        }

        // GET: Usuario/Create
        public async Task<IActionResult> Create()
        {
            await CarregarSetoresAsync();
            return View();
        }

        // POST: Usuario/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nome,Cargo,Email,PerfilAcesso,IdSetor")] Usuario usuario)
        {
            await ValidarAsync(usuario, idAtual: null);

            if (!ModelState.IsValid)
            {
                await CarregarSetoresAsync(usuario.IdSetor);
                return View(usuario);
            }

            await Colecao.AddAsync(usuario);
            return RedirectToAction(nameof(Index));
        }

        // GET: Usuario/Edit/abc123
        public async Task<IActionResult> Edit(string id)
        {
            var usuario = await ObterAsync(id);
            if (usuario == null) return NotFound();

            await CarregarSetoresAsync(usuario.IdSetor);
            return View(usuario);
        }

        // POST: Usuario/Edit/abc123
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,Nome,Cargo,Email,PerfilAcesso,IdSetor")] Usuario usuario)
        {
            if (id != usuario.Id) return NotFound();
            if (await ObterAsync(id) == null) return NotFound();

            await ValidarAsync(usuario, idAtual: id);

            if (!ModelState.IsValid)
            {
                await CarregarSetoresAsync(usuario.IdSetor);
                return View(usuario);
            }

            await Colecao.Document(id).SetAsync(usuario);
            return RedirectToAction(nameof(Index));
        }

        // GET: Usuario/Delete/abc123
        public async Task<IActionResult> Delete(string id)
        {
            var usuario = await ObterAsync(id);
            if (usuario == null) return NotFound();

            ViewBag.NomeSetor = await NomeSetorAsync(usuario.IdSetor);
            return View(usuario);
        }

        // POST: Usuario/Delete/abc123
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var inspecoes = await _db.Collection(Colecoes.Inspecoes)
                .WhereEqualTo(nameof(InspecaoISP.IdInspetorUsuario), id)
                .Limit(1)
                .GetSnapshotAsync();

            var planos = await _db.Collection(Colecoes.PlanosAcao)
                .WhereEqualTo(nameof(PlanoAcao5W2H.IdResponsavelUsuario), id)
                .Limit(1)
                .GetSnapshotAsync();

            if (inspecoes.Count > 0 || planos.Count > 0)
            {
                TempData["Erro"] = "Não é possível excluir: este usuário é inspetor de inspeções ou responsável por planos de ação.";
                return RedirectToAction(nameof(Index));
            }

            await Colecao.Document(id).DeleteAsync();
            return RedirectToAction(nameof(Index));
        }

        // ---------- Auxiliares ----------

        private async Task<Usuario?> ObterAsync(string? id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            var snapshot = await Colecao.Document(id).GetSnapshotAsync();
            return snapshot.Exists ? snapshot.ConvertTo<Usuario>() : null;
        }

        private async Task<Dictionary<string, string>> NomesSetoresAsync()
        {
            var snapshot = await _db.Collection(Colecoes.Setores).GetSnapshotAsync();
            return snapshot.Documents.ToDictionary(d => d.Id, d => d.ConvertTo<SetorArea>().Nome);
        }

        private async Task<string> NomeSetorAsync(string? idSetor)
        {
            if (string.IsNullOrEmpty(idSetor)) return "—";

            var snapshot = await _db.Collection(Colecoes.Setores).Document(idSetor).GetSnapshotAsync();
            return snapshot.Exists ? snapshot.ConvertTo<SetorArea>().Nome : "—";
        }

        private async Task CarregarSetoresAsync(string? selecionado = null)
        {
            var snapshot = await _db.Collection(Colecoes.Setores).GetSnapshotAsync();
            var setores = snapshot.Documents
                .Select(d => d.ConvertTo<SetorArea>())
                .OrderBy(s => s.Nome)
                .ToList();

            ViewBag.Setores = new SelectList(setores, nameof(SetorArea.Id), nameof(SetorArea.Nome), selecionado);
        }

        private async Task ValidarAsync(Usuario usuario, string? idAtual)
        {
            // E-mail sempre minúsculo e sem espaços (será a ligação com o login Microsoft)
            usuario.Email = (usuario.Email ?? string.Empty).Trim().ToLowerInvariant();

            if (!string.IsNullOrEmpty(usuario.Email))
            {
                var existentes = await Colecao
                    .WhereEqualTo(nameof(Usuario.Email), usuario.Email)
                    .Limit(2)
                    .GetSnapshotAsync();

                if (existentes.Documents.Any(d => d.Id != idAtual))
                    ModelState.AddModelError(nameof(Usuario.Email), "Já existe um usuário com este e-mail.");
            }

            if (!string.IsNullOrEmpty(usuario.IdSetor))
            {
                var setor = await _db.Collection(Colecoes.Setores).Document(usuario.IdSetor).GetSnapshotAsync();
                if (!setor.Exists)
                    ModelState.AddModelError(nameof(Usuario.IdSetor), "Setor inválido.");
            }
        }
    }
}


