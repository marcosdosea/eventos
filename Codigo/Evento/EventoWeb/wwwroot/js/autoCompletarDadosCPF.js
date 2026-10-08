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
            if (resposta && resposta.nome) {
                document.getElementById('Nome').value = resposta.nome;
                if (resposta.email) {
                    document.getElementById('Email').value = resposta.email;
                }

                if (resposta.telefone1) {
                    var campoTelefone = document.getElementById('Telefone1');
                    campoTelefone.value = resposta.telefone1;
                    formatarTelefoneForm(campoTelefone);
                }
            }
        }
    });
}
