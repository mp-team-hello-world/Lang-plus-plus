function init() {
    console.log('Инициализация...');

    applyTheme('kotlin');

    if (mainApp) mainApp.hidden = true;

    const optimalHeight = calculateOptimalHeight();
    setBothTextareasHeight(optimalHeight);

    setupManualResize(leftTextarea, rightTextarea, 'LEFT');
    setupManualResize(rightTextarea, leftTextarea, 'RIGHT');

    leftLangName.textContent = 'Choose language';
    updateTranslateButtonState();

    progressToast.classList.remove('show');
    successToast.classList.remove('show');
    unknownLangToast.classList.remove('show');
    modal.classList.remove('show');
    languageModal.classList.remove('show');

    if (typeof initHighlighting === 'function') {
        initHighlighting();
    }

    console.log('Готово.');
}

document.addEventListener('DOMContentLoaded', init);