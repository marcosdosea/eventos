function toggleSelect(radio) {
    document.querySelectorAll('.subevent-item').forEach(el => el.classList.remove('selected'));
    if (radio.checked) {
        radio.closest('.subevent-item').classList.add('selected');
        calcularValorTotal();
    }
}

function toggleSubeventoSelect(checkbox, id) {
    const wrapper = document.getElementById('wrapper-tipo-' + id);
    const select = document.getElementById('select-tipo-' + id);
    if (checkbox.checked) {
        wrapper.style.display = 'block';
        select.disabled = false;
    } else {
        wrapper.style.display = 'none';
        select.disabled = true;
    }
    calcularValorTotal();
}

function calcularValorTotal() {
    const priceElement = document.querySelector('.price-total');
    if (!priceElement) return;

    let total = 0;

    // Valor do evento principal
    const eventoRadio = document.querySelector('input[name="IdTipoInscricao"]:checked');
    if (eventoRadio && eventoRadio.dataset.valor) {
        total += parseFloat(eventoRadio.dataset.valor);
    }

    // Valor dos subeventos selecionados
    const subeventosCheckboxes = document.querySelectorAll('.subevento-checkbox:checked');
    subeventosCheckboxes.forEach(checkbox => {
        const id = checkbox.value;
        const select = document.getElementById('select-tipo-' + id);
        if (select && select.options[select.selectedIndex]) {
            const valorOption = select.options[select.selectedIndex].dataset.valor;
            if (valorOption) {
                total += parseFloat(valorOption);
            }
        }
    });

    // Atualizar HTML
    if (total === 0) {
        priceElement.textContent = "Gratuito";
    } else {
        priceElement.textContent = "R$ " + total.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }
}
