// Tela "Perguntas": busca instantânea, janelas (cadastrar / editar / excluir) e criação de tópico com o botão "+".
(function () {
    // ----- Filtros: qualquer mudança nos selects recarrega a lista -----
    const formFiltros = document.getElementById('formFiltros');
    if (formFiltros) {
        formFiltros.querySelectorAll('select').forEach(function (select) {
            select.addEventListener('change', function () { formFiltros.submit(); });
        });
    }

    // ----- Busca instantânea (filtra a lista que já está na página) -----
    const busca = document.getElementById('busca');
    const linhas = document.querySelectorAll('#listaPerguntas .pergunta');
    const semResultados = document.getElementById('semResultados');

    if (busca) {
        // Enter na caixa de busca não deve recarregar a página
        busca.addEventListener('keydown', function (ev) {
            if (ev.key === 'Enter') ev.preventDefault();
        });

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

    // ----- Janela de edição -----
    const modalEditar = document.getElementById('modalEditar');
    if (modalEditar) {
        modalEditar.addEventListener('show.bs.modal', function (ev) {
            const botao = ev.relatedTarget;
            modalEditar.querySelector('[name="id"]').value = botao.dataset.id;
            modalEditar.querySelector('[name="novoTexto"]').value = botao.dataset.texto;
            modalEditar.querySelector('[name="motivo"]').value = '';
            modalEditar.querySelector('[name="idTopico"]').value = botao.dataset.topico || '';

            const marcados = (botao.dataset.checklists || '').split(',').filter(Boolean);
            modalEditar.querySelectorAll('input[name="idsChecklists"]').forEach(function (caixa) {
                caixa.checked = marcados.indexOf(caixa.value) !== -1;
            });
        });
    }

    // ----- Janela de exclusão -----
    const modalRemover = document.getElementById('modalRemover');
    if (modalRemover) {
        modalRemover.addEventListener('show.bs.modal', function (ev) {
            const botao = ev.relatedTarget;
            modalRemover.querySelector('[name="id"]').value = botao.dataset.id;
            modalRemover.querySelector('#removerTexto').textContent = botao.dataset.texto;

            const areaChecklists = modalRemover.querySelector('#removerChecklists');
            areaChecklists.replaceChildren();
            const nomes = (botao.dataset.nomes || '').split('|').filter(Boolean);

            if (!nomes.length) {
                const vazio = document.createElement('span');
                vazio.className = 'chips-rotulo';
                vazio.textContent = 'Nenhum checklist.';
                areaChecklists.append(vazio);
            } else {
                nomes.forEach(function (nome) {
                    const chip = document.createElement('span');
                    chip.className = 'chip';
                    chip.textContent = nome;
                    areaChecklists.append(chip);
                });
            }

            const desativar = botao.dataset.modo === 'desativar';
            modalRemover.querySelector('#removerDesativar').hidden = !desativar;
            modalRemover.querySelector('#removerExcluir').hidden = desativar;
            modalRemover.querySelector('#botaoConfirmarRemover').textContent = desativar ? 'Desativar' : 'Excluir';
        });
    }

    // ----- Botão "+": cria um tópico novo sem sair da janela -----
    const scriptAtual = document.currentScript;
    const urlTopico = scriptAtual ? scriptAtual.dataset.urlTopico : null;

    document.querySelectorAll('.botao-novo-topico').forEach(function (botaoMais) {
        const coluna = botaoMais.closest('.col-md-5');
        const linhaNovo = coluna.querySelector('.novo-topico');
        const campo = linhaNovo.querySelector('input');
        const botaoCriar = linhaNovo.querySelector('.botao-criar-topico');
        const erro = coluna.querySelector('.texto-erro');

        function mostrarErro(texto) {
            erro.textContent = texto || '';
            erro.hidden = !texto;
        }

        botaoMais.addEventListener('click', function () {
            linhaNovo.hidden = !linhaNovo.hidden;
            mostrarErro('');
            if (!linhaNovo.hidden) campo.focus();
        });

        // Enter no campo cria o tópico (e não envia o formulário da pergunta)
        campo.addEventListener('keydown', function (ev) {
            if (ev.key === 'Enter') { ev.preventDefault(); botaoCriar.click(); }
        });

        botaoCriar.addEventListener('click', async function () {
            const nome = campo.value.trim();
            if (!nome) { mostrarErro('Digite o nome do tópico.'); return; }

            const token = document.querySelector('input[name="__RequestVerificationToken"]');
            const dados = new FormData();
            dados.append('nome', nome);

            botaoCriar.disabled = true;
            try {
                const resposta = await fetch(urlTopico, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': token ? token.value : '' },
                    body: dados,
                    credentials: 'same-origin'
                });

                if (!resposta.ok) {
                    mostrarErro(resposta.status === 400 ? await resposta.text() : 'Não foi possível criar o tópico.');
                    return;
                }

                const topico = await resposta.json();

                // Inclui o tópico nas listas de todas as janelas (se ainda não estiver)
                document.querySelectorAll('select.select-topico').forEach(function (select) {
                    const existe = Array.prototype.some.call(select.options, function (o) { return o.value === topico.id; });
                    if (!existe) {
                        const opcao = document.createElement('option');
                        opcao.value = topico.id;
                        opcao.textContent = topico.nome;
                        select.append(opcao);
                    }
                });

                coluna.querySelector('select.select-topico').value = topico.id;
                campo.value = '';
                linhaNovo.hidden = true;
                mostrarErro('');
            } catch (e) {
                mostrarErro('Não foi possível criar o tópico.');
            } finally {
                botaoCriar.disabled = false;
            }
        });
    });
})();
