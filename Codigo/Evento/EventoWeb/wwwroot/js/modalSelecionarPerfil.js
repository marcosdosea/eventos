document.addEventListener("DOMContentLoaded", function () {
        var modalElement = document.getElementById('modalEscolherPerfil');
        if (modalElement) {
            var myModal = new bootstrap.Modal(modalElement);
            myModal.show();
        }
});

