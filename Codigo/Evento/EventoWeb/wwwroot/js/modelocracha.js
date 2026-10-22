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
        const btnCarregarPredefinido = document.getElementById("btnCarregarPredefinido");

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
        const urlObterPredefinidoBase = form.dataset.obterPredefinidoUrl || "/Modelocracha/ObterPredefinido";
        const TEXTO_PADRAO_CRACHA = "Acesso pessoal e intransferível. Obrigatório porte visível em todas as atividades do congresso e catracas credenciadas.";

        let escalaAtual = 1;

        function calcularTamanhoTexto(texto) {
            if (!texto) return 0;
            return texto.replace(/\r?\n/g, "\r\n").length;
        }

        function limitarTextoAosCaracteres(texto, maxCaracteres, maxQuebras = 5) {
            if (!texto) return "";
            let tamanho = 0;
            let quebras = 0;
            let resultado = "";
            for (let i = 0; i < texto.length; i++) {
                const char = texto[i];
                if (char === "\n") {
                    if (quebras >= maxQuebras) {
                        continue;
                    }
                    quebras++;
                }
                const incremento = (char === "\n") ? 2 : 1;
                if (tamanho + incremento > maxCaracteres) {
                    break;
                }
                resultado += char;
                tamanho += incremento;
            }
            return resultado;
        }

        function atualizarTexto() {
            if (!txtTexto) return;

            const textoLimitado = limitarTextoAosCaracteres(txtTexto.value, 200, 5);
            if (txtTexto.value !== textoLimitado) {
                txtTexto.value = textoLimitado;
            }

            const len = calcularTamanhoTexto(txtTexto.value);
            if (lblContador) {
                lblContador.textContent = `${len} / 200 caracteres`;
            }
            if (lblTextoCracha) {
                lblTextoCracha.textContent = txtTexto.value || TEXTO_PADRAO_CRACHA;
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
                    lblNomeArquivo.textContent = nomeArquivo || "Nenhum arquivo selecionado";
                }
                if (lblInfoArquivo) {
                    lblInfoArquivo.textContent = infoTamanho || "PNG, JPEG, WEBP (máx. 64 KB)";
                }

                if (crachaLogoImg) {
                    crachaLogoImg.src = "";
                }
                if (crachaLogoContainer) {
                    crachaLogoContainer.style.display = "none";
                }
            }
        }

        function obterMimeType(nomeArquivo, base64) {
            if (nomeArquivo) {
                const nome = nomeArquivo.toLowerCase();
                if (nome.endsWith(".jpg") || nome.endsWith(".jpeg")) return "image/jpeg";
                if (nome.endsWith(".webp")) return "image/webp";
                if (nome.endsWith(".gif")) return "image/gif";
                if (nome.endsWith(".svg")) return "image/svg+xml";
                if (nome.endsWith(".png")) return "image/png";
            }
            if (base64) {
                if (base64.startsWith("/9j/")) return "image/jpeg";
                if (base64.startsWith("UklGR")) return "image/webp";
                if (base64.startsWith("R0lGO")) return "image/gif";
                if (base64.startsWith("iVBOR")) return "image/png";
            }
            return "image/png";
        }

        if (txtTexto) {
            txtTexto.addEventListener("keydown", function (e) {
                if (e.key === "Enter") {
                    const start = this.selectionStart;
                    const end = this.selectionEnd;
                    const selecionado = this.value.substring(start, end);
                    const quebrasNoSelecionado = (selecionado.match(/\n/g) || []).length;
                    const totalQuebrasAtuais = (this.value.match(/\n/g) || []).length;
                    if ((totalQuebrasAtuais - quebrasNoSelecionado + 1) > 5) {
                        e.preventDefault();
                    }
                }
            });
            txtTexto.addEventListener("input", atualizarTexto);
            txtTexto.addEventListener("change", atualizarTexto);
            txtTexto.addEventListener("paste", function () {
                setTimeout(atualizarTexto, 0);
            });
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
                                const mime = obterMimeType(data.nomeArquivo, data.logotipoBase64);
                                const srcImg = `data:${mime};base64,` + data.logotipoBase64;
                                aplicarLogotipo(srcImg, data.nomeArquivo || "logo_institucional.png", data.tamanhoArquivo || "64 KB");
                            } else {
                                if (hiddenLogo) hiddenLogo.value = "";
                                aplicarLogotipo(null);
                            }
                        } else {
                            if (txtTexto) {
                                txtTexto.value = data.texto || TEXTO_PADRAO_CRACHA;
                            }
                            if (chkQrcode) chkQrcode.checked = (data.qrcode === 1);
                            if (inputId) inputId.value = "0";

                            if (form) {
                                form.action = urlCreateBase;
                            }

                            if (data.temLogotipo && data.logotipoBase64) {
                                if (hiddenLogo) hiddenLogo.value = data.logotipoBase64;
                                const mime = obterMimeType(data.nomeArquivo, data.logotipoBase64);
                                const srcImg = `data:${mime};base64,` + data.logotipoBase64;
                                aplicarLogotipo(srcImg, data.nomeArquivo || "LOGOTIPO.png", data.tamanhoArquivo || "64 KB");
                            } else {
                                if (hiddenLogo) hiddenLogo.value = "";
                                aplicarLogotipo(null);
                            }
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

        if (btnCarregarPredefinido) {
            btnCarregarPredefinido.addEventListener("click", async function () {
                const idEvento = selectEvento ? selectEvento.value : 0;
                if (!idEvento || idEvento === "0") {
                    exibirMensagemFeedback("Selecione um evento antes de carregar o modelo pré-definido.", "warning");
                    return;
                }

                let texto = TEXTO_PADRAO_CRACHA;
                let qrcode = true;
                let logotipoBase64 = null;
                let nomeArquivo = "LOGOTIPO.png";
                let infoTamanho = "7.3 KB";

                try {
                    const endpoint = `${urlObterPredefinidoBase}/${idEvento}`;
                    const response = await fetch(endpoint);
                    if (response.ok) {
                        const data = await response.json();
                        if (data.texto) texto = data.texto;
                        qrcode = (data.qrcode === 1);
                        if (data.temLogotipo && data.logotipoBase64) {
                            logotipoBase64 = data.logotipoBase64;
                            nomeArquivo = data.nomeArquivo || "LOGOTIPO.png";
                            infoTamanho = data.tamanhoArquivo || "7.3 KB";
                        }
                    } else if (response.status === 403) {
                        exibirMensagemFeedback("Você não tem permissão para gerenciar modelos deste evento.", "danger");
                        return;
                    }
                } catch (err) {
                    console.warn("Falha ao buscar pré-definido do servidor, aplicando padrão local:", err);
                }

                if (txtTexto) {
                    txtTexto.value = texto;
                }
                if (chkQrcode) {
                    chkQrcode.checked = qrcode;
                }
                if (inputArquivo) {
                    inputArquivo.value = "";
                }

                if (logotipoBase64) {
                    if (hiddenLogo) hiddenLogo.value = logotipoBase64;
                    const mime = obterMimeType(nomeArquivo, logotipoBase64);
                    const srcImg = `data:${mime};base64,` + logotipoBase64;
                    aplicarLogotipo(srcImg, nomeArquivo, infoTamanho);
                } else {
                    if (hiddenLogo) hiddenLogo.value = "";
                    aplicarLogotipo(null, nomeArquivo, infoTamanho);
                }

                atualizarTexto();
                atualizarQrcode();
                exibirMensagemFeedback("Modelo pré-definido carregado com sucesso!");
            });
        }

        function exibirMensagemFeedback(mensagem, tipo) {
            tipo = tipo || "success";
            const icone = tipo === "success" ? "bi-check-circle-fill text-success" : (tipo === "warning" ? "bi-exclamation-triangle-fill text-warning" : "bi-exclamation-octagon-fill text-danger");
            let container = document.getElementById("alertaPredefinidoContainer");
            if (!container) {
                container = document.createElement("div");
                container.id = "alertaPredefinidoContainer";
                if (form && form.parentNode) {
                    form.parentNode.insertBefore(container, form);
                }
            }

            const alertDiv = document.createElement("div");
            alertDiv.className = `alert alert-${tipo} alert-dismissible fade show d-flex align-items-center shadow-sm rounded-3 py-2 px-3 mb-3`;
            alertDiv.setAttribute("role", "alert");

            const iconEl = document.createElement("i");
            iconEl.className = `bi ${icone} fs-5 me-2`;

            const spanEl = document.createElement("span");
            spanEl.className = "fw-semibold small";
            spanEl.textContent = mensagem;

            const btnClose = document.createElement("button");
            btnClose.type = "button";
            btnClose.className = "btn-close ms-auto";
            btnClose.setAttribute("data-bs-dismiss", "alert");
            btnClose.setAttribute("aria-label", "Fechar");

            alertDiv.appendChild(iconEl);
            alertDiv.appendChild(spanEl);
            alertDiv.appendChild(btnClose);

            container.innerHTML = "";
            container.appendChild(alertDiv);

            setTimeout(function () {
                if (alertDiv && alertDiv.parentNode) {
                    alertDiv.classList.remove("show");
                    setTimeout(function () {
                        if (alertDiv.parentNode) {
                            alertDiv.remove();
                        }
                    }, 300);
                }
            }, 4000);
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

        window.addEventListener("beforeprint", function () {
            if (crachaCard) {
                crachaCard.dataset.escalaPreImpressao = escalaAtual.toString();
                crachaCard.style.transform = "none";
            }
        });

        window.addEventListener("afterprint", function () {
            if (crachaCard && crachaCard.dataset.escalaPreImpressao !== undefined) {
                const escalaRestaurada = parseFloat(crachaCard.dataset.escalaPreImpressao);
                if (!isNaN(escalaRestaurada) && escalaRestaurada !== 1) {
                    crachaCard.style.transformOrigin = "top center";
                    crachaCard.style.transform = `scale(${escalaRestaurada})`;
                } else {
                    crachaCard.style.transform = "";
                }
                delete crachaCard.dataset.escalaPreImpressao;
            }
        });

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
