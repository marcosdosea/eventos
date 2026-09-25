/*
 * Mensagens padrão do jQuery Validation em português (pt-BR).
 * Carregado após o jquery.validate/unobtrusive nas páginas do Identity para que
 * validações de campo no navegador (ex. campo obrigatório, e-mail inválido)
 * apareçam em português em vez do inglês padrão do plugin.
 * Observação: mensagens definidas via DataAnnotations (ErrorMessage) têm
 * prioridade sobre estes padrões, pois chegam ao plugin via data-val-*.
 */
(function ($) {
    if (!$ || !$.validator) {
        return;
    }

    $.extend($.validator.messages, {
        required: "Este campo é obrigatório.",
        remote: "Por favor, corrija este campo.",
        email: "Por favor, insira um endereço de e-mail válido.",
        url: "Por favor, insira uma URL válida.",
        date: "Por favor, insira uma data válida.",
        dateISO: "Por favor, insira uma data válida (AAAA-MM-DD).",
        number: "Por favor, insira um número válido.",
        digits: "Por favor, insira somente dígitos.",
        equalTo: "Por favor, insira o mesmo valor novamente.",
        maxlength: $.validator.format("Por favor, insira no máximo {0} caracteres."),
        minlength: $.validator.format("Por favor, insira ao menos {0} caracteres."),
        rangelength: $.validator.format("Por favor, insira um valor entre {0} e {1} caracteres."),
        range: $.validator.format("Por favor, insira um valor entre {0} e {1}."),
        max: $.validator.format("Por favor, insira um valor menor ou igual a {0}."),
        min: $.validator.format("Por favor, insira um valor maior ou igual a {0}."),
        step: $.validator.format("Por favor, insira múltiplos de {0}.")
    });
}(window.jQuery));
