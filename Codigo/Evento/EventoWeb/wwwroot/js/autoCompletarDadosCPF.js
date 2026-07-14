$(document).ready(function () {
    async function autoCompletarCPF() {
        const cpf = document.getElementById("Cpf").value.replace(/\D/g, "");

        if (cpf.length !== 11) {
            return;
        }

        const resposta = await fetch(`/Pessoa/BuscarPessoaPorCpf?cpf=${cpf}`)

        if (!resposta.ok)
            return;

        const pessoa = await resposta.json();

        document.getElementById("Pessoa_Nome").value = pessoa.nome;
        document.getElementById("Pessoa_Email").value = pessoa.email;
        document.getElementById("Pessoa_Telefone1").value = pessoa.telefone;
    }
    autoCompletarCPF();
}