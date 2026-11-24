function validarCpf(cpfValor) {
    const cpf = (cpfValor || "").replace(/\D/g, "");
    if (cpf.length !== 11 || /^(\d)\1{10}$/.test(cpf)) {
        return false;
    }

    let soma = 0;
    for (let i = 0; i < 9; i++) {
        soma += parseInt(cpf.charAt(i), 10) * (10 - i);
    }
    let resto = soma % 11;
    let digito1 = resto < 2 ? 0 : 11 - resto;
    if (parseInt(cpf.charAt(9), 10) !== digito1) {
        return false;
    }

    soma = 0;
    for (let i = 0; i < 10; i++) {
        soma += parseInt(cpf.charAt(i), 10) * (11 - i);
    }
    resto = soma % 11;
    let digito2 = resto < 2 ? 0 : 11 - resto;
    return parseInt(cpf.charAt(10), 10) === digito2;
}

function atualizarValidacaoCpfCampo(campo) {
    if (!campo) return true;
    const digitos = (campo.value || "").replace(/\D/g, "");
    const valido = digitos.length === 0 || (digitos.length === 11 && validarCpf(digitos));

    if (!valido && digitos.length === 11) {
        campo.setCustomValidity("CPF inválido");
    } else {
        campo.setCustomValidity("");
    }

    const container = campo.closest(".form-group, .form-floating");
    const spanErro = container ? container.querySelector("span[data-valmsg-for]") : null;
    if (spanErro) {
        if (!valido && digitos.length === 11) {
            spanErro.textContent = "CPF inválido";
            spanErro.classList.remove("field-validation-valid");
            spanErro.classList.add("field-validation-error");
        } else if (spanErro.textContent === "CPF inválido") {
            spanErro.textContent = "";
            spanErro.classList.remove("field-validation-error");
            spanErro.classList.add("field-validation-valid");
        }
    }

    return valido;
}

function formatarCpfForm(campo) {

    let cpf = campo.value.replace(/\D/g, "");
    if (cpf.length > 11) cpf = cpf.slice(0, 11);
    cpf = cpf.replace(/(\d{3})(\d)/, "$1.$2");
    cpf = cpf.replace(/(\d{3})(\d)/, "$1.$2");
    cpf = cpf.replace(/(\d{3})(\d{1,2})$/, "$1-$2");
    campo.value = cpf;
    atualizarValidacaoCpfCampo(campo);
}

function removerMascara() {
    let cpf = document.getElementById("Cpf") || document.getElementById("Input_CPF");
    if (cpf) {
        cpf.value = cpf.value.replace(/\D/g, "");
    }
}


document.addEventListener("DOMContentLoaded", function () {
    let campoCpf = document.getElementById("Cpf") || document.getElementById("Input_CPF");
    if (campoCpf) {
        if (campoCpf.value) {
            formatarCpfForm(campoCpf);
        }
        campoCpf.addEventListener("blur", function () {
            atualizarValidacaoCpfCampo(campoCpf);
        });
        const form = campoCpf.closest("form");
        if (form) {
            form.addEventListener("submit", function (e) {
                const digitos = (campoCpf.value || "").replace(/\D/g, "");
                if (digitos.length > 0 && !validarCpf(digitos)) {
                    atualizarValidacaoCpfCampo(campoCpf);
                    campoCpf.setCustomValidity("CPF inválido");
                    e.preventDefault();
                    campoCpf.focus();
                }
            });
        }
    }
});