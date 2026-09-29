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
    }
});

document.addEventListener("DOMContentLoaded", function() {
    calcularValorTotal();
});
