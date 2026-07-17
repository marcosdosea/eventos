function gerenciarCampoMonetario(idTela, idReal) {
    const campoTela = document.getElementById(idTela);
    const campoReal = document.getElementById(idReal);
    if (!campoTela || !campoReal) return;
    campoReal.value = campoTela.value;
    campoTela.addEventListener('input', function (e) {
        let value = e.target.value.replace(/\D/g, '');
        if (value.length > 0) {
            value = (parseFloat(value) / 100).toFixed(2).replace('.', ',');
        } else {
            value = '0,00';
        }
        e.target.value = value;
        campoReal.value = value;
    });
}
gerenciarCampoMonetario("ValorTela", "ValorReal");
gerenciarCampoMonetario('ValorInscricao_Tela', 'ValorInscricao_Real');
gerenciarCampoMonetario('FrequenciaMinimaCertificado_Tela', 'FrequenciaMinimaCertificado_Real');