function showModal(formId, modalId) {
    if (typeof (formId) === "string" && typeof (modalId) === "string") {
        form = $(`#${formId}`);
        modal = $(`#${modalId}`);
        if (form != null && modal != null) {
            modal.find("form").attr("action", form.attr("action"));
        }
    }
}