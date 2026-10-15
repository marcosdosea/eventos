function toggleSelect(radio) {
    document.querySelectorAll('.subevent-item').forEach(el => el.classList.remove('selected'));
    if (radio.checked) {
        radio.closest('.subevent-item').classList.add('selected');
        calcularValorTotal();
    }
}

function toggleSubeventoSelect(checkbox, id) {
    const row = document.getElementById('row-subevento-' + id);
    const select = document.getElementById('select-tipo-' + id);
    if (checkbox.checked) {
        if (row) row.classList.add('row-selected');
        if (select) select.disabled = false;
    } else {
        if (row) row.classList.remove('row-selected');
        if (select) select.disabled = true;
    }
    calcularValorTotal();
}

function abrirEmentaModal(nome, descricao, tipo, carga) {
    const overlay = document.getElementById('ementaModalOverlay');
    const lblTitulo = document.getElementById('ementaModalTitulo');
    const lblTipo = document.getElementById('ementaModalTipo');
    const lblCarga = document.getElementById('ementaModalCarga');
    const lblDesc = document.getElementById('ementaModalDescricao');

    if (lblTitulo) lblTitulo.textContent = nome || "Ementa do Subevento";
    if (lblTipo) lblTipo.textContent = tipo || "Atividade";
    if (lblCarga) lblCarga.textContent = "Carga: " + (carga || "Livre");
    if (lblDesc) lblDesc.textContent = descricao || "Nenhuma ementa detalhada informada.";

    if (overlay) {
        overlay.style.display = 'flex';
        document.body.style.overflow = 'hidden';
    }
}

function fecharEmentaModal(event) {
    if (event && event.target && event.target.id !== 'ementaModalOverlay' && !event.target.classList.contains('modal-ementa-close') && event.target.tagName !== 'BUTTON') {
        return;
    }
    const overlay = document.getElementById('ementaModalOverlay');
    if (overlay) {
        overlay.style.display = 'none';
        document.body.style.overflow = 'auto';
    }
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

    // submit button disabled if no evento type selected or no subevento selected (when subeventos exist)
    const btnSubmit = document.querySelector('.btn-submit:not(.btn-login)');
    if (btnSubmit) {
        let isEventoValid = eventoRadio !== null;
        let isSubeventoValid = true;
        const hasSubeventos = document.querySelectorAll('.subevento-checkbox').length > 0;
        
        if (hasSubeventos) {
            isSubeventoValid = subeventosCheckboxes.length > 0;
        }

        if (!isEventoValid || !isSubeventoValid) {
            btnSubmit.disabled = true;
            btnSubmit.style.opacity = '0.5';
            btnSubmit.title = "Selecione a inscrição no evento e ao menos 1 subevento (se houver).";
        } else {
            btnSubmit.disabled = false;
            btnSubmit.style.opacity = '1';
            btnSubmit.title = "";
        }
    }

    // Atualizar HTML
    if (total === 0) {
        priceElement.textContent = "Gratuito";
    } else {
        priceElement.textContent = "R$ " + total.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }
}

// Modal functions
function openImageModal(src) {
    var modal = document.getElementById("imageModal");
    var modalImg = document.getElementById("expandedImg");
    modal.style.display = "block";
    modalImg.src = src;
    document.body.style.overflow = "hidden";
}

function closeImageModal() {
    var modal = document.getElementById("imageModal");
    modal.style.display = "none";
    document.body.style.overflow = "auto";
}

document.addEventListener('keydown', function(event) {
    if (event.key === "Escape") {
        closeImageModal();
        fecharEmentaModal();
    }
});

document.addEventListener("DOMContentLoaded", function() {
    calcularValorTotal();
});
