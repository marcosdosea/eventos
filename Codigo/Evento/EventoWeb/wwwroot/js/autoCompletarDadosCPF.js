function autoCompletarCPF() {
    var cpf = document.getElementById('Cpf').value.replace(/\D/g, '');

    if (!cpf) {
        return;
    }
    if (cpf.length !== 11) {
        return;
    }

    $.ajax({
        url: '/Pessoa/BuscarPessoaPorCpf',
        type: 'GET',
        data: { cpf: cpf },
        success: function (resposta) {
            document.getElementById('Nome').value = resposta.nome;
            document.getElementById('Telefone1').value = resposta.telefone1;
            document.getElementById('Email').value = resposta.email;
        }
    });
}