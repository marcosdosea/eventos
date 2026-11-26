(() => {
    const cpf = document.getElementById('Cpf');
    cpf.addEventListener('blur', () => {
        if (cpf.validity.valid) {
            autoCompletarCPF();
        }
    });

    const filtro = document.getElementById('filtroGestores');
    if (!filtro) return;

    const modal = document.getElementById('confirmRemovalModal');
    modal.addEventListener('show.bs.modal', (event) => {
        const gestor = event.relatedTarget;
        if (!gestor?.dataset.pessoaId) {
            event.preventDefault();
            return;
        }

        document.getElementById('removerGestorId').value = gestor.dataset.pessoaId;
        document.getElementById('removerGestorNome').textContent = gestor.dataset.pessoaNome;
    });

    const normalizarNome = (nome) => nome.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase();
    const linhas = Array.from(document.querySelectorAll('#gestoresVinculados tbody tr'), (linha) => ({
        linha,
        nome: normalizarNome(linha.dataset.nome),
        cpf: linha.dataset.cpf
    }));
    const anterior = document.getElementById('gestoresAnterior');
    const proximo = document.getElementById('gestoresProximo');
    const paginaAtual = document.getElementById('gestoresPagina');
    const contagem = document.getElementById('contagemGestores');
    const semResultados = document.getElementById('gestoresSemResultados');
    const porPagina = 10;
    let pagina = 1;

    function atualizarLista() {
        const termo = normalizarNome(filtro.value.trim());
        const cpfBusca = /^[0-9.\-\s]+$/.test(termo) ? termo.replace(/\D/g, '') : '';
        const resultados = linhas.filter((gestor) =>
            gestor.nome.includes(termo) || (cpfBusca && gestor.cpf.includes(cpfBusca)));
        const inicio = (pagina - 1) * porPagina;
        const fim = Math.min(inicio + porPagina, resultados.length);
        const visiveis = new Set(resultados.slice(inicio, fim));

        linhas.forEach((gestor) => {
            gestor.linha.hidden = !visiveis.has(gestor);
        });

        semResultados.hidden = resultados.length > 0;
        paginaAtual.textContent = pagina;
        anterior.disabled = pagina === 1;
        proximo.disabled = fim === resultados.length;
        contagem.textContent = resultados.length
            ? `Exibindo ${inicio + 1}–${fim} de ${resultados.length} ${resultados.length === 1 ? 'gestor' : 'gestores'}`
            : '0 gestores encontrados';
    }

    filtro.addEventListener('input', () => {
        pagina = 1;
        atualizarLista();
    });
    anterior.addEventListener('click', () => {
        pagina--;
        atualizarLista();
    });
    proximo.addEventListener('click', () => {
        pagina++;
        atualizarLista();
    });
    atualizarLista();
})();
