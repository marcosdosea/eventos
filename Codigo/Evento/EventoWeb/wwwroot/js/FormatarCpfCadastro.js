function formatarCpfForm(campo) {

    let cpf = campo.value.replace(/\D/g, "");
    cpf = cpf.replace(/(\d{3})(\d)/, "$1.$2");
    cpf = cpf.replace(/(\d{3})(\d)/, "$1.$2");
    cpf = cpf.replace(/(\d{3})(\d{1,2})$/, "$1-$2");
    campo.value = cpf;
}

function removerMascara() {
    let cpf = document.getElementById("Cpf");
    if (cpf) {
        cpf.value = cpf.value.replace(".", "");
        cpf.value = cpf.value.replace(".", "");
        cpf.value = cpf.value.replace("-", "");
    }
}