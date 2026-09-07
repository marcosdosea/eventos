function showModal(formId, modalId) {
    if (typeof (formId) === "string" && typeof (modalId) === "string") {
        form = $(`#${formId}`);
        modal = $(`#${modalId}`);
        if (form != null && modal != null) {
            modal.find("form").attr("action", form.attr("action"));
        }

        
    }
}
const modalExcluir = document.getElementById('modalExcluir');
if (modalExcluir) {
    modalExcluir.addEventListener('show.bs.modal', function (event) {
        const botao = event.relatedTarget;
        const nome = botao.getAttribute('data-nome').split(' ')[0];
        const nomeFormatado = nome.charAt(0).toUpperCase() + nome.slice(1).toLowerCase();
       
        const nomeItem = modalExcluir.querySelector('.nome-item');
        if (nomeItem) {
            if (nomeFormatado) {
                nomeItem.textContent = nomeFormatado;
            } else {
                nomeItem.textContent = 'essa pessoa';
            }

        } else {
            console.warn("O elemento '.nome-item' não foi encontrado no modal.");
        }
    });
}