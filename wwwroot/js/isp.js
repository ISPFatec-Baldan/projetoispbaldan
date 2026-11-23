// Menu lateral: o botão de 3 linhas recolhe (só ícones) ou expande (ícones e nomes).
// A escolha fica guardada neste navegador.
(function () {
    const botao = document.getElementById('botaoMenu');
    if (!botao) return;

    const chave = 'ispMenuRecolhido';

    function aplicar(recolhido) {
        document.body.classList.toggle('recolhida', recolhido);
        botao.setAttribute('aria-expanded', String(!recolhido));
        botao.setAttribute('aria-label', recolhido ? 'Expandir menu' : 'Recolher menu');
    }

    let recolhido = false;
    try { recolhido = localStorage.getItem(chave) === '1'; } catch (e) { /* ignora */ }
    aplicar(recolhido);

    botao.addEventListener('click', function () {
        recolhido = !document.body.classList.contains('recolhida');
        aplicar(recolhido);
        try { localStorage.setItem(chave, recolhido ? '1' : '0'); } catch (e) { /* ignora */ }
    });
})();
