function toggleInputBased(selectId, inputGroupId, value) {
    document.addEventListener("DOMContentLoaded", function () {
        var selectElement = document.getElementById(selectId);
        var inputGroup = document.getElementById(inputGroupId);

        function toggleInputGroup() {
            var isTriggerValue = selectElement.value === value;
            var input = inputGroup.querySelector('input');
            input.disabled = isTriggerValue;
            if (isTriggerValue) {
                input.value = '';
            }
        }

        selectElement.addEventListener('change', toggleInputGroup);
        toggleInputGroup();
    });
}

function validateInput(event) {
    const charCode = event.which ? event.which : event.keyCode;
    const char = String.fromCharCode(charCode);
    const pattern = /[0-9.,]/;

    if (!pattern.test(char) && ![8, 46, 37, 39].includes(charCode)) {
        event.preventDefault();
    }
}