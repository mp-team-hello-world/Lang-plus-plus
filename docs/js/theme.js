function applyTheme(themeKey) {
    if (!themeKey || !THEMES[themeKey]) themeKey = 'kotlin';
    document.documentElement.setAttribute('data-theme', themeKey);
    if (appTitle) {
        appTitle.textContent = THEMES[themeKey].title;
    }
}