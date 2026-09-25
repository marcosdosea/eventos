function exibirModalSelecionarPerfil() {
  var modalElement = document.getElementById('modalEscolherPerfil');
    if (modalElement) {
            var myModal = new bootstrap.Modal(modalElement);
            myModal.show();   
    }
}

function verificarEExibirModalPerfil() {
    var modalElement = document.getElementById('modalEscolherPerfil');
    if (modalElement) {

        var exibirModal = modalElement.getAttribute('data-exibir');

        if (exibirModal && exibirModal.toLowerCase() === 'true') {
            exibirModalSelecionarPerfil();
        }
    }
}

document.addEventListener("DOMContentLoaded", function () {
    verificarEExibirModalPerfil();
});
