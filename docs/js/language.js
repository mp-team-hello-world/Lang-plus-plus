function renderLanguageList() {
    languageList.innerHTML = '';

    AVAILABLE_SOURCE_LANGS.forEach(code => {
        const info = LANGUAGES[code];
        if (!info) return;

        const btn = document.createElement('button');
        btn.className = 'language-option';
        btn.dataset.lang = code;
        if (code === sourceLanguage) btn.classList.add('active');

        btn.innerHTML = `
            <span class="lang-flag">${info.flag}</span>
            <span class="lang-name">${info.name}</span>
            <span class="lang-check">✓</span>
        `;
        languageList.appendChild(btn);
    });
}

function openLanguageModal() {
    renderLanguageList();
    languageModal.classList.add('show');
}

function closeLanguageModal() {
    languageModal.classList.remove('show');
}

function applySourceLanguage(langCode) {
    if (!AVAILABLE_SOURCE_LANGS.includes(langCode)) return;
    if (!LANGUAGES[langCode]) return;

    sourceLanguage = langCode;
    leftLangName.textContent = LANGUAGES[langCode].name;

    const themeForLang = LANGUAGES[langCode].theme;
    if (themeForLang && THEMES[themeForLang]) applyTheme(themeForLang);

    updateTranslateButtonState();

    if (typeof refreshLeftHighlight === 'function') {
        refreshLeftHighlight();
    }
}