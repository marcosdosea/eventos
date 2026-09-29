function toggleSubeventoSelect(checkbox, id) {
    const wrapper = document.getElementById('wrapper-tipo-' + id);
    const qtyInputs = document.querySelectorAll('.subevento-qty-' + id);
    if (checkbox.checked) {
        wrapper.style.display = 'block';
        qtyInputs.forEach(input => {
            input.disabled = false;
            const parts = input.id.split('_'); // qty_subevento_id_tipo
            if(parts.length >= 4) {
                const typeId = parts[3];
                const btnMinus = document.getElementById('btn_minus_subevento_' + id + '_' + typeId);
                const btnPlus = document.getElementById('btn_plus_subevento_' + id + '_' + typeId);
                if(btnMinus) btnMinus.disabled = false;
                if(btnPlus) btnPlus.disabled = false;
            }
        });
    } else {
        wrapper.style.display = 'none';
        qtyInputs.forEach(input => {
            input.disabled = true;
            input.value = 0;
            const parts = input.id.split('_');
            if(parts.length >= 4) {
                const typeId = parts[3];
                const btnMinus = document.getElementById('btn_minus_subevento_' + id + '_' + typeId);
                const btnPlus = document.getElementById('btn_plus_subevento_' + id + '_' + typeId);
                if(btnMinus) btnMinus.disabled = true;
                if(btnPlus) btnPlus.disabled = true;
            }
        });
    }
    calcularValorTotal();
}

let lastValidState = {}; // para reverter em caso de passar de 8

function changeQty(group, id, delta) {
    const inputId = group === 'evento' ? 'qty_evento_' + id : 'qty_' + group + '_' + id;
    const input = document.getElementById(inputId);
    if (!input || input.disabled) return;

    let current = parseInt(input.value, 10);
    if (isNaN(current)) current = 0;
    
    let newValue = current + delta;
    if (newValue < 0) newValue = 0;
    if (newValue > 8) newValue = 8;
    
    // test if global sum exceeds 8
    const isEvento = group === 'evento';
    const classSelector = isEvento ? '.evento-qty' : '.subevento-qty-' + group.split('_')[1];
    
    let sum = 0;
    document.querySelectorAll(classSelector).forEach(inp => {
        if (inp.id !== inputId) {
            sum += parseInt(inp.value, 10) || 0;
        }
    });
    
    if (sum + newValue > 8) {
        alert("O limite máximo é de 8 ingressos no total para este item.");
        return;
    }
    
    input.value = newValue;
    calcularValorTotal();
}

function calcularValorTotal() {
    const priceElement = document.querySelector('.price-total');
    if (!priceElement) return;

    let total = 0;
    let sumEvento = 0;
    
    // Valor e soma do evento principal
    const eventoInputs = document.querySelectorAll('.evento-qty');
    eventoInputs.forEach(input => {
        const qty = parseInt(input.value, 10) || 0;
        sumEvento += qty;
        const valor = parseFloat(input.dataset.valor) || 0;
        total += (qty * valor);
    });
    
    // Valor dos subeventos selecionados
    const subeventosCheckboxes = document.querySelectorAll('.subevento-checkbox:checked');
    subeventosCheckboxes.forEach(checkbox => {
        const subId = checkbox.value;
        const subInputs = document.querySelectorAll('.subevento-qty-' + subId);
        subInputs.forEach(input => {
            const qty = parseInt(input.value, 10) || 0;
            const valor = parseFloat(input.dataset.valor) || 0;
            total += (qty * valor);
        });
    });

    // submit button disabled if sumEvento == 0 or > 8 or no subevento selected (when subeventos exist)
    const btnSubmit = document.querySelector('.btn-submit:not(.btn-login)');
    if (btnSubmit) {
        let isSubeventoValid = true;
        const hasSubeventos = document.querySelectorAll('.subevento-checkbox').length > 0;
        
        if (hasSubeventos) {
            isSubeventoValid = false;
            if (subeventosCheckboxes.length > 0) {
                let qtySubeventoTotal = 0;
                subeventosCheckboxes.forEach(checkbox => {
                    const subId = checkbox.value;
                    const subInputs = document.querySelectorAll('.subevento-qty-' + subId);
                    subInputs.forEach(input => {
                        qtySubeventoTotal += parseInt(input.value, 10) || 0;
                    });
                });
                // Se os subeventos tiverem input de quantidade, pelo menos 1 ingresso de subevento deve ser selecionado
                if (qtySubeventoTotal > 0) {
                    isSubeventoValid = true;
                }
            }
        }

        if (sumEvento === 0 || sumEvento > 8 || !isSubeventoValid) {
            btnSubmit.disabled = true;
            btnSubmit.style.opacity = '0.5';
            btnSubmit.title = "Selecione ao menos 1 quantidade para o evento e 1 subevento (se houver).";
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
