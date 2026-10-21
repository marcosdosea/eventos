function autoCompletarCPF(limitarCracha = false) {
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
            if (resposta) {
                var nomeCompleto = resposta.nome || resposta.Nome || "";
                var nomeCracha = resposta.nomeCracha || resposta.NomeCracha || "";

                var textoFinal = "";

                if (limitarCracha) {
                    var baseCracha = nomeCracha || nomeCompleto;
                    textoFinal = baseCracha.substring(0, 20);
                } else {
                    textoFinal = nomeCompleto;
                }

                var campoNome = document.getElementById('Nome');
                if (campoNome) {
                    campoNome.value = textoFinal;
                }

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