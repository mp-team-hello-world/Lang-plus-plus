// Плавное скрытие всего welcome-экрана (fade-out 400 мс).
function hideWelcomeScreen() {
    if (!welcomeScreen || welcomeScreen.classList.contains('hide')) return;
    welcomeScreen.classList.add('hide');
    setTimeout(() => {
        if (sourceLanguage !== null) {
            welcomeScreen.style.display = 'none';
        }
    }, 400);
}

// Плавное скрытие только карточки welcome (без фона).
function hideWelcomeCard() {
    const card = welcomeScreen?.querySelector('.welcome-card');
    if (!card) return;
    card.classList.add('welcome-card-hide');
}

function showMainApp() {
    if (mainApp) mainApp.hidden = false;
}