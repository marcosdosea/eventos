function toggleSubeventoSelect(checkbox, id) {
    if (checkbox.disabled) return;
    const row = document.getElementById('row-subevento-' + id);
    const wrapper = document.getElementById('wrapper-tipo-' + id);
    const qtyInputs = document.querySelectorAll('.subevento-qty-' + id);
    if (checkbox.checked) {
        if (row) row.classList.add('row-selected');
        if (wrapper) wrapper.style.display = 'block';
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
        if (row) row.classList.remove('row-selected');
        if (wrapper) wrapper.style.display = 'none';
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
    const subeventosCheckboxes = document.querySelectorAll('.subevento-checkbox:checked:not(:disabled)');
    subeventosCheckboxes.forEach(checkbox => {
        const subId = checkbox.value;
        const subInputs = document.querySelectorAll('.subevento-qty-' + subId);
        subInputs.forEach(input => {
            const qty = parseInt(input.value, 10) || 0;
            const valor = parseFloat(input.dataset.valor) || 0;
            total += (qty * valor);
        });
    });

    // submit button disabled if sumEvento == 0 or > 8, or if a checked subevento has 0 tickets
    const btnSubmit = document.querySelector('.btn-submit:not(.btn-login)');
    if (btnSubmit) {
        let isSubeventoValid = true;
        if (subeventosCheckboxes.length > 0) {
            subeventosCheckboxes.forEach(checkbox => {
                const subId = checkbox.value;
                const subInputs = document.querySelectorAll('.subevento-qty-' + subId);
                let qtyEsteSub = 0;
                subInputs.forEach(input => {
                    qtyEsteSub += parseInt(input.value, 10) || 0;
                });
                if (subInputs.length > 0 && qtyEsteSub === 0) {
                    isSubeventoValid = false;
                }
            });
        }

        if (sumEvento === 0 || sumEvento > 8 || !isSubeventoValid) {
            btnSubmit.disabled = true;
            btnSubmit.style.opacity = '0.5';
            if (sumEvento === 0 || sumEvento > 8) {
                btnSubmit.title = "Selecione ao menos 1 quantidade para o evento.";
            } else {
                btnSubmit.title = "Informe a quantidade de ingressos para os subeventos selecionados.";
            }
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
