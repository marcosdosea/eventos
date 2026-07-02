$(document).ready(function () {

    function gerenciarCampoMonetario(idTela, idReal) {

        const campoTela = document.getElementById(idTela);

        const campoReal = document.getElementById(idReal);

        if (!campoTela || !campoReal) return;

        if (campoTela.value) {

            campoReal.value = campoTela.value.replace(/\./g, '').replace(',', '.');

        }

        campoTela.addEventListener('input', function (e) {

            let value = e.target.value.replace(/\D/g, '');

            if (value.length > 0) {

                value = (parseFloat(value) / 100).toFixed(2).replace('.', ',');

            } else {

                value = '0,00';

            }

            e.target.value = value;

            campoReal.value = value.replace(/\./g, '').replace(',', '.');

        });

    }

    gerenciarCampoMonetario("valorTela", "valorReal");

});