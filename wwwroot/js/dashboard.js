// Painel: gráfico em rosca "Status de ações" e seletor de período.
// Os dados chegam do servidor em window.dadosDashboard (definido em Views/Home/Index.cshtml).
(function () {
    const dados = window.dadosDashboard;
    const canvas = document.getElementById('graficoStatusAcoes');

    if (dados && canvas && typeof Chart !== 'undefined') {

        // Escreve o total no centro da rosca
        const totalCentral = {
            id: 'totalCentral',
            afterDraw: function (chart) {
                const meta = chart.getDatasetMeta(0);
                if (!meta.data.length) return;

                const centro = meta.data[0];
                const total = chart.data.datasets[0].data.reduce(function (a, b) { return a + b; }, 0);
                const ctx = chart.ctx;

                ctx.save();
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                ctx.fillStyle = '#111827';
                ctx.font = '700 30px Inter, system-ui, sans-serif';
                ctx.fillText(total, centro.x, centro.y - 8);
                ctx.fillStyle = '#6B7280';
                ctx.font = '500 12px Inter, system-ui, sans-serif';
                ctx.fillText('ações', centro.x, centro.y + 16);
                ctx.restore();
            }
        };

        new Chart(canvas, {
            type: 'doughnut',
            data: {
                labels: dados.rotulos,
                datasets: [{
                    data: dados.valores,
                    backgroundColor: dados.cores,
                    borderColor: '#FFFFFF',
                    borderWidth: 3
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '62%',
                plugins: {
                    legend: { position: 'bottom', labels: { usePointStyle: true, padding: 16 } },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                const total = ctx.dataset.data.reduce(function (a, b) { return a + b; }, 0);
                                const pct = total ? ((ctx.parsed / total) * 100).toFixed(1) : 0;
                                return ctx.label + ': ' + ctx.parsed + ' (' + pct + '%)';
                            }
                        }
                    }
                }
            },
            plugins: [totalCentral]
        });
    }

    // Seletor de período (Semana / Mês / Trimestre).
    // TODO: quando os dados forem reais, recarregar indicadores e gráfico para o período escolhido.
    const botoes = document.querySelectorAll('.periodo-chips button');
    botoes.forEach(function (botao) {
        botao.addEventListener('click', function () {
            botoes.forEach(function (b) { b.setAttribute('aria-pressed', 'false'); });
            botao.setAttribute('aria-pressed', 'true');
        });
    });
})();
