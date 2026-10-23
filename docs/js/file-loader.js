function detectLanguageFromFilename(fileName) {
    const lower = fileName.toLowerCase();
    for (const code of AVAILABLE_SOURCE_LANGS) {
        const aliases = LANGUAGES[code].aliases || [LANGUAGES[code].ext];
        for (const ext of aliases) {
            if (lower.endsWith(ext)) return code;
        }
    }
    return null;
}

function pickAndLoadFile() {
    const fileInput = document.createElement('input');
    fileInput.type = 'file';

    fileInput.onchange = (event) => {
        const file = event.target.files[0];
        if (!file) return;

        const detected = detectLanguageFromFilename(file.name);

        const reader = new FileReader();
        reader.onload = (e) => {
            const content = e.target.result;

            if (detected) {
                leftTextarea.value = content;
                applySourceLanguage(detected);
                hideWelcomeScreen();
                showMainApp();
                setTimeout(() => {
                    setBothTextareasHeight(calculateOptimalHeight());
                }, 50);
            } else {
                pendingFileContent = content;
                showUnknownLangMessage();
                hideWelcomeCard();
                setTimeout(openLanguageModal, 300);
            }

            updateTranslateButtonState();

            if (typeof refreshLeftHighlight === 'function') {
                refreshLeftHighlight();
            }
        };
        reader.readAsText(file, 'UTF-8');
    };

    fileInput.click();
}