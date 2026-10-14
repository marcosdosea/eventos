(function () {
    "use strict";

    function inicializarModeloCrachaForm() {
        const form = document.getElementById("formModeloCracha");
        if (!form) return;

        const selectEvento = document.getElementById("selectEventoVinculado");
        const txtTexto = document.getElementById("txtTextoCracha");
        const lblContador = document.getElementById("lblContadorCaracteres");
        const chkQrcode = document.getElementById("chkHabilitarQrcode");
        const inputId = document.getElementById("inputModeloId");
        const hiddenLogo = document.getElementById("hiddenLogotipoBase64");
        const btnCancelar = document.getElementById("btnCancelarAcao");

        const btnUpload = document.getElementById("btnDispararUpload");
        const inputArquivo = document.getElementById("inputArquivoLogotipo");
        const thumbImg = document.getElementById("thumbImageElement");
        const thumbIcon = document.getElementById("thumbIconPlaceholder");
        const lblNomeArquivo = document.getElementById("lblNomeArquivoVisual");
        const lblInfoArquivo = document.getElementById("lblInfoArquivoVisual");

        const crachaCard = document.getElementById("crachaCardElement");
        const lblNomeEvento = document.getElementById("lblPreviewNomeEvento");
        const lblTextoCracha = document.getElementById("lblPreviewTextoCracha");
        const blocoQrcode = document.getElementById("blocoQrCodePreview");
        const crachaLogoContainer = document.getElementById("crachaLogoContainer");
        const crachaLogoImg = document.getElementById("crachaLogoElement");

        const btnZoomIn = document.getElementById("btnZoomIn");
        const btnZoomOut = document.getElementById("btnZoomOut");
        const btnImprimir = document.getElementById("btnImprimirCracha");
        const btnFullscreen = document.getElementById("btnFullscreenPreview");

        const urlObterModeloBase = form.dataset.obterModeloUrl || "/Modelocracha/ObterModeloPorEvento";
        const urlIndexBase = form.dataset.indexUrl || "/Modelocracha/Index";
        const urlCreateBase = form.dataset.createUrl || "/Modelocracha/Create";
        const urlEditBase = form.dataset.editUrl || "/Modelocracha/Edit";

        let escalaAtual = 1;

        function atualizarTexto() {
            if (!txtTexto) return;
            const len = txtTexto.value.length;
            if (lblContador) {
                lblContador.textContent = `${len} / 200 caracteres`;
            }
            if (lblTextoCracha) {
                lblTextoCracha.textContent = txtTexto.value || "Acesso pessoal e intransferível. Obrigatório porte visível em todas as atividades do congresso e catracas credenciadas.";
            }
        }

        function atualizarQrcode() {
            if (!blocoQrcode || !chkQrcode) return;
            blocoQrcode.style.display = chkQrcode.checked ? "flex" : "none";
        }

        function aplicarLogotipo(srcDataUrl, nomeArquivo, infoTamanho) {
            if (srcDataUrl && srcDataUrl.trim() !== "") {
                if (thumbImg) {
                    thumbImg.src = srcDataUrl;
                    thumbImg.style.display = "block";
                }
                if (thumbIcon) {
                    thumbIcon.style.display = "none";
                }
                if (lblNomeArquivo && nomeArquivo) {
                    lblNomeArquivo.textContent = nomeArquivo;
                }
                if (lblInfoArquivo && infoTamanho) {
                    lblInfoArquivo.textContent = infoTamanho;
                }

                if (crachaLogoImg) {
                    crachaLogoImg.src = srcDataUrl;
                }
                if (crachaLogoContainer) {
                    crachaLogoContainer.style.display = "flex";
                }
            } else {
                if (thumbImg) {
                    thumbImg.src = "";
                    thumbImg.style.display = "none";
                }
                if (thumbIcon) {
                    thumbIcon.style.display = "block";
                }
                if (lblNomeArquivo) {
                    lblNomeArquivo.textContent = "Nenhum arquivo selecionado";
                }
                if (lblInfoArquivo) {
                    lblInfoArquivo.textContent = "PNG, SVG, JPEG (máx. 64 KB)";
                }

                if (crachaLogoImg) {
                    crachaLogoImg.src = "";
                }
                if (crachaLogoContainer) {
                    crachaLogoContainer.style.display = "none";
                }
            }
        }

        if (txtTexto) {
            txtTexto.addEventListener("input", atualizarTexto);
            atualizarTexto();
        }

        if (chkQrcode) {
            chkQrcode.addEventListener("change", atualizarQrcode);
            atualizarQrcode();
        }

        if (btnUpload && inputArquivo) {
            btnUpload.addEventListener("click", function () {
                inputArquivo.click();
            });

            inputArquivo.addEventListener("change", function () {
                const file = this.files && this.files[0];
                if (file) {
                    const limiteBytes = 64 * 1024;
                    if (file.size > limiteBytes) {
                        alert("O arquivo selecionado excede o tamanho máximo suportado de 64 KB. Por favor, selecione uma imagem menor.");
                        this.value = "";
                        return;
                    }

                    const kb = Math.round(file.size / 1024);
                    const tamanhoTexto = `${kb} KB`;

                    const reader = new FileReader();
                    reader.onload = function (e) {
                        const resultUrl = e.target.result;
                        aplicarLogotipo(resultUrl, file.name, tamanhoTexto);

                        if (hiddenLogo) {
                            const partes = resultUrl.split(",");
                            hiddenLogo.value = partes.length > 1 ? partes[1] : partes[0];
                        }
                    };
                    reader.readAsDataURL(file);
                }
            });
        }

        if (selectEvento) {
            selectEvento.addEventListener("change", async function () {
                const idEvento = this.value;
                const selectedText = this.options[this.selectedIndex] ? this.options[this.selectedIndex].text : "";

                if (lblNomeEvento) {
                    lblNomeEvento.textContent = selectedText.trim();
                }

                if (!idEvento || idEvento === "0") return;

                if (btnCancelar) {
                    btnCancelar.href = `${urlIndexBase}?idEvento=${idEvento}`;
                }

                try {
                    const response = await fetch(`${urlObterModeloBase}/${idEvento}`);
                    if (response.ok) {
                        const data = await response.json();

                        if (data.existe) {
                            if (txtTexto) txtTexto.value = data.texto || "";
                            if (chkQrcode) chkQrcode.checked = (data.qrcode === 1);
                            if (inputId) inputId.value = data.id;

                            if (form && data.id > 0) {
                                form.action = `${urlEditBase}/${data.id}`;
                            }

                            if (data.temLogotipo && data.logotipoBase64) {
                                if (hiddenLogo) hiddenLogo.value = data.logotipoBase64;
                                const srcImg = "data:image/png;base64," + data.logotipoBase64;
                                aplicarLogotipo(srcImg, data.nomeArquivo || "logo_institucional.png", data.tamanhoArquivo || "64 KB");
                            } else {
                                if (hiddenLogo) hiddenLogo.value = "";
                                aplicarLogotipo(null);
                            }
                        } else {
                            if (txtTexto) {
                                txtTexto.value = data.texto || "Acesso pessoal e intransferível. Obrigatório porte visível em todas as atividades do congresso e catracas credenciadas.";
                            }
                            if (chkQrcode) chkQrcode.checked = (data.qrcode === 1);
                            if (inputId) inputId.value = "0";

                            if (form) {
                                form.action = urlCreateBase;
                            }

                            if (hiddenLogo) hiddenLogo.value = "";
                            aplicarLogotipo(null);
                        }

                        if (inputArquivo) inputArquivo.value = "";
                        atualizarTexto();
                        atualizarQrcode();
                    }
                } catch (error) {
                    console.error("Erro ao carregar modelo do evento:", error);
                }
            });
        }

        if (btnZoomIn && crachaCard) {
            btnZoomIn.addEventListener("click", function () {
                if (escalaAtual < 1.4) {
                    escalaAtual = Math.round((escalaAtual + 0.1) * 10) / 10;
                    crachaCard.style.transformOrigin = "top center";
                    crachaCard.style.transform = `scale(${escalaAtual})`;
                }
            });
        }

        if (btnZoomOut && crachaCard) {
            btnZoomOut.addEventListener("click", function () {
                if (escalaAtual > 0.6) {
                    escalaAtual = Math.round((escalaAtual - 0.1) * 10) / 10;
                    crachaCard.style.transformOrigin = "top center";
                    crachaCard.style.transform = `scale(${escalaAtual})`;
                }
            });
        }

        if (btnImprimir) {
            btnImprimir.addEventListener("click", function () {
                window.print();
            });
        }

        if (btnFullscreen) {
            btnFullscreen.addEventListener("click", function () {
                const container = document.querySelector(".preview-card-container");
                if (!document.fullscreenElement) {
                    container?.requestFullscreen().catch(err => console.log(err));
                } else {
                    document.exitFullscreen();
                }
            });
        }
    }

    function inicializarModeloCrachaIndex() {
        const filtroInput = document.getElementById("filtroModelos");
        const tabelaBody = document.getElementById("tabelaModelosBody");
        if (!filtroInput || !tabelaBody) return;

        const linhas = Array.from(tabelaBody.querySelectorAll(".linha-modelo"));
        const lblExibidos = document.getElementById("lblContagemExibidos");

        filtroInput.addEventListener("input", function () {
            const termo = filtroInput.value.toLowerCase().trim();
            let exibidos = 0;

            linhas.forEach(linha => {
                const evento = (linha.getAttribute("data-evento") || "").toLowerCase();
                const texto = (linha.getAttribute("data-texto") || "").toLowerCase();

                if (!termo || evento.includes(termo) || texto.includes(termo)) {
                    linha.style.display = "";
                    exibidos++;
                } else {
                    linha.style.display = "none";
                }
            });

            if (lblExibidos) {
                if (exibidos === 0) {
                    lblExibidos.textContent = "0";
                } else {
                    lblExibidos.textContent = `1-${exibidos}`;
                }
            }
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        if (document.getElementById("formModeloCracha")) {
            inicializarModeloCrachaForm();
        }
        if (document.getElementById("filtroModelos") && document.getElementById("tabelaModelosBody")) {
            inicializarModeloCrachaIndex();
        }
    });

    window.ModeloCracha = {
        initForm: inicializarModeloCrachaForm,
        initIndex: inicializarModeloCrachaIndex
    };
})();
