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
            // Endpoint retorna DTO mínimo (cpf, nome, nomeCracha) — sem PII sensível.
            if (resposta && resposta.nome) {
                document.getElementById('Nome').value = resposta.nome;
            }
        }
    });
}
