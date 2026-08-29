function formatarCpfForm(campo) {

    let cpf = campo.value.replace(/\D/g, "");
    if (cpf.length > 11) cpf = cpf.slice(0, 11);
    cpf = cpf.replace(/(\d{3})(\d)/, "$1.$2");
    cpf = cpf.replace(/(\d{3})(\d)/, "$1.$2");
    cpf = cpf.replace(/(\d{3})(\d{1,2})$/, "$1-$2");
    campo.value = cpf;
}

function removerMascara() {
    let cpf = document.getElementById("Cpf");
    if (cpf) {
        cpf.value = cpf.value.replace(/\D/g, "");
    }
}


document.addEventListener("DOMContentLoaded", function () {
    let campoCpf = document.getElementById("Cpf");
    if (campoCpf && campoCpf.value) {
        formatarCpfForm(campoCpf);
    }
});