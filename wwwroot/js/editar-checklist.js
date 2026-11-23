// Tela "Editar Checklist": busca instantânea e preenchimento das janelas (editar / remover).
(function () {
    // ----- Busca instantânea (filtra a lista que já está na página) -----
    const busca = document.getElementById('busca');
    const linhas = document.querySelectorAll('#listaPerguntas .pergunta');
    const semResultados = document.getElementById('semResultados');

    if (busca) {
        busca.addEventListener('input', function () {
            const termo = busca.value.trim().toLowerCase();
            let visiveis = 0;

            linhas.forEach(function (linha) {
                const combina = !termo || (linha.dataset.texto || '').includes(termo);
                linha.hidden = !combina;
                if (combina) visiveis++;
            });

            if (semResultados) semResultados.hidden = visiveis !== 0 || linhas.length === 0;
        });
    }

    // ----- Janela "Escreva a alteração" -----
    const modalEditar = document.getElementById('modalEditar');
    if (modalEditar) {
        modalEditar.addEventListener('show.bs.modal', function (ev) {
            const botao = ev.relatedTarget;
            modalEditar.querySelector('[name="id"]').value = botao.dataset.id;
            modalEditar.querySelector('[name="novoTexto"]').value = botao.dataset.texto;
            modalEditar.querySelector('[name="motivo"]').value = '';
        });
    }

    // ----- Janela de confirmação de remoção -----
    const modalRemover = document.getElementById('modalRemover');
    if (modalRemover) {
        modalRemover.addEventListener('show.bs.modal', function (ev) {
            const botao = ev.relatedTarget;
            modalRemover.querySelector('[name="id"]').value = botao.dataset.id;
            modalRemover.querySelector('#removerTexto').textContent = botao.dataset.texto;

            const usada = botao.dataset.usada === 'sim';
            modalRemover.querySelector('#removerUsada').hidden = !usada;
            modalRemover.querySelector('#removerNaoUsada').hidden = usada;
            modalRemover.querySelector('#botaoConfirmarRemover').textContent = usada ? 'Desativar' : 'Excluir';
        });
    }
})();
