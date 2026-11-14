// Menu lateral: abre/fecha pelo botão de 3 linhas, fecha com Esc ou clicando fora.
(function () {
    const botao = document.getElementById('botaoMenu');
    const menu = document.getElementById('menuIsp');
    if (!botao || !menu) return;

    function alternar(abrir) {
        const aberto = abrir === undefined ? !menu.classList.contains('aberto') : abrir;
        menu.classList.toggle('aberto', aberto);
        botao.setAttribute('aria-expanded', String(aberto));
    }

    botao.addEventListener('click', function () { alternar(); });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') alternar(false);
    });

    document.addEventListener('click', function (e) {
        if (!menu.contains(e.target) && !botao.contains(e.target)) alternar(false);
    });
})();
