function formatarCpfForm(campo) {

    let cpf = campo.value.replace(/\D/g, "");
    if (cpf.length > 11) cpf = cpf.slice(0, 11);
    cpf = cpf.replace(/(\d{3})(\d)/, "$1.$2");
    cpf = cpf.replace(/(\d{3})(\d)/, "$1.$2");
    cpf = cpf.replace(/(\d{3})(\d{1,2})$/, "$1-$2");
    campo.value = cpf;
}

function formatarTelefoneForm(campo) {

    let telefone = campo.value.replace(/\D/g, '');
    telefone = telefone.replace(/(\d{2})(\d)/, "($1) $2");
    telefone = telefone.replace(/(\d{5})(\d)/, "$1-$2");
    telefone = telefone.replace(/(\d{4})(\d)/, "$1$2");
    campo.value = telefone;
}

function removerMascara() {
    let cpf = document.getElementById("Cpf");
    let tel = document.getElementById("Telefone1");

    if (cpf) {
        cpf.value = cpf.value.replace(/\D/g, "");
    }

    if (tel) {
        tel.value = tel.value.replace(/\D/g, "");
    }
}


document.addEventListener("DOMContentLoaded", function () {
    let campoCpf = document.getElementById("Cpf");
    let campoTelefone = document.getElementById("Telefone1");
    if (campoCpf && campoCpf.value) {
        formatarCpfForm(campoCpf);
        formatarTelefoneForm(campoTelefone);
    }
});

