// ========================
// МОДАЛКА ЯЗЫКА — события
// ========================
languageModal.addEventListener('click', (e) => {
    if (e.target === languageModal) closeLanguageModal();
});

document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && languageModal.classList.contains('show')) {
        closeLanguageModal();
    }
});

languageList.addEventListener('click', (e) => {
    const option = e.target.closest('.language-option');
    if (!option) return;

    const isFirstPick = sourceLanguage === null;

    applySourceLanguage(option.dataset.lang);

    if (pendingFileContent !== null) {
        leftTextarea.value = pendingFileContent;
        pendingFileContent = null;
        updateTranslateButtonState();
    }

    if (isFirstPick) {
        showMainApp();
        if (welcomeScreen) welcomeScreen.style.display = 'none';
    }

    closeLanguageModal();

    setTimeout(() => {
        setBothTextareasHeight(calculateOptimalHeight());
    }, 260);
});

leftLangBtn.addEventListener('click', openLanguageModal);

// ========================
// ОБРАБОТЧИКИ
// ========================
loadFileBtn.addEventListener('click', pickAndLoadFile);

welcomeLoadFileBtn.addEventListener('click', () => {
    pickAndLoadFile();
});

welcomeChooseLangBtn.addEventListener('click', () => {
    hideWelcomeCard();
    openLanguageModal();
});

translateBtn.addEventListener('click', () => {
    if (!translateBtn.disabled) translateText();
});

viewTreeBtn.addEventListener('click', () => {
    if (lastTreeData) TreeView.open(lastTreeData);
});

downloadBtn.addEventListener('click', openFilenameModal);

closeModalBtn.addEventListener('click', closeModal);
cancelModalBtn.addEventListener('click', closeModal);
submitFilenameBtn.addEventListener('click', triggerDownload);
modal.addEventListener('click', (e) => {
    if (e.target === modal) closeModal();
});
filenameInput.addEventListener('keypress', (e) => {
    if (e.key === 'Enter') {
        e.preventDefault();
        triggerDownload();
    }
});

// ========================
// TEXTAREA
// ========================
leftTextarea.addEventListener('input', updateTranslateButtonState);
leftTextarea.addEventListener('paste', () => setTimeout(updateTranslateButtonState, 10));

// ========================
// RESIZE
// ========================
window.addEventListener('resize', () => {
    setTimeout(updateHeightOnResize, 100);
});