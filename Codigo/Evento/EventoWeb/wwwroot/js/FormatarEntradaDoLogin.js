function formatarEntradaLogin(input) {
    let valor = input.value;

    if (/[a-zA-Z@]/.test(valor)) {
        return;
    }
    valor = valor.replace(/\D/g, ""); 

    if (valor.length > 11) {
        valor = valor.slice(0, 11);
    }

    if (valor.length > 9) {
        valor = valor.replace(/(\d{3})(\d{3})(\d{3})(\d{1,2})/, "$1.$2.$3-$4");
    } else if (valor.length > 6) {
        valor = valor.replace(/(\d{3})(\d{3})(\d{1,3})/, "$1.$2.$3");
    } else if (valor.length > 3) {
        valor = valor.replace(/(\d{3})(\d{1,3})/, "$1.$2");
    }

    input.value = valor;
}