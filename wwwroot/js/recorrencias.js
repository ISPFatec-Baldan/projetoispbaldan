// Painel: quadro "Recorrências registradas" (filtros por período e por setor).
// Busca em /Home/Recorrencias (endereço vem do atributo data-url) e desenha a lista.
(function () {
    const quadro = document.getElementById('quadroRecorrencias');
    if (!quadro) return;

    const url = quadro.dataset.url;
    const campoDe = document.getElementById('recDe');
    const campoAte = document.getElementById('recAte');
    const campoSetor = document.getElementById('recSetor');
    const botaoLimpar = document.getElementById('recLimpar');
    const resumo = document.getElementById('recResumo');
    const lista = document.getElementById('recLista');
    const estado = document.getElementById('recEstado');

    let controle = null;

    function mostrarEstado(texto, erro) {
        lista.replaceChildren();
        estado.textContent = texto;
        estado.classList.toggle('erro', !!erro);
        estado.hidden = false;
    }

    function desenhar(dados) {
        lista.replaceChildren();

        if (!dados.total) {
            resumo.textContent = '';
            mostrarEstado('Nenhuma recorrência encontrada para os filtros escolhidos.', false);
            return;
        }

        estado.hidden = true;
        resumo.textContent = dados.total + (dados.total === 1 ? ' recorrência encontrada' : ' recorrências encontradas');

        dados.itens.forEach(function (item) {
            const li = document.createElement('li');
            li.className = 'recorrencia-item';

            const texto = document.createElement('div');
            const pergunta = document.createElement('p');
            pergunta.className = 'recorrencia-pergunta';
            pergunta.textContent = item.pergunta;
            const meta = document.createElement('p');
            meta.className = 'recorrencia-meta';
            meta.textContent = item.setor + ' · última em ' + item.ultima;
            texto.append(pergunta, meta);

            const selo = document.createElement('span');
            selo.className = 'selo selo-perigo';
            selo.textContent = item.seguidas + ' seguidas';

            li.append(texto, selo);
            lista.append(li);
        });
    }

    async function carregar() {
        if (controle) controle.abort();
        controle = new AbortController();

        const parametros = new URLSearchParams();
        if (campoDe.value) parametros.set('de', campoDe.value);
        if (campoAte.value) parametros.set('ate', campoAte.value);
        if (campoSetor.value) parametros.set('idSetor', campoSetor.value);

        resumo.textContent = '';
        mostrarEstado('Carregando...', false);

        try {
            const resposta = await fetch(url + '?' + parametros.toString(), {
                headers: { 'Accept': 'application/json' },
                credentials: 'same-origin',
                signal: controle.signal
            });

            if (resposta.status === 400) {
                mostrarEstado(await resposta.text(), true);
                return;
            }
            if (!resposta.ok) throw new Error('Falha ' + resposta.status);

            desenhar(await resposta.json());
        } catch (erro) {
            if (erro.name === 'AbortError') return;
            mostrarEstado('Não foi possível carregar as recorrências. Tente novamente.', true);
        }
    }

    [campoDe, campoAte, campoSetor].forEach(function (campo) {
        campo.addEventListener('change', carregar);
    });

    botaoLimpar.addEventListener('click', function () {
        campoDe.value = '';
        campoAte.value = '';
        campoSetor.value = '';
        carregar();
    });

    carregar();   // ao abrir o painel, já lista as recorrências (sem filtros)
})();
