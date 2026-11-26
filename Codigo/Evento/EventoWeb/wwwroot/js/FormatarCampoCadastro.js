function validarCpfForm(campo) {
    const valor = campo.value.trim();
    const cpf = valor.replace(/[.-]/g, "");
    const formatoValido = /^(?:[0-9]{11}|[0-9]{3}\.[0-9]{3}\.[0-9]{3}-[0-9]{2})$/.test(valor);
    const digitoValido = (posicao) => {
        let soma = 0;
        for (let i = 0; i < posicao; i++) {
            soma += Number(cpf[i]) * (posicao + 1 - i);
        }
        return ((soma * 10) % 11) % 10 === Number(cpf[posicao]);
    };
    const valido = formatoValido && !/^([0-9])\1{10}$/.test(cpf)
        && digitoValido(9) && digitoValido(10);
    campo.setCustomValidity(!valor || valido ? "" : "CPF inválido.");
}

function formatarCpfForm(campo) {
    const valor = campo.value.trim();
    let cpf = valor.replace(/[.-]/g, "");
    if (/[^0-9]/.test(cpf) || cpf.length > 11) {
        validarCpfForm(campo);
        return;
    }
    cpf = cpf.replace(/(\d{3})(\d)/, "$1.$2");
    cpf = cpf.replace(/(\d{3})(\d)/, "$1.$2");
    cpf = cpf.replace(/(\d{3})(\d{1,2})$/, "$1-$2");
    campo.value = cpf;
    validarCpfForm(campo);
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
    const campoCpf = document.querySelector('input[oninput^="formatarCpfForm"]');
    if (campoCpf?.form) {
        campoCpf.form.addEventListener("submit", function (event) {
            validarCpfForm(campoCpf);
            if (!campoCpf.reportValidity()) {
                event.preventDefault();
                event.stopImmediatePropagation();
            }
        }, true);
    }
    let campoTelefone = document.getElementById("Telefone1");
    if (campoCpf && campoCpf.value) {
        validarCpfForm(campoCpf);
        if (campoTelefone) formatarTelefoneForm(campoTelefone);
    }
});

