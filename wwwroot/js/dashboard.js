// Dashboard: gráfico de pizza "Status de Ações ISP".
// Os dados chegam do servidor em window.dadosDashboard (definido em Views/Home/Index.cshtml).
(function () {
    const dados = window.dadosDashboard;
    const canvas = document.getElementById('graficoStatusAcoes');

    if (!dados || !canvas || typeof Chart === 'undefined') return;

    new Chart(canvas, {
        type: 'pie',
        data: {
            labels: dados.rotulos,
            datasets: [{
                data: dados.valores,
                backgroundColor: dados.cores,
                borderColor: '#000000',
                borderWidth: 2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { position: 'bottom' },
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
        }
    });
})();
