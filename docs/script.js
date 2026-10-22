// ========================
// ЭЛЕМЕНТЫ DOM
// ========================
const leftTextarea = document.getElementById('leftText');
const rightTextarea = document.getElementById('rightText');

const loadFileBtn = document.getElementById('loadFileBtn');
const translateBtn = document.getElementById('translateBtn');
const viewTreeBtn = document.getElementById('viewTreeBtn');
const downloadBtn = document.getElementById('downloadBtn');

// Стартовый экран
const welcomeScreen = document.getElementById('welcomeScreen');
const welcomeLoadFileBtn = document.getElementById('welcomeLoadFileBtn');
const welcomeChooseLangBtn = document.getElementById('welcomeChooseLangBtn');
const mainApp = document.getElementById('mainApp');

// Дерево разбора
let lastTreeData = null;

// Уведомления
const progressToast = document.getElementById('progressToast');
const successToast = document.getElementById('successToast');
const unknownLangToast = document.getElementById('unknownLangToast');

// Модалка сохранения файла
const modal = document.getElementById('filenameModal');
const filenameInput = document.getElementById('filenameInput');
const closeModalBtn = document.getElementById('closeModalBtn');
const cancelModalBtn = document.getElementById('cancelModalBtn');
const submitFilenameBtn = document.getElementById('submitFilenameBtn');

// ========================
// ЯЗЫКИ
// ========================
const LANGUAGES = {
    kt:     { name: 'Kotlin', flag: '💎', ext: '.kt',    theme: 'kotlin', aliases: ['.kt', '.kts']         },
    sharp:  { name: 'Sharp',  flag: '☕', ext: '.sharp', theme: 'sharp',  aliases: ['.cs', '.sharp']       },
    python: { name: 'Python', flag: '🐍', ext: '.py',    theme: 'python', aliases: ['.py', '.pyw', '.pyi'] },
};

const AVAILABLE_SOURCE_LANGS = ['kt', 'sharp', 'python'];

let sourceLanguage = null;
const targetLanguage = 'cpp';
const TARGET_EXT = '.cpp';

// DOM модалки языков
const languageModal = document.getElementById('languageModal');
const languageList = document.getElementById('languageList');

const leftLangBtn = document.getElementById('leftLangBtn');
const leftLangName = document.getElementById('leftLangName');

// Содержимое файла, ожидающее выбора языка (когда расширение не распознано)
let pendingFileContent = null;

// ========================
// ТЕМЫ
// ========================
const THEMES = {
    kotlin: { title: 'Kotlin++' },
    sharp:  { title: 'Sharp++'  },
    python: { title: 'Python++' },
};

const appTitle = document.getElementById('appTitle');

function applyTheme(themeKey) {
    if (!themeKey || !THEMES[themeKey]) themeKey = 'kotlin';
    document.documentElement.setAttribute('data-theme', themeKey);
    if (appTitle) {
        appTitle.textContent = THEMES[themeKey].title;
        // document.title намеренно не меняем — вкладка всегда "Lang++"
    }
}

// ========================
// СТАРТОВЫЙ ЭКРАН
// ========================

// Плавное скрытие всего welcome-экрана (fade-out 400 мс).
// Используется при загрузке распознанного файла.
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
// Вызывается сразу при клике "Create New File" — карточка уходит
// на фоне под модалкой, к моменту выбора языка её уже нет.
function hideWelcomeCard() {
    const card = welcomeScreen?.querySelector('.welcome-card');
    if (!card) return;
    card.classList.add('welcome-card-hide');
}

function showMainApp() {
    if (mainApp) mainApp.hidden = false;
}

// ========================
// УВЕДОМЛЕНИЯ
// ========================
function flashToast(el, duration = 2000) {
    el.classList.remove('show');
    void el.offsetWidth;
    el.classList.add('show');
    setTimeout(() => el.classList.remove('show'), duration);
}

function showInProgressMessage()  { flashToast(progressToast); }
function showSuccessSavedMessage(){ flashToast(successToast);  }
function showUnknownLangMessage() { flashToast(unknownLangToast, 2600); }

// ========================
// КНОПКА TRANSLATE
// ========================
function updateTranslateButtonState() {
    translateBtn.disabled =
        leftTextarea.value.trim() === '' || sourceLanguage === null;
}

// ========================
// TRANSLATE
// ========================
async function translateText() {
    if (translateBtn.disabled) return;

    const payload = {
        source: sourceLanguage,
        target: targetLanguage,
        code: leftTextarea.value
    };

    translateBtn.style.transform = 'scale(0.97)';
    setTimeout(() => { translateBtn.style.transform = ''; }, 120);

    try {
        const tunnelUrl = 'https://lang-plus-plus-jikop.loca.lt/translate';

        const response = await fetch(tunnelUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Bypass-Tunnel-Reminder': 'true'
            },
            body: JSON.stringify(payload)
        });

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.error || 'Ошибка компиляции');
        }

        const data = await response.json();
        rightTextarea.value = data.cppCode;

        lastTreeData = data.tree || null;
        viewTreeBtn.disabled = !lastTreeData;

    } catch (error) {
        console.error("Ошибка трансляции:", error);
        rightTextarea.value = `/* \n[ОШИБКА ТРАНСЛЯЦИИ]\nНе удалось связаться с сервером или парсер ANTLR обнаружил ошибку:\n${error.message}\n*/`;
        lastTreeData = null;
        viewTreeBtn.disabled = true;
    } finally {
        if (typeof autoResize === 'function') autoResize(rightTextarea);
    }
}

// ========================
// DOWNLOAD
// ========================
function openFilenameModal() {
    filenameInput.value = '';
    modal.classList.add('show');
    setTimeout(() => filenameInput.focus(), 100);
}
function closeModal() { modal.classList.remove('show'); }

