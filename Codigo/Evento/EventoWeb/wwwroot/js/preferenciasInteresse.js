(() => {
    const form = document.getElementById('formPreferenciasInteresse');
    const editando = form?.dataset.editando === 'true';
    const brasil = document.getElementById('abrangencia-brasil');
    const estadosRadio = document.getElementById('abrangencia-estados');
    const lista = document.getElementById('lista-estados');
    const areasCheckboxes = Array.from(document.querySelectorAll('.check-area'));
    const estadosCheckboxes = Array.from(document.querySelectorAll('.check-estado'));
    const contadorAreas = document.getElementById('contadorAreasSelecionadas');
    const contadorEstados = document.getElementById('contadorEstadosSelecionados');
    const resumo = document.getElementById('resumoCombinacao');

    function atualizarEstadoVisualCards() {
        document.querySelectorAll('.pref-scope-card').forEach(card => {
            const input = card.querySelector('input[type="radio"]');
            card.classList.toggle('is-selected', Boolean(input && input.checked));
        });
        areasCheckboxes.forEach(chk => {
            const tile = chk.closest('.pref-select-tile');
            if (tile) tile.classList.toggle('is-selected', chk.checked);
        });
        estadosCheckboxes.forEach(chk => {
            const chip = chk.closest('.pref-state-chip');
            if (chip) chip.classList.toggle('is-selected', chk.checked);
        });
    }

    function atualizarContadoresEResumo() {
        const qtdAreas = editando ? 1 : areasCheckboxes.filter(c => c.checked).length;
        const qtdEstados = estadosCheckboxes.filter(c => c.checked).length;

        if (contadorAreas) {
            contadorAreas.textContent = `${qtdAreas} de ${areasCheckboxes.length} ${qtdAreas === 1 ? 'selecionada' : 'selecionadas'}`;
        }
        if (contadorEstados) {
            contadorEstados.textContent = `${qtdEstados} de ${estadosCheckboxes.length} ${qtdEstados === 1 ? 'estado' : 'estados'}`;
        }

        if (!resumo) return;

        if (!editando && qtdAreas === 0) {
            resumo.innerHTML = '<i class="bi bi-info-circle blue-normal"></i><span>Selecione ao menos uma área de interesse.</span>';
            return;
        }

        if (brasil && brasil.checked) {
            const labelArea = editando ? '1 área selecionada' : `${qtdAreas} ${qtdAreas === 1 ? 'área' : 'áreas'}`;
            resumo.innerHTML = `<i class="bi bi-globe-americas blue-normal"></i><span>Pronto para salvar: <strong>${labelArea}</strong> para <strong>Todo o Brasil</strong>.</span>`;
            return;
        }

        if (qtdEstados === 0) {
            resumo.innerHTML = '<i class="bi bi-geo-alt blue-normal"></i><span>Selecione ao menos um estado abaixo.</span>';
            return;
        }

        if (editando) {
            resumo.innerHTML = `<i class="bi bi-check2-all blue-normal"></i><span>Atualizando para <strong>${qtdEstados} ${qtdEstados === 1 ? 'estado' : 'estados'}</strong>.</span>`;
            return;
        }

        const total = qtdAreas * qtdEstados;
        resumo.innerHTML = `<i class="bi bi-layers blue-normal"></i><span>Combinação: <strong>${qtdAreas} ${qtdAreas === 1 ? 'área' : 'áreas'} × ${qtdEstados} ${qtdEstados === 1 ? 'estado' : 'estados'}</strong> (${total} ${total === 1 ? 'preferência' : 'preferências'}).</span>`;
    }

    function atualizarAbrangencia() {
        const todoBrasil = Boolean(brasil && brasil.checked);
        if (lista) {
            lista.hidden = todoBrasil;
            lista.disabled = todoBrasil;
        }
        atualizarEstadoVisualCards();
        atualizarContadoresEResumo();
    }

    document.querySelectorAll('input[name="Abrangencia"]').forEach(radio => {
        radio.addEventListener('change', atualizarAbrangencia);
    });
    if (brasil && estadosRadio && !brasil.checked && !estadosRadio.checked) {
        brasil.checked = true;
    }

    function configurarSelecionarTodos(toggleId, itens, somenteVisiveisAoMarcar) {
        const toggle = document.getElementById(toggleId);
        if (!toggle) return;

        function sincronizarToggle() {
            toggle.checked = itens.length > 0 && itens.every(c => c.checked);
        }

        toggle.addEventListener('change', () => {
            const alvos = somenteVisiveisAoMarcar
                ? itens.filter(c => {
                    const col = c.closest('.pref-state-col');
                    return !col || !col.classList.contains('d-none');
                })
                : itens;
            alvos.forEach(c => { c.checked = toggle.checked; });
            atualizarEstadoVisualCards();
            atualizarContadoresEResumo();
        });

        itens.forEach(c => c.addEventListener('change', () => {
            sincronizarToggle();
            atualizarEstadoVisualCards();
            atualizarContadoresEResumo();
        }));
        sincronizarToggle();
    }

    configurarSelecionarTodos('selecionar-todas-areas', areasCheckboxes, false);
    configurarSelecionarTodos('selecionar-todos-estados', estadosCheckboxes, true);

    const filtroInput = document.getElementById('filtroEstados');
    const btnLimparFiltro = document.getElementById('btnLimparFiltroEstados');
    const colsEstados = Array.from(document.querySelectorAll('.pref-state-col'));
    const msgNenhumEstado = document.getElementById('nenhumEstadoEncontrado');

    function aplicarFiltroEstados() {
        if (!filtroInput) return;
        const termo = filtroInput.value.trim().toLowerCase();
        let visiveis = 0;

        colsEstados.forEach(col => {
            const texto = col.getAttribute('data-estado-texto') || '';
            const corresponde = termo === '' || texto.includes(termo);
            col.classList.toggle('d-none', !corresponde);
            if (corresponde) visiveis++;
        });

        if (btnLimparFiltro) btnLimparFiltro.classList.toggle('d-none', termo === '');
        if (msgNenhumEstado) msgNenhumEstado.classList.toggle('d-none', visiveis > 0);
    }

    if (filtroInput) {
        filtroInput.addEventListener('input', aplicarFiltroEstados);
    }
    if (btnLimparFiltro && filtroInput) {
        btnLimparFiltro.addEventListener('click', () => {
            filtroInput.value = '';
            aplicarFiltroEstados();
            filtroInput.focus();
        });
    }

    document.querySelectorAll('.pref-delete-form').forEach(deleteForm => {
        deleteForm.addEventListener('submit', event => {
            const areaNome = deleteForm.dataset.areaNome || 'esta área';
            if (!window.confirm(`Deseja realmente remover "${areaNome}" das suas áreas de interesse?`)) {
                event.preventDefault();
            }
        });
    });

    atualizarAbrangencia();
})();
