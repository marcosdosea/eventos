(function () {
    "use strict";

    function formatarDecimalPtBr(valorBruto) {
        const digitos = (valorBruto || "").replace(/\D/g, "");
        if (!digitos) {
            return "0,00";
        }
        return (parseFloat(digitos) / 100).toFixed(2).replace(".", ",");
    }

    function gerenciarCampoDecimal(idTela, idReal, onValorAlterado) {
        const campoTela = document.getElementById(idTela);
        const campoReal = document.getElementById(idReal);
        if (!campoTela || !campoReal) return;

        if (!campoReal.value) {
            campoReal.value = campoTela.value || "0,00";
        }

        campoTela.addEventListener("input", function (e) {
            const valorFormatado = formatarDecimalPtBr(e.target.value);
            e.target.value = valorFormatado;
            campoReal.value = valorFormatado;

            if (typeof onValorAlterado === "function") {
                onValorAlterado(valorFormatado);
            }
        });
    }

    function inicializarFormularioSubevento() {
        const form = document.getElementById("formSubevento");
        if (!form) return;

        const checkboxGratuito = document.getElementById("InscricaoGratuita_Checkbox");
        const hiddenGratuito = document.getElementById("InscricaoGratuita");
        const campoValorTela = document.getElementById("ValorInscricao_Tela");
        const campoValorReal = document.getElementById("ValorInscricao_Real");
        const grupoValor = document.getElementById("valorInscricaoInputGroup");
        const msgValor = document.getElementById("valorInscricaoMsg");

        const checkboxCertificado = document.getElementById("PossuiCertificado_Checkbox");
        const hiddenCertificado = document.getElementById("PossuiCertificado");
        const campoFreqTela = document.getElementById("FrequenciaMinimaCertificado_Tela");
        const grupoFreq = document.getElementById("frequenciaInputGroup");

        function validarGratuitoValor() {
            if (!checkboxGratuito || !campoValorReal) return true;

            const isGratuito = checkboxGratuito.checked;
            const valorTexto = campoValorReal.value || "0,00";
            const valorNumerico = parseFloat(valorTexto.replace(/\./g, "").replace(",", "."));

            if (isGratuito && !isNaN(valorNumerico) && valorNumerico > 0) {
                if (msgValor) {
                    msgValor.textContent = "Para subeventos gratuitos, o valor de inscrição deve ser 0,00.";
                }
                return false;
            }

            if (msgValor && msgValor.textContent === "Para subeventos gratuitos, o valor de inscrição deve ser 0,00.") {
                msgValor.textContent = "";
            }
            return true;
        }

        function sincronizarEstadoGratuito(limparValorQuandoGratuito) {
            if (!checkboxGratuito) return;

            const gratuito = checkboxGratuito.checked;
            if (hiddenGratuito) {
                hiddenGratuito.value = gratuito ? "1" : "0";
            }

            if (campoValorTela) {
                campoValorTela.readOnly = gratuito;
            }
            if (grupoValor) {
                grupoValor.classList.toggle("subevento-input-locked", gratuito);
            }

            if (gratuito && limparValorQuandoGratuito) {
                if (campoValorTela) campoValorTela.value = "0,00";
                if (campoValorReal) campoValorReal.value = "0,00";
                if (msgValor) msgValor.textContent = "";
            } else {
                validarGratuitoValor();
            }
        }

        function sincronizarEstadoCertificado() {
            if (!checkboxCertificado) return;

            const possuiCertificado = checkboxCertificado.checked;
            if (hiddenCertificado) {
                hiddenCertificado.value = possuiCertificado ? "1" : "0";
            }

            if (campoFreqTela) {
                campoFreqTela.readOnly = !possuiCertificado;
            }
            if (grupoFreq) {
                grupoFreq.classList.toggle("subevento-input-locked", !possuiCertificado);
            }
        }

        gerenciarCampoDecimal("FrequenciaMinimaCertificado_Tela", "FrequenciaMinimaCertificado_Real");
        gerenciarCampoDecimal("ValorInscricao_Tela", "ValorInscricao_Real", validarGratuitoValor);

        if (checkboxGratuito) {
            checkboxGratuito.addEventListener("change", function () {
                sincronizarEstadoGratuito(true);
            });
            sincronizarEstadoGratuito(false);
        }

        if (checkboxCertificado) {
            checkboxCertificado.addEventListener("change", sincronizarEstadoCertificado);
            sincronizarEstadoCertificado();
        }

        form.addEventListener("submit", function (e) {
            if (!validarGratuitoValor()) {
                e.preventDefault();
                if (campoValorTela) {
                    campoValorTela.focus();
                }
            }
        });
    }

    function inicializarTabelaSubeventos() {
        const tabela = document.getElementById("tableSubEvento");
        if (!tabela || !window.jQuery || !window.jQuery.fn.DataTable) return;
        if (window.jQuery.fn.DataTable.isDataTable(tabela)) return;

        const langUrl = tabela.getAttribute("data-language-url") || "";
        window.jQuery(tabela).DataTable({
            columnDefs: [
                { orderable: true, targets: [0, 1, 2, 3, 4] },
                { orderable: false, targets: [5] }
            ],
            searching: true,
            ordering: true,
            paging: true,
            order: [[2, "asc"]],
            language: {
                url: langUrl,
                search: "",
                searchPlaceholder: "Buscar subevento, evento ou tipo..."
            }
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        inicializarFormularioSubevento();
        inicializarTabelaSubeventos();
    });
})();