function triggerDownload() {
    let fileName = filenameInput.value.trim();
    if (fileName === '') fileName = 'document';
    fileName = fileName.replace(/[^a-zA-Z0-9_\-]/g, '_');
    if (fileName.length === 0) fileName = 'downloaded_file';

    const fullFileName = `${fileName}${TARGET_EXT}`;
    let contentToSave = rightTextarea.value;
    if (contentToSave.trim() === '') contentToSave = '// Generated file\n';

    const blob = new Blob([contentToSave], { type: 'text/plain' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.href = url;
    link.download = fullFileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);

    showSuccessSavedMessage();
    closeModal();
}

// ========================
// ВЫСОТА TEXTAREA
// ========================
function calculateOptimalHeight() {
    const header = document.querySelector('.header-title');
    const panels = document.querySelector('.panels');
    if (!header || !panels) return 320;

    const headerTop = header.getBoundingClientRect().top;
    const windowHeight = window.innerHeight;
    const panelsTop = panels.getBoundingClientRect().top;
    const bottomOffset = headerTop + 30;

    let availableHeight = windowHeight - panelsTop - bottomOffset;
    const minHeight = 200;
    const maxHeight = 800;
    return Math.max(minHeight, Math.min(maxHeight, availableHeight));
}

let isManualResizing = false;

function setBothTextareasHeight(height) {
    leftTextarea.style.height = height + 'px';
    leftTextarea.style.overflowY = 'auto';
    rightTextarea.style.height = height + 'px';
    rightTextarea.style.overflowY = 'auto';
}

function updateHeightOnResize() {
    if (isManualResizing) return;
    setBothTextareasHeight(calculateOptimalHeight());
}

// ========================
// РЕСАЙЗ
// ========================
function setupManualResize(textarea, otherTextarea, sideName) {
    let wrapper = textarea.parentElement;
    if (!wrapper.classList.contains('textarea-wrapper')) {
        const newWrapper = document.createElement('div');
        newWrapper.className = 'textarea-wrapper';
        textarea.parentNode.insertBefore(newWrapper, textarea);
        newWrapper.appendChild(textarea);
        wrapper = newWrapper;
    }

    let handle = wrapper.querySelector('.resize-handle');
    if (!handle) {
        handle = document.createElement('div');
        handle.className = 'resize-handle';
        wrapper.appendChild(handle);
    }

    handle.style.cursor = 'ns-resize';
    handle.addEventListener('mousedown', function(e) {
        e.preventDefault();
        e.stopPropagation();
        isManualResizing = true;
        const startY = e.clientY;
        const startHeight = textarea.offsetHeight;

        function onMouseMove(moveEvent) {
            const delta = moveEvent.clientY - startY;
            let newHeight = startHeight + delta;
            newHeight = Math.max(150, Math.min(800, newHeight));
            textarea.style.height = newHeight + 'px';
            textarea.style.overflowY = 'auto';
            otherTextarea.style.height = newHeight + 'px';
            otherTextarea.style.overflowY = 'auto';
        }
        function onMouseUp() {
            isManualResizing = false;
            document.removeEventListener('mousemove', onMouseMove);
            document.removeEventListener('mouseup', onMouseUp);
        }
        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
    });
}

// ========================
// МОДАЛКА ЯЗЫКА
// ========================
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
}

languageModal.addEventListener('click', (e) => {
    if (e.target === languageModal) closeLanguageModal();
});

document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && languageModal.classList.contains('show')) {
        closeLanguageModal();
    }
});

// Клик по языку в модалке
languageList.addEventListener('click', (e) => {
    const option = e.target.closest('.language-option');
    if (!option) return;

    const isFirstPick = sourceLanguage === null;

    applySourceLanguage(option.dataset.lang);

    // Если ждали выбора языка для загруженного файла — вставим содержимое
    if (pendingFileContent !== null) {
        leftTextarea.value = pendingFileContent;
        pendingFileContent = null;
        updateTranslateButtonState();
    }

    if (isFirstPick) {
        // Показываем mainApp под модалкой (его не видно — модалка непрозрачна).
        showMainApp();
        // Карточка welcome уже давно скрыта — просто убираем весь экран.
        // За модалкой сразу виден mainApp, никаких белых прямоугольников.
        if (welcomeScreen) welcomeScreen.style.display = 'none';
    }

    // Модалка закроется плавно (CSS transition: opacity + visibility, 0.25s).
    closeLanguageModal();

    setTimeout(() => {
        setBothTextareasHeight(calculateOptimalHeight());
    }, 260);
});

// Клик по заголовку левой панели — открывает ту же модалку (смена языка)
leftLangBtn.addEventListener('click', openLanguageModal);

// ========================
// ЗАГРУЗКА ФАЙЛА
// ========================
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
                // Если welcome-карточка ещё видна — прячем её,
                // чтобы за модалкой не было белого прямоугольника.
                hideWelcomeCard();
                setTimeout(openLanguageModal, 300);
            }

            updateTranslateButtonState();
        };
        reader.readAsText(file, 'UTF-8');
    };

    fileInput.click();
}

// ========================
// ОБРАБОТЧИКИ
// ========================
loadFileBtn.addEventListener('click', pickAndLoadFile);

welcomeLoadFileBtn.addEventListener('click', () => {
    // welcome не скрываем — если расширение распознается, уйдём сами;
    // если нет, поверх welcome откроется модалка
    pickAndLoadFile();
});

welcomeChooseLangBtn.addEventListener('click', () => {
    // Прячем welcome-карточку сразу — пока пользователь выбирает язык,
    // она плавно исчезает на фоне под модалкой.
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

// ========================
// INIT
// ========================
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

    console.log('Готово.');
}

document.addEventListener('DOMContentLoaded', init);
